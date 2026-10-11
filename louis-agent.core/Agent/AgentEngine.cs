namespace LouisAgent.Core.Agent;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.DotNet;
using LouisAgent.Core.Tools.Git;
using LouisAgent.Core.Tools.Integrations.DevOps;
using LouisAgent.Core.Tools.Integrations.Paymo;
using LouisAgent.Core.Tools.Scripts;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Workspace;
using LouisAgent.Core.Logging;

/// <summary>
/// Orchestrates one chat turn: the model is given the agent's tools and
/// <see cref="FunctionInvokingChatClient"/> runs the tool-call loop until the model produces a final answer.
/// Provider specifics live entirely behind <see cref="IChatClient"/>.
/// </summary>
public partial class AgentEngine
{
    // Room for a sizeable file in one tool call; larger files are written in chunks (see default.md).
    internal const int MaxOutputTokens = LlmClientFactory.MaxOutputTokens;

    internal const string NoToolsNotice =
        "No tools are available in this session because the configured model does not support tool calling. " +
        "If the user asks you to perform an action (log time, run a skill, read or write files), say plainly that the " +
        "current model cannot do that and suggest switching LLM_MODEL to a tool-capable model. " +
        "Never pretend to have performed an action.";

    private readonly SkillTools _skillTools;
    private readonly PythonTools _python;
    private readonly PowerShellTools _powerShell;
    private readonly BashTools _bash;
    private readonly IChatClient _chatClient;
    private readonly IChatClient _innerChatClient;
    private readonly AgentOptions _options;
    private readonly ToolRegistry _registry = new();
    private readonly ToolCallGuards _guards;

    /// <summary>Combined default instructions and skill documentation used as the system prompt.</summary>
    public string SkillDocumentation => _skillTools.Documentation;
    public string WorkspaceRoot { get; }
    public bool SupportsTools { get; }

    /// <summary>
    /// Whether model requests are recorded (a usage sink was given). Hosts show usage only then; otherwise /usage says
    /// usage isn't tracked rather than "nothing yet".
    /// </summary>
    public bool RecordsUsage { get; }

    /// <summary>
    /// The currency usage lines and /usage show costs in (F14), set by <c>AgentHost.Build</c>; null shows them as charged.
    /// Hosts give it to their <see cref="UsageSession"/>s.
    /// </summary>
    public DisplayCurrency? Display { get; set; }
    public IReadOnlyList<AITool> Tools => _registry.Live;

    /// <summary>The tool-call guards in this engine's pipeline (tests call them directly).</summary>
    internal ToolCallGuards Guards => _guards;

    /// <summary>The currently loaded skills (refreshed by <see cref="ReloadSkills"/>).</summary>
    public IReadOnlyCollection<MarkdownSkillLoader.SkillDef> Skills => _skillTools.Skills;

    /// <summary>Raised after agent-built tools are added or removed, so hosts that publish <see cref="Tools"/> can resync.</summary>
    public event Action? ToolsChanged;

    /// <summary>Folder where <see cref="CreateSkillFile"/> writes new *-skills.md files; null disables skill authoring.</summary>
    public string? SkillsDirectory { get => _skillTools.SkillsDirectory; init => _skillTools.SkillsDirectory = value; }

    /// <summary>Reasoning effort for each turn (the model's thinking is streamed to hosts); null disables thinking.</summary>
    public ReasoningEffort? Thinking { get; init; }

    /// <summary>Rebuilds the skill set from disk for <see cref="ReloadSkills"/>; null disables reloading.</summary>
    public Func<ISkillProvider>? SkillReloader { get => _skillTools.SkillReloader; init => _skillTools.SkillReloader = value; }

    /// <summary>Approved agent-built tools live here (*.tool.md); new ones wait in its 'pending' subfolder.</summary>
    public string? ToolsDirectory => SkillsDirectory is null ? null : Path.Combine(SkillsDirectory, "tools");
    private string? PendingToolsDirectory => ToolsDirectory is null ? null : Path.Combine(ToolsDirectory, "pending");

    private readonly Dictionary<string, ScriptTool> _scriptTools = new(StringComparer.OrdinalIgnoreCase);

    /// <param name="webSearch">Search engine for WebSearch; null selects one from configuration (see <see cref="WebSearchProviderFactory"/>).</param>
    /// <param name="toolsets">
    /// Objects whose public methods become the agent's only tools (plus <see cref="ExecuteSkill"/>), for agents that aren't
    /// coding agents, such as an orchestrator. Null registers the built-in coding toolsets (files, git, dotnet, shells, web,
    /// Paymo, DevOps) and the self-extension tools.
    /// </param>
    /// <param name="usageSink">Where one usage record per model request goes; null records nothing.</param>
    /// <param name="usageMapper">The provider's usage mapping (<c>LlmClientFactory.CreateUsageMapper</c>); null uses the standard one.</param>
    public AgentEngine(ISkillProvider skillProvider, IChatClient chatClient, AgentOptions options, bool supportsTools = true,
        IWebSearchProvider? webSearch = null, IEnumerable<object>? toolsets = null,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null)
    {
        _options = options;
        SupportsTools = supportsTools;
        WorkspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        _python = new PythonTools(WorkspaceRoot);
        _powerShell = new PowerShellTools(WorkspaceRoot);
        _bash = new BashTools(WorkspaceRoot);
        _skillTools = new SkillTools(skillProvider, options, WorkspaceRoot, _python, _powerShell);

        // Summaries call the model directly, outside the tool loop, so they are recorded by their own recorder.
        RecordsUsage = usageSink is not null;
        _innerChatClient = usageSink is null ? chatClient : new UsageRecordingChatClient(chatClient, usageSink, mapper: usageMapper);
        _guards = new ToolCallGuards(_registry, _innerChatClient);
        var pipeline = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation(configure: c =>
            {
                c.MaximumIterationsPerRequest = 10;
                c.FunctionInvoker = _guards.InvokeAsync;
                // Lets the model see why a call failed (e.g. a missing argument) instead of a generic failure.
                c.IncludeDetailedErrors = true;
            })
            .Use(_guards.KeepToolsOnFinalRequestAsync, _guards.KeepToolsOnFinalRequestStreamingAsync);
        // Inside the tool loop, so each round is its own request and record; outside it would see one call per turn.
        if (usageSink is not null)
            pipeline.Use(inner => new UsageRecordingChatClient(inner, usageSink, mapper: usageMapper));
        _chatClient = pipeline
            .Use(ToolCallGuards.MarkTruncatedToolCallsAsync, ToolCallGuards.MarkTruncatedToolCallsStreamingAsync)
            .Build();

        if (!supportsTools)
        {
            Console.Error.WriteLine("[WARN] Model does not support tools; running without them.");
            return;
        }

        _registry.Add(Function(_skillTools.ExecuteSkill, nameof(ExecuteSkill)));
        if (toolsets is not null)
        {
            foreach (object toolset in toolsets) _registry.AddPublicMethods(toolset);
            return;
        }

        _registry.Add(Function(_skillTools.CreateSkillFile, nameof(CreateSkillFile)));
        _registry.Add(Function(_skillTools.ReloadSkills, nameof(ReloadSkills)));
        _registry.Add(Function(CreateTool, nameof(CreateTool)));
        _registry.Add(Function(ListAgentTools, nameof(ListAgentTools)));
        _registry.AddPublicMethods(new WorkspaceTools(WorkspaceRoot));
        _registry.AddPublicMethods(new GitTools(WorkspaceRoot));
        _registry.AddPublicMethods(new DotNetTools(WorkspaceRoot));
        _registry.AddPublicMethods(_python);
        _registry.AddPublicMethods(_powerShell);
        _registry.AddPublicMethods(_bash);
        _registry.AddPublicMethods(new WebTools(webSearch ?? WebSearchProviderFactory.Create(options)));
        if (string.IsNullOrWhiteSpace(options.PaymoApiKey))
        {
            Console.Error.WriteLine("[WARN] PAYMO_API_KEY not set; Paymo tool disabled.");
        }
        else
        {
            _registry.AddPublicMethods(new PaymoTools(options));
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
            _registry.AddPublicMethods(new DevOpsTools(options.DevopsApiKey, options.DevopsOrganization, options.DevopsProject, options.DevopsTeam));
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

        return _registry.RegisterDiscovered(name, description);
    }

    /// <summary>Run the interactive chat loop.</summary>
    public async Task RunAsync()
    {
        var history = NewHistory();
        // One CLI run is one session in the usage ledger.
        string session = $"cli_{Guid.NewGuid():N}";
        int turns = 0;
        var sessionUsage = new UsageSession(Display);

        // Ctrl+C stops the reply in progress; with no reply running it exits as usual.
        CancellationTokenSource? turn = null;
        Console.CancelKeyPress += (_, e) =>
        {
            if (Volatile.Read(ref turn) is not { } active) return;
            e.Cancel = true;
            active.Cancel();
        };

        Console.WriteLine("=== Louis Agent CLI ===");
        Console.WriteLine("Type 'exit' to quit; Ctrl+C stops a reply; /usage shows this session's tokens and cost;");
        Console.WriteLine("/tools, /approve <Name>, /reject <Name> manage agent-built tools\n");

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

            // Approval commands and /usage come from the user only; they never reach the model.
            if (HandleUserCommand(userInput, RecordsUsage ? sessionUsage.Records : null) is { } commandReply)
            {
                Console.WriteLine($"\n{commandReply}\n");
                continue;
            }

            using var cancellation = new CancellationTokenSource();
            Volatile.Write(ref turn, cancellation);
            using var usage = UsageScope.Begin(host: "cli", session: session, turn: ++turns);
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
                // After a stopped or failed reply too: what its requests used was still charged.
                WriteUsageLine(sessionUsage.EndTurn(usage.Records));
            }
        }
    }

    /// <summary>Run a single prompt and exit (non-interactive mode).</summary>
    public async Task RunSinglePromptAsync(string userInput)
    {
        using var usage = UsageScope.Begin(host: "cli", session: $"cli_{Guid.NewGuid():N}", turn: 1);
        try
        {
            await StreamToConsoleAsync(NewHistory(), userInput);
            Console.WriteLine();
            // One prompt is the whole session, so the line has no session total.
            if (usage.Records.Count > 0) WriteUsageLine(UsageText.Line(usage.Records, display: Display));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// The usage line, in grey on stderr: stdout stays the answer alone, so a script reading a single prompt's answer
    /// isn't given usage text.
    /// </summary>
    private static void WriteUsageLine(string? line)
    {
        if (line is null) return;
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Error.WriteLine(line);
        Console.Error.WriteLine();
        Console.ForegroundColor = previous;
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
            // The rest of this turn's requests exist only because of the cut-off; the host's scope says so.
            if (UsageScope.Current is { } scope) scope.Purpose = UsagePurpose.Continue;
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
            chatOptions.Tools = _registry.Live;
            chatOptions.ToolMode = ChatToolMode.Auto;
        }

        return chatOptions;
    }

    /// <summary>Runs a loaded skill (the <c>ExecuteSkill</c> tool; see <see cref="SkillTools.ExecuteSkill"/>).</summary>
    public string ExecuteSkill(string skillName, string jsonArgs = "{}") => _skillTools.ExecuteSkill(skillName, jsonArgs);

    /// <summary>Saves and loads a skills file (the <c>CreateSkillFile</c> tool; see <see cref="SkillTools.CreateSkillFile"/>).</summary>
    public string CreateSkillFile(string name, string markdown, bool overwrite = false) =>
        _skillTools.CreateSkillFile(name, markdown, overwrite);

    /// <summary>Re-reads the skills from disk (the <c>ReloadSkills</c> tool; see <see cref="SkillTools.ReloadSkills"/>).</summary>
    public string ReloadSkills() => _skillTools.ReloadSkills();

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
        else if (_registry.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase))
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

            if (_registry.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase))
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
    /// Handles commands typed by the user (never exposed to the model): /tools, /approve &lt;Name&gt;, /reject &lt;Name&gt;,
    /// /usage. Returns the reply, or null when the input is not one of these commands.
    /// </summary>
    /// <param name="input">What the user typed.</param>
    /// <param name="sessionUsage">
    /// The session's usage records so far, for /usage (F3). The host keeps them: the engine serves every session of a host
    /// and doesn't know which one is asking. Null when the host doesn't track usage.
    /// </param>
    public string? HandleUserCommand(string input, IReadOnlyList<UsageRecord>? sessionUsage = null)
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
            "/usage" => sessionUsage is null ? "Usage isn't tracked in this session." : UsageText.Summary(sessionUsage, Display),
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
        else if (SupportsTools && !_registry.Contains(parsed.Name, StringComparison.OrdinalIgnoreCase))
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
            _registry.Remove(tool!);
            ToolsChanged?.Invoke();
        }

        return hadFile || wasActive
            ? $"Rejected '{name}': deleted the pending definition{(wasActive ? " and removed it from this session" : "")}."
            : $"No pending tool named '{name}'.";
    }

    private void RegisterScriptTool(ScriptToolDefinition definition, bool approved)
    {
        if (_scriptTools.Remove(definition.Name, out var previous)) _registry.Remove(previous);

        var tool = new ScriptTool(definition, approved, RunScriptTool);
        _scriptTools[definition.Name] = tool;
        _registry.Add(tool);
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
            _ => ProcessRunner.Run(BashTools.ResolveShell(), ["-c", definition.Code], WorkspaceRoot, timeout, inputJson, env),
        };

        if (notInstalled is not null) return $"Error: {notInstalled}";
        if (result!.TimedOut) return $"[Timeout] {definition.Name} exceeded {definition.TimeoutSeconds}s and was killed.\n{ProcessRunner.Tail(result.Output)}";

        string output = ProcessRunner.Truncate(result.Output.Trim());
        if (result.ExitCode != 0) return $"[Exit Code {result.ExitCode}] {output}";
        return string.IsNullOrWhiteSpace(output) ? "Tool ran successfully (no output)." : output;
    }

    /// <summary>
    /// What the model is sent must be the same bytes on every machine. Text read from the skill files, and a
    /// [Description] written as a multi-line raw string, take the line endings of the checkout (CRLF on Windows with
    /// autocrlf, LF on Linux); everything the engine sends uses LF.
    /// </summary>
    internal static string Normalise(string text) => text.ReplaceLineEndings("\n");

    /// <summary>One of the engine's own methods as a tool, its [Description] with LF line endings (see <see cref="Normalise"/>).</summary>
    private static AIFunction Function(Delegate method, string name) =>
        AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
        {
            Name = name,
            Description = method.Method.GetCustomAttribute<DescriptionAttribute>() is { } description ? Normalise(description.Description) : null,
        });
}
