namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using louis_agent.core.config;
using louis_agent.core.providers;
using Microsoft.Extensions.AI;

/// <summary>
/// Orchestrates one chat turn: the model is given the agent's tools and
/// <see cref="FunctionInvokingChatClient"/> runs the tool-call loop until the model produces a final answer.
/// Provider specifics live entirely behind <see cref="IChatClient"/>.
/// </summary>
public partial class AgentEngine
{
    // Room for a sizeable file in one tool call; larger files are written in chunks (see default.md).
    internal const int MaxOutputTokens = 16_000;
    private const int ShellTimeoutMs = 30_000;

    // Tool results above this are summarised before reaching the model; normal source files stay verbatim.
    internal const int MaxToolResultChars = 50_000;
    // Keeps the summarisation request itself well inside a 200k-token context, even for token-dense diffs.
    private const int MaxSummaryInputChars = 200_000;

    // Set on tool calls from a response that hit the output limit: their arguments are incomplete.
    private const string TruncatedCallKey = "louis.truncated";

    internal const string TruncatedCallMessage =
        "Error: this tool call was cut off by the output limit before its arguments were complete, so it was NOT run. " +
        "Retrying the same call will fail the same way. Split large content into chunks of at most ~300 lines: " +
        "WriteWorkspaceFile with the first chunk, then AppendToFile for each following chunk, one call at a time, until done.";

    internal const string NoToolsNotice =
        "No tools are available in this session because the configured model does not support tool calling. " +
        "If the user asks you to perform an action (log time, run a skill, read or write files), say plainly that the " +
        "current model cannot do that and suggest switching LLM_MODEL to a tool-capable model. " +
        "Never pretend to have performed an action.";

    private Dictionary<string, MarkdownSkillLoader.SkillDef> _skills;
    private readonly PythonTools _python;
    private readonly PowerShellTools _powerShell;
    private readonly BashTools _bash;
    private readonly IChatClient _chatClient;
    private readonly IChatClient _innerChatClient;
    private readonly AgentOptions _options;
    private readonly List<AITool> _tools = [];

    /// <summary>Combined default instructions and skill documentation used as the system prompt.</summary>
    public string SkillDocumentation { get; private set; }
    public string WorkspaceRoot { get; }
    public bool SupportsTools { get; }
    public IReadOnlyList<AITool> Tools => _tools;

    /// <summary>The currently loaded skills (refreshed by <see cref="ReloadSkills"/>).</summary>
    public IReadOnlyCollection<MarkdownSkillLoader.SkillDef> Skills => _skills.Values;

    /// <summary>Raised after agent-built tools are added or removed, so hosts that publish <see cref="Tools"/> can resync.</summary>
    public event Action? ToolsChanged;

    /// <summary>Folder where <see cref="CreateSkillFile"/> writes new *-skills.md files; null disables skill authoring.</summary>
    public string? SkillsDirectory { get; init; }

    /// <summary>Reasoning effort for each turn (the model's thinking is streamed to hosts); null disables thinking.</summary>
    public ReasoningEffort? Thinking { get; init; }

    /// <summary>Rebuilds the skill set from disk for <see cref="ReloadSkills"/>; null disables reloading.</summary>
    public Func<ISkillProvider>? SkillReloader { get; init; }

    // Files created this session, so they load even when the agent function's skill set wouldn't pick them up.
    private readonly List<string> _createdSkillFiles = [];

    /// <summary>Approved agent-built tools live here (*.tool.md); new ones wait in its 'pending' subfolder.</summary>
    public string? ToolsDirectory => SkillsDirectory is null ? null : Path.Combine(SkillsDirectory, "tools");
    private string? PendingToolsDirectory => ToolsDirectory is null ? null : Path.Combine(ToolsDirectory, "pending");

    private readonly Dictionary<string, ScriptTool> _scriptTools = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="webSearch">Search engine for WebSearch; null selects one from configuration (see <see cref="WebSearchProviderFactory"/>).</param>
    public AgentEngine(ISkillProvider skillProvider, IChatClient chatClient, AgentOptions options, bool supportsTools = true,
        IWebSearchProvider? webSearch = null)
    {
        _skills = ToSkillMap(skillProvider);
        SkillDocumentation = skillProvider.Documentation;
        _options = options;
        SupportsTools = supportsTools;
        WorkspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        _python = new PythonTools(WorkspaceRoot);
        _powerShell = new PowerShellTools(WorkspaceRoot);
        _bash = new BashTools(WorkspaceRoot);

        _innerChatClient = chatClient;
        _chatClient = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation(configure: c =>
            {
                c.MaximumIterationsPerRequest = 10;
                c.FunctionInvoker = LogAndInvokeAsync;
                // Lets the model see why a call failed (e.g. a missing argument) instead of a generic failure.
                c.IncludeDetailedErrors = true;
            })
            .Use(MarkTruncatedToolCallsAsync, MarkTruncatedToolCallsStreamingAsync)
            .Build();

        if (!supportsTools)
        {
            Console.Error.WriteLine("[WARN] Model does not support tools; running without them.");
            return;
        }

        _tools.Add(AIFunctionFactory.Create(ExecuteSkill, nameof(ExecuteSkill)));
        _tools.Add(AIFunctionFactory.Create(CreateSkillFile, nameof(CreateSkillFile)));
        _tools.Add(AIFunctionFactory.Create(ReloadSkills, nameof(ReloadSkills)));
        _tools.Add(AIFunctionFactory.Create(CreateTool, nameof(CreateTool)));
        _tools.Add(AIFunctionFactory.Create(ListAgentTools, nameof(ListAgentTools)));
        AddPublicMethodsAsTools(new WorkspaceTools(WorkspaceRoot));
        AddPublicMethodsAsTools(new GitTools(WorkspaceRoot));
        AddPublicMethodsAsTools(new DotNetTools(WorkspaceRoot));
        AddPublicMethodsAsTools(_python);
        AddPublicMethodsAsTools(_powerShell);
        AddPublicMethodsAsTools(_bash);
        AddPublicMethodsAsTools(new WebTools(webSearch ?? WebSearchProviderFactory.Create(options)));
        if (string.IsNullOrWhiteSpace(options.PaymoApiKey))
        {
            Console.Error.WriteLine("[WARN] PAYMO_API_KEY not set; Paymo tool disabled.");
        }
        else
        {
            AddPublicMethodsAsTools(new PaymoTools(options));
        }

        if (string.IsNullOrWhiteSpace(options.DevopsApiKey))
        {
            Console.Error.WriteLine("[WARN] DEVOPS_API_KEY not set; DevOps tool disabled.");
        }
        else if (string.IsNullOrWhiteSpace(options.DevopsOrganization) || string.IsNullOrWhiteSpace(options.DevopsProject))
        {
            Console.Error.WriteLine("[WARN] DEVOPS_ORGANIZATION and DEVOPS_PROJECT must be set; DevOps tool disabled.");
        }
        else
        {
            AddPublicMethodsAsTools(new DevOpsTools(options.DevopsApiKey, options.DevopsOrganization, options.DevopsProject, options.DevopsTeam));
        }
    }

    private async ValueTask<object?> LogAndInvokeAsync(FunctionInvocationContext context, CancellationToken cancellationToken)
    {
        string arguments = JsonSerializer.Serialize(context.Arguments);
        Console.Error.WriteLine($"[TOOL] -> {context.Function.Name}({arguments})");

        if (context.CallContent.AdditionalProperties?.ContainsKey(TruncatedCallKey) == true)
        {
            // Running it would act on partial input, e.g. half a script or a file missing its content.
            Console.Error.WriteLine($"[TOOL] <- {context.Function.Name}: skipped, call was cut off by the output limit ({MaxOutputTokens} tokens)");
            AgentLog.RecordTruncatedToolCall(context.Function.Name, arguments, MaxOutputTokens);
            return TruncatedCallMessage;
        }

        object? result;
        try
        {
            result = await context.Function.InvokeAsync(context.Arguments, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"[TOOL] <- {context.Function.Name} failed: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        string text = result?.ToString() ?? string.Empty;
        Console.Error.WriteLine($"[TOOL] <- {context.Function.Name}: {(text.Length > 300 ? text[..300] + "..." : text)}");

        if (text.Length <= MaxToolResultChars) return result;

        // An oversized result (e.g. a huge diff) would blow the model's context window on the next request.
        string? request = context.Messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        return await SummariseToolResultAsync(context.Function.Name, arguments, text, request, cancellationToken);
    }

    /// <summary>
    /// Sits inside the tool-call loop: when the model stopped at the output limit, any tool calls it was writing are
    /// incomplete, so they are flagged for <see cref="LogAndInvokeAsync"/> to reject instead of run.
    /// </summary>
    private static async Task<ChatResponse> MarkTruncatedToolCallsAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next, CancellationToken cancellationToken)
    {
        ChatResponse response = await next.GetResponseAsync(messages, options, cancellationToken);
        if (response.FinishReason != ChatFinishReason.Length) return response;

        foreach (var call in response.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>())
            (call.AdditionalProperties ??= [])[TruncatedCallKey] = true;
        return response;
    }

    /// <summary>Streaming form of <see cref="MarkTruncatedToolCallsAsync"/>; the tool loop only runs calls once the stream ends.</summary>
    private static async IAsyncEnumerable<ChatResponseUpdate> MarkTruncatedToolCallsStreamingAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, IChatClient next,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var calls = new List<FunctionCallContent>();
        await foreach (var update in next.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            calls.AddRange(update.Contents.OfType<FunctionCallContent>());
            if (update.FinishReason == ChatFinishReason.Length)
                foreach (var call in calls)
                    (call.AdditionalProperties ??= [])[TruncatedCallKey] = true;
            yield return update;
        }
    }

    /// <summary>
    /// Condenses an oversized tool result with a separate, tool-less model call; falls back to truncation if that fails.
    /// </summary>
    internal async Task<string> SummariseToolResultAsync(
        string toolName, string arguments, string output, string? userRequest, CancellationToken cancellationToken = default)
    {
        string note = $"[{toolName} returned {output.Length:N0} chars, too large to include verbatim. " +
                      "If exact content is needed, call it again with narrower arguments (e.g. a specific file path).]";
        string input = output.Length <= MaxSummaryInputChars
            ? output
            : output[..MaxSummaryInputChars] + $"\n... [{output.Length - MaxSummaryInputChars:N0} more chars not shown]";

        try
        {
            Console.Error.WriteLine($"[TOOL] summarising {toolName} output ({output.Length:N0} chars)");
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System,
                    "You condense tool output for another AI agent that cannot see the original. Keep what matters for " +
                    "the user's request: file names, counts, errors, key values and the substance of changes. Be concise " +
                    "and factual. If the output looks mechanical (e.g. every line changed only by line endings or " +
                    "whitespace), say so plainly."),
                new(ChatRole.User,
                    $"User's request: {userRequest ?? "(unknown)"}\nTool: {toolName}({arguments})\n\nOutput:\n{input}"),
            };
            ChatResponse response = await _innerChatClient.GetResponseAsync(
                messages, new ChatOptions { MaxOutputTokens = MaxOutputTokens }, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response.Text))
            {
                string summary = $"{note}\nSummary:\n{response.Text}";
                AgentLog.RecordOversizedToolResult(toolName, arguments, output.Length, userRequest, "summarised", summary.Length);
                return summary;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"[WARN] Could not summarise {toolName} output: {ex.Message}");
        }

        string truncated = $"{note}\n{ProcessRunner.Truncate(output, MaxToolResultChars)}";
        AgentLog.RecordOversizedToolResult(toolName, arguments, output.Length, userRequest, "truncated", truncated.Length);
        return truncated;
    }

    private void AddPublicMethodsAsTools(object target)
    {
        foreach (MethodInfo method in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            _tools.Add(AIFunctionFactory.Create(method, target));
        }
    }

    /// <summary>
    /// Register a tool discovered from an external source (e.g., Rider MCP).
    /// Currently logs the discovery; full invocation requires MCP protocol implementation.
    /// Returns true if not already registered, false if duplicate.
    /// </summary>
    public bool RegisterDiscoveredTool(string name, string description)
    {
        if (!SupportsTools)
        {
            Console.Error.WriteLine($"[MCP] Cannot register tool '{name}': model does not support tools");
            return false;
        }

        if (_tools.Any(t => t.Name == name))
        {
            Console.Error.WriteLine($"[MCP] Tool '{name}' already registered; skipping");
            return false;
        }

        // Log discovered tool for informational purposes
        // Full MCP tool invocation would require a complete MCP protocol client
        Console.Error.WriteLine($"[MCP] Discovered tool from Rider: {name} - {description}");
        return true;
    }

    /// <summary>Run the interactive chat loop.</summary>
    public async Task RunAsync()
    {
        var history = NewHistory();

        // Ctrl+C stops the reply in progress; with no reply running it exits as usual.
        CancellationTokenSource? turn = null;
        Console.CancelKeyPress += (_, e) =>
        {
            if (Volatile.Read(ref turn) is not { } active) return;
            e.Cancel = true;
            active.Cancel();
        };

        Console.WriteLine("=== Louis Agent CLI ===");
        Console.WriteLine("Type 'exit' to quit; Ctrl+C stops a reply; /tools, /approve <Name>, /reject <Name> manage agent-built tools\n");

        while (true)
        {
            Console.Write("You: ");
            string? userInput = Console.ReadLine();

            // null = stdin closed (EOF); blank lines are ignored rather than ending the session.
            if (userInput is null || userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            // Approval commands come from the user only; they never reach the model.
            if (HandleUserCommand(userInput) is { } commandReply)
            {
                Console.WriteLine($"\n{commandReply}\n");
                continue;
            }

            using var cancellation = new CancellationTokenSource();
            Volatile.Write(ref turn, cancellation);
            try
            {
                Console.Write("\nAssistant: ");
                await StreamToConsoleAsync(history, userInput, cancellation.Token);
                Console.WriteLine("\n");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\n[stopped]\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}\n");
            }
            finally
            {
                Volatile.Write(ref turn, null);
            }
        }
    }

    /// <summary>Run a single prompt and exit (non-interactive mode).</summary>
    public async Task RunSinglePromptAsync(string userInput)
    {
        try
        {
            await StreamToConsoleAsync(NewHistory(), userInput);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>Writes the reply as it streams (thinking in grey), with a one-line status for each tool call.</summary>
    private async Task StreamToConsoleAsync(List<ChatMessage> history, string userInput, CancellationToken cancellationToken = default)
    {
        bool midLine = false, inThinking = false;
        await foreach (var update in StreamPromptAsync(history, userInput, cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent { Text.Length: > 0 } thinking:
                        var previous = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(thinking.Text);
                        Console.ForegroundColor = previous;
                        midLine = !thinking.Text.EndsWith('\n');
                        inThinking = true;
                        break;
                    case TextContent { Text.Length: > 0 } text:
                        if (inThinking)
                        {
                            // Separates the thinking from the answer that follows it.
                            Console.Write(midLine ? "\n\n" : "\n");
                            inThinking = false;
                        }
                        Console.Write(text.Text);
                        midLine = !text.Text.EndsWith('\n');
                        break;
                    case FunctionCallContent call:
                        if (midLine) Console.WriteLine();
                        inThinking = false;
                        WriteStatus($"  > {call.Name} {DescribeArguments(call.Arguments)}");
                        midLine = false;
                        break;
                    case FunctionResultContent result:
                        WriteStatus($"    {(result.Exception is null ? "done" : "failed")}: {FirstLine(result.Result?.ToString())}");
                        break;
                }
            }
        }

        static void WriteStatus(string line)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(line.Length > 160 ? line[..160] + "..." : line);
            Console.ForegroundColor = previous;
        }

        static string FirstLine(string? text) => (text ?? string.Empty).Split('\n', 2)[0].Trim();
    }

    /// <summary>Short, single-line view of a tool call's arguments for progress displays.</summary>
    public static string DescribeArguments(IDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0) return string.Empty;
        return string.Join(", ", arguments.Select(a =>
        {
            string value = a.Value?.ToString()?.ReplaceLineEndings(" ") ?? "null";
            return $"{a.Key}={(value.Length > 60 ? value[..60] + "..." : value)}";
        }));
    }

    /// <summary>A history seeded with the system prompt (and the no-tools notice when applicable).</summary>
    public List<ChatMessage> NewHistory()
    {
        var history = new List<ChatMessage> { new(ChatRole.System, SkillDocumentation) };
        if (!SupportsTools) history.Add(new ChatMessage(ChatRole.System, NoToolsNotice));
        return history;
    }

    /// <summary>
    /// Processes one prompt without writing to the console, allowing protocol adapters
    /// to control how the response is transported. Tool calls made by the model are executed
    /// and recorded in <paramref name="history"/> along with the final answer.
    /// </summary>
    public async Task<string> ProcessPromptAsync(
        List<ChatMessage> history,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        ChatOptions chatOptions = BeginTurn(history, userInput);
        ChatResponse response = await _chatClient.GetResponseAsync(history, chatOptions, cancellationToken);

        // Includes assistant tool-call and tool-result messages so follow-up turns keep their context.
        history.AddRange(response.Messages);
        return response.Text;
    }

    /// <summary>
    /// Streaming form of <see cref="ProcessPromptAsync"/>: yields text as the model writes it, each tool call as it is
    /// made (<see cref="FunctionCallContent"/>) and its outcome (<see cref="FunctionResultContent"/>). The completed turn
    /// is recorded in <paramref name="history"/> once the stream ends.
    /// </summary>
    public async IAsyncEnumerable<ChatResponseUpdate> StreamPromptAsync(
        List<ChatMessage> history,
        string userInput,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatOptions chatOptions = BeginTurn(history, userInput);
        for (int continuation = 0; ; continuation++)
        {
            var updates = new List<ChatResponseUpdate>();
            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, chatOptions, cancellationToken))
            {
                updates.Add(update);
                yield return update;
            }

            history.AddMessages(updates);

            // When streaming, a tool call cut off by the output limit is dropped entirely, so the turn would just stop.
            bool cutOff = updates.LastOrDefault(u => u.FinishReason is not null)?.FinishReason == ChatFinishReason.Length;
            if (!cutOff || continuation >= MaxCutOffContinuations) yield break;

            Console.Error.WriteLine($"[WARN] Reply cut off by the output limit ({MaxOutputTokens} tokens); asking the model to continue");
            AgentLog.RecordTruncatedToolCall("(streamed reply)", string.Empty, MaxOutputTokens);
            history.Add(new ChatMessage(ChatRole.User, ContinueAfterCutOffMessage));
        }
    }

    private const int MaxCutOffContinuations = 3;

    internal const string ContinueAfterCutOffMessage =
        "[automatic notice] Your previous reply was cut off by the output limit, so any tool call you were writing was " +
        "lost and NOT run. Continue the task from where it stopped, writing large content in smaller chunks " +
        "(AppendToFile, at most ~300 lines per call).";

    private ChatOptions BeginTurn(List<ChatMessage> history, string userInput)
    {
        // Skills may have been reloaded since this conversation started; keep its system prompt current.
        if (history.Count > 0 && history[0].Role == ChatRole.System && history[0].Text != SkillDocumentation)
            history[0] = new ChatMessage(ChatRole.System, SkillDocumentation);

        history.Add(new ChatMessage(ChatRole.User, userInput));

        var chatOptions = new ChatOptions { MaxOutputTokens = MaxOutputTokens };
        if (Thinking is { } effort)
            chatOptions.Reasoning = new ReasoningOptions { Effort = effort, Output = ReasoningOutput.Full };
        if (SupportsTools)
        {
            chatOptions.Tools = _tools;
            chatOptions.ToolMode = ChatToolMode.Auto;
        }

        return chatOptions;
    }

    [Description("Runs a named markdown skill — a reusable bash/PowerShell/Python procedure loaded from the Skills " +
        "folder — passing jsonArgs as key-value pairs that the skill receives as SKILL_ARG_* environment variables, " +
        "never spliced into the script text, so model-supplied values can't be interpreted as shell syntax. Skills " +
        "are for repeatable, pre-authored procedures (e.g. Paymo lookups, code-review checklists); if a dedicated " +
        "tool already exists for what you're trying to do, call that tool directly instead of going through a skill " +
        "with the same effect. If the named skill isn't found, the error lists every skill name currently loaded so " +
        "you can pick a valid one.")]
    public string ExecuteSkill(
        [Description("The exact name of the skill to execute (e.g., 'ListParentTasks').")] string skillName,
        [Description("JSON object of arguments for the skill (e.g., {\"project_id\":\"123\"}).")] string jsonArgs = "{}")
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return "Error: Skill name cannot be empty.";
        }

        skillName = skillName.Trim();

        if (!_skills.TryGetValue(skillName, out var skill))
        {
            return $"Error: Skill '{skillName}' was not found in the loaded skill definitions. Available skills: {string.Join(", ", _skills.Keys)}";
        }

        try
        {
            // Values are handed to the shell as environment variables and referenced as "${SKILL_ARG_x}",
            // so model-supplied text is never parsed as shell syntax.
            var env = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(_options.PaymoApiKey))
            {
                env["PAYMO_API_KEY"] = _options.PaymoApiKey;
                env["PAYMO_API_KEY_BASE64"] = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.PaymoApiKey}:X"));
            }

            if (!string.IsNullOrWhiteSpace(_options.DevopsApiKey))
            {
                env["DEVOPS_API_KEY"] = _options.DevopsApiKey;
            }

            if (!string.IsNullOrWhiteSpace(jsonArgs))
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonArgs);
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return "Error: Invalid JSON arguments: expected an object.";
                    }

                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        env[$"SKILL_ARG_{EnvNameSanitizer().Replace(prop.Name, "_")}"] =
                            prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.GetRawText();
                    }
                }
                catch (JsonException jsonEx)
                {
                    return $"Error: Invalid JSON arguments: {jsonEx.Message}";
                }
            }

            // {{name}} placeholders become environment-variable reads in the skill's own language.
            string resolvedCommand = PlaceholderPattern().Replace(skill.Command, m =>
            {
                string name = m.Groups[1].Value;
                string variable = name is "PAYMO_API_KEY" or "PAYMO_API_KEY_BASE64" ? name : $"SKILL_ARG_{name}";
                return skill.Language switch
                {
                    "powershell" => $"$env:{variable}",
                    "python" => $"__import__('os').environ.get('{variable}', '')",
                    _ => $"${{{variable}}}",
                };
            }).Trim();

            if (string.IsNullOrWhiteSpace(resolvedCommand))
            {
                return $"Error: Skill '{skillName}' template is empty after substitution.";
            }

            return skill.Language switch
            {
                "powershell" => FormatSkillResult(_powerShell.RunScript(resolvedCommand, null, [], null,
                    TimeSpan.FromMilliseconds(ShellTimeoutMs), out var noPowerShell, env), noPowerShell),
                "python" => FormatSkillResult(_python.RunScript(resolvedCommand, null, [], null,
                    TimeSpan.FromMilliseconds(ShellTimeoutMs), out var noPython, env), noPython),
                _ => RunShellCommand(resolvedCommand, env, WorkspaceRoot),
            };
        }
        catch (Exception ex)
        {
            return $"Failed to execute skill '{skillName}': {ex.Message}";
        }
    }

    [Description("""
        Creates (or updates, with overwrite=true) a reusable *-skills.md file under the Skills folder and loads it
        immediately so its skills become callable via ExecuteSkill in this same session — the file is saved to disk
        right away, but only becomes part of every future session's skill set if the agent's AGENT_FUNCTION setting
        loads it (e.g. 'louis', which loads every skill file). Use this when the user asks to save a repeatable
        procedure as a skill, not for a one-off action that only needs to happen once. A built-in skills file name
        (including 'default') cannot be overwritten with this tool. Markdown format, one section per skill:
        # <Title>
        <guidance for when/how to use these skills>
        ## Skill: <PascalCaseName>
        - Description: <one line>
        - Parameters:
          - `argName` (required|optional): <meaning>
        - Execution:
        ```bash
        python3 script.py "${SKILL_ARG_argName}"
        ```
        The code fence language picks the interpreter: ```bash (default), ```powershell or ```python. Code runs from the
        workspace root (30s limit). Read arguments only from environment variables, never by pasting values into code:
        bash "${SKILL_ARG_name}" (quoted), PowerShell $env:SKILL_ARG_name, Python os.environ['SKILL_ARG_name'].
        Guidance-only files (no Skill sections) are allowed.
        """)]
    public string CreateSkillFile(
        [Description("Short kebab-case name; the file is saved as <name>-skills.md (e.g. 'csv-reports')")] string name,
        [Description("Full markdown content of the skills file")] string markdown,
        [Description("Set true to replace an existing file previously created with this name")] bool overwrite = false)
    {
        if (SkillsDirectory is null) return "Error: skill authoring is not available (no skills folder was found).";
        name = (name ?? "").Trim().ToLowerInvariant();
        if (name.EndsWith("-skills")) name = name[..^"-skills".Length];
        if (!SkillFileName().IsMatch(name)) return "Error: name must be kebab-case letters/digits, e.g. 'csv-reports'.";
        if (string.IsNullOrWhiteSpace(markdown)) return "Error: markdown content is empty.";

        string fileName = $"{name}-skills.md";
        if (name == "default" || AgentHost.BuiltInSkillFiles.Contains(fileName))
            return $"Error: '{fileName}' is a built-in skills file and cannot be replaced; choose another name.";

        string path = Path.Combine(SkillsDirectory, fileName);
        if (File.Exists(path) && !overwrite)
            return $"Error: '{fileName}' already exists. Pass overwrite=true to replace it, or pick another name.";

        // Validate before saving so a malformed section is reported instead of silently skipped.
        string tempPath = Path.Combine(Path.GetTempPath(), $"skill-check-{Guid.NewGuid():N}.md");
        File.WriteAllText(tempPath, markdown);
        List<MarkdownSkillLoader.SkillDef> parsed;
        try { parsed = MarkdownSkillLoader.Load(tempPath); }
        finally { File.Delete(tempPath); }

        int declared = SkillHeader().Matches(markdown).Count;
        if (parsed.Count < declared)
            return $"Error: {declared} '## Skill:' section(s) found but only {parsed.Count} are valid. " +
                   "Each needs a '- Description:' line and an '- Execution:' line followed by a ```bash code block.";

        File.WriteAllText(path, markdown);
        if (!_createdSkillFiles.Contains(path, StringComparer.OrdinalIgnoreCase)) _createdSkillFiles.Add(path);

        string reload = ReloadSkills();
        string skills = parsed.Count == 0 ? "guidance only (no executable skills)" : string.Join(", ", parsed.Select(s => s.Name));
        return $"Saved {path} — {skills}.\n{reload}";
    }

    [Description("Re-reads every skill file from disk — including any created earlier this session with " +
        "CreateSkillFile — rebuilding the full skill set and the system prompt's skill documentation from scratch, " +
        "then returns the names of every skill now available. Use this after a skills file has been edited outside " +
        "the agent (e.g. by the user, or by another tool) so the change takes effect without restarting the " +
        "session; CreateSkillFile already reloads automatically, so there's no need to call this right after it. " +
        "Returns an error if the host wasn't configured with a skill reloader.")]
    public string ReloadSkills()
    {
        if (SkillReloader is null) return "Error: skill reloading is not available in this host.";

        var provider = new CompositeSkillProvider(SkillReloader());
        string loadedDocs = provider.Documentation;
        foreach (string file in _createdSkillFiles.Where(File.Exists))
        {
            // Skip files the reloader already picked up (e.g. via skills-folder discovery).
            if (!loadedDocs.Contains(File.ReadAllText(file).Trim(), StringComparison.Ordinal))
                provider.AddProvider(new MarkdownSkillProvider(file, includeDefaultInstructions: false));
        }

        _skills = ToSkillMap(provider);
        SkillDocumentation = provider.Documentation;
        return $"Skills reloaded. Available skills: {(_skills.Count == 0 ? "(none)" : string.Join(", ", _skills.Keys.Order()))}";
    }

    [Description("""
        Builds a new, typed tool from a markdown definition when no existing tool — built-in or already agent-built —
        can do what's needed and the capability is worth keeping, not for a single one-off action a skill or an
        inline script could handle just as well. The tool becomes callable immediately in this session; it is only
        saved for future sessions once the user explicitly approves it with '/approve <Name>' (reject with
        '/reject <Name>' to discard it). A name already used by a built-in tool, or an existing approved tool, is
        rejected. Definition format (markdown):
        # Tool: <PascalCaseName>
        - Description: <what it does and when to use it>
        - Timeout: <seconds, optional, default 60, max 600>
        - Parameters:
          - `argName` (string|integer|number|boolean, required|optional): <meaning>
        - Implementation:
        ```python
        import json, sys
        args = json.load(sys.stdin)   # validated arguments as a JSON object
        print(json.dumps({"result": ...}))
        ```
        Language: ```python, ```powershell (read input with [Console]::In.ReadToEnd() | ConvertFrom-Json) or ```bash.
        Arguments arrive as JSON on stdin (also as TOOL_ARG_<name> env vars) — never interpolate them into code or commands.
        Runs from the workspace root. Print the result (JSON preferred); exit non-zero with a message on failure.
        """)]
    public string CreateTool(
        [Description("The full tool definition markdown")] string definition,
        [Description("Set true to replace a pending tool you created earlier with the same name")] bool overwrite = false)
    {
        if (!SupportsTools) return "Error: tools are not supported by the current model.";
        if (PendingToolsDirectory is null) return "Error: tool building is not available (no skills folder was found).";

        var (parsed, errors) = ScriptToolDefinition.Parse(definition);
        if (parsed is null) return "Error: invalid tool definition:\n- " + string.Join("\n- ", errors);

        if (_scriptTools.TryGetValue(parsed.Name, out var existing))
        {
            if (existing.Approved) return $"Error: an approved tool named '{parsed.Name}' already exists; choose another name.";
            if (!overwrite) return $"Error: a pending tool named '{parsed.Name}' already exists. Pass overwrite=true to replace it.";
        }
        else if (_tools.Any(t => t.Name.Equals(parsed.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Error: '{parsed.Name}' is the name of a built-in tool; choose another name.";
        }

        Directory.CreateDirectory(PendingToolsDirectory);
        File.WriteAllText(Path.Combine(PendingToolsDirectory, $"{parsed.Name}.tool.md"), definition);
        RegisterScriptTool(parsed, approved: false);

        string parameters = parsed.Parameters.Count == 0
            ? "no parameters"
            : string.Join(", ", parsed.Parameters.Select(p => $"{p.Name}: {p.Type}{(p.Required ? "" : "?")}"));
        return $"Created tool '{parsed.Name}' ({parsed.Language}; {parameters}). You can call it now. " +
               $"It is pending: tell the user to type '/approve {parsed.Name}' to keep it for future sessions (or '/reject {parsed.Name}').";
    }

    [Description("Lists every agent-built tool known right now: approved ones that load in every session, ones " +
        "pending approval that are only active for this session, and — separately — pending tool files left on " +
        "disk from an earlier session that never got approved and aren't currently loaded. Each entry shows its " +
        "approval state and its own description. Use this before CreateTool to check whether a similar tool already " +
        "exists, or when the user asks what tools the agent has built for itself; it makes no changes.")]
    public string ListAgentTools()
    {
        var pendingOnDisk = PendingToolsDirectory is not null && Directory.Exists(PendingToolsDirectory)
            ? Directory.EnumerateFiles(PendingToolsDirectory, "*.tool.md").Select(f => Path.GetFileName(f)[..^".tool.md".Length]).ToList()
            : [];

        var lines = _scriptTools.Values.OrderBy(t => t.Name)
            .Select(t => $"- {t.Name} [{(t.Approved ? "approved" : "pending, active this session")}]: {t.Definition.Description}")
            .Concat(pendingOnDisk.Where(n => !_scriptTools.ContainsKey(n)).Order()
                .Select(n => $"- {n} [pending from an earlier session, not loaded]"))
            .ToList();

        return lines.Count == 0 ? "No agent-built tools yet." : string.Join('\n', lines);
    }

    /// <summary>
    /// Loads approved tools from <see cref="ToolsDirectory"/>. Called once by the host after construction.
    /// Pending tools are deliberately not loaded: they only last for the session that created them.
    /// </summary>
    public int LoadApprovedTools()
    {
        if (!SupportsTools || ToolsDirectory is null || !Directory.Exists(ToolsDirectory)) return 0;

        int loaded = 0;
        foreach (string file in Directory.EnumerateFiles(ToolsDirectory, "*.tool.md").Order())
        {
            var (parsed, errors) = ScriptToolDefinition.Parse(File.ReadAllText(file));
            if (parsed is null)
            {
                Console.Error.WriteLine($"[WARN] Skipping invalid tool {Path.GetFileName(file)}: {string.Join("; ", errors)}");
                continue;
            }

            if (_tools.Any(t => t.Name.Equals(parsed.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine($"[WARN] Skipping tool {Path.GetFileName(file)}: name '{parsed.Name}' is already in use.");
                continue;
            }

            RegisterScriptTool(parsed, approved: true);
            loaded++;
        }

        if (loaded > 0) Console.Error.WriteLine($"[INFO] Loaded {loaded} agent-built tool(s) from {ToolsDirectory}");
        return loaded;
    }

    /// <summary>
    /// Handles commands typed by the user (never exposed to the model): /tools, /approve &lt;Name&gt;, /reject &lt;Name&gt;.
    /// Returns the reply, or null when the input is not one of these commands.
    /// </summary>
    public string? HandleUserCommand(string input)
    {
        string[] parts = (input ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        string command = parts[0].ToLowerInvariant();
        string argument = parts.Length > 1 ? parts[1] : "";
        return command switch
        {
            "/tools" => ListAgentTools() + "\n\nApprove with /approve <Name>, discard with /reject <Name>.",
            "/approve" => ApproveTool(argument),
            "/reject" => RejectTool(argument),
            _ => null,
        };
    }

    private string ApproveTool(string name)
    {
        if (PendingToolsDirectory is null || ToolsDirectory is null) return "Tool building is not available.";
        if (string.IsNullOrWhiteSpace(name)) return "Usage: /approve <ToolName>";

        string pending = Path.Combine(PendingToolsDirectory, $"{name}.tool.md");
        if (!File.Exists(pending)) return $"No pending tool named '{name}'. Use /tools to see pending tools.";

        var (parsed, errors) = ScriptToolDefinition.Parse(File.ReadAllText(pending));
        if (parsed is null) return $"Cannot approve '{name}': the definition is invalid ({string.Join("; ", errors)}).";

        string approvedPath = Path.Combine(ToolsDirectory, $"{parsed.Name}.tool.md");
        if (File.Exists(approvedPath)) return $"An approved tool '{parsed.Name}' already exists; remove {approvedPath} first.";

        File.Move(pending, approvedPath);
        if (_scriptTools.TryGetValue(parsed.Name, out var tool))
        {
            tool.Approved = true;
            ToolsChanged?.Invoke();
        }
        else if (SupportsTools && !_tools.Any(t => t.Name.Equals(parsed.Name, StringComparison.OrdinalIgnoreCase)))
            RegisterScriptTool(parsed, approved: true);

        return $"Approved '{parsed.Name}'. Saved to {approvedPath}; it will load in every future session.";
    }

    private string RejectTool(string name)
    {
        if (PendingToolsDirectory is null) return "Tool building is not available.";
        if (string.IsNullOrWhiteSpace(name)) return "Usage: /reject <ToolName>";

        string pending = Path.Combine(PendingToolsDirectory, $"{name}.tool.md");
        bool hadFile = File.Exists(pending);
        if (hadFile) File.Delete(pending);

        bool wasActive = _scriptTools.TryGetValue(name, out var tool) && !tool.Approved;
        if (wasActive)
        {
            _scriptTools.Remove(name);
            _tools.Remove(tool!);
            ToolsChanged?.Invoke();
        }

        return hadFile || wasActive
            ? $"Rejected '{name}': deleted the pending definition{(wasActive ? " and removed it from this session" : "")}."
            : $"No pending tool named '{name}'.";
    }

    private void RegisterScriptTool(ScriptToolDefinition definition, bool approved)
    {
        if (_scriptTools.Remove(definition.Name, out var previous)) _tools.Remove(previous);

        var tool = new ScriptTool(definition, approved, RunScriptTool);
        _scriptTools[definition.Name] = tool;
        _tools.Add(tool);
        ToolsChanged?.Invoke();
    }

    private string RunScriptTool(ScriptToolDefinition definition, string inputJson, IReadOnlyDictionary<string, string> env)
    {
        var timeout = TimeSpan.FromSeconds(definition.TimeoutSeconds);
        string? notInstalled = null;
        ProcessRunner.Result? result = definition.Language switch
        {
            "python" => _python.RunScript(definition.Code, null, [], inputJson, timeout, out notInstalled, env),
            "powershell" => _powerShell.RunScript(definition.Code, null, [], inputJson, timeout, out notInstalled, env),
            _ => ProcessRunner.Run(ResolveShell(), ["-c", definition.Code], WorkspaceRoot, timeout, inputJson, env),
        };

        if (notInstalled is not null) return $"Error: {notInstalled}";
        if (result!.TimedOut) return $"[Timeout] {definition.Name} exceeded {definition.TimeoutSeconds}s and was killed.\n{ProcessRunner.Tail(result.Output)}";

        string output = ProcessRunner.Truncate(result.Output.Trim());
        if (result.ExitCode != 0) return $"[Exit Code {result.ExitCode}] {output}";
        return string.IsNullOrWhiteSpace(output) ? "Tool ran successfully (no output)." : output;
    }

    private static Dictionary<string, MarkdownSkillLoader.SkillDef> ToSkillMap(ISkillProvider provider) =>
        provider.Skills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>Same result shape as <see cref="RunShellCommand"/> so skills read alike whatever their language.</summary>
    private static string FormatSkillResult(ProcessRunner.Result? result, string? notInstalled)
    {
        if (notInstalled is not null) return $"Error: {notInstalled}";
        if (result!.TimedOut) return $"[Timeout] Command execution exceeded {ShellTimeoutMs}ms limit.";

        string output = result.Output.Trim();
        if (result.ExitCode != 0) return $"[Exit Code {result.ExitCode}] Error: {output}";
        return string.IsNullOrWhiteSpace(output) ? "Command executed successfully (no output returned)." : output;
    }

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SkillFileName();

    [GeneratedRegex(@"^\s*##\s*Skill:", RegexOptions.Multiline)]
    private static partial Regex SkillHeader();

    // Shared with BashRun so skills, agent-built tools and the bash toolset all use the same (Git) bash, never the WSL stub.
    private static string ResolveShell() => BashTools.FindBash() ?? (OperatingSystem.IsWindows() ? "bash" : "/bin/bash");

    private static string RunShellCommand(string command, Dictionary<string, string> env, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveShell(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : Environment.CurrentDirectory
        };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add(command);
        foreach (var (key, value) in env) startInfo.Environment[key] = value;

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read asynchronously so a chatty command cannot fill the pipe and deadlock WaitForExit.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(ShellTimeoutMs))
        {
            process.Kill(entireProcessTree: true);
            return $"[Timeout] Command execution exceeded {ShellTimeoutMs}ms limit.";
        }

        string output = stdout.GetAwaiter().GetResult();
        string error = stderr.GetAwaiter().GetResult();

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            return $"[Exit Code {process.ExitCode}] Error: {error.Trim()}";
        }

        return string.IsNullOrWhiteSpace(output) ? "Command executed successfully (no output returned)." : output.Trim();
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"[^A-Za-z0-9_]")]
    private static partial Regex EnvNameSanitizer();
}
