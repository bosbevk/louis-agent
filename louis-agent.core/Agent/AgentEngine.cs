namespace LouisAgent.Core.Agent;

using LouisAgent.Core.Tools.Clock;
using LouisAgent.Core.Agent.Tools;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates one chat turn: the model is given the agent's tools and
/// <see cref="FunctionInvokingChatClient"/> runs the tool-call loop until the model produces a final answer.
/// Provider specifics live entirely behind <see cref="IChatClient"/>.
/// </summary>
public class AgentEngine
{
    private static readonly ILogger Log = AgentLogging.For<AgentEngine>();

    // Room for a sizeable file in one tool call; larger files are written in chunks (see default.md).
    internal const int MaxOutputTokens = LlmClientFactory.MaxOutputTokens;

    internal static string NoToolsNotice => MessageText.Format(ToolMessage.NoToolsNotice);

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
    public event Action? ToolsChanged
    {
        add => _agentBuiltTools.ToolsChanged += value;
        remove => _agentBuiltTools.ToolsChanged -= value;
    }

    /// <summary>The skills folder; <see cref="CreateSkillFile"/> writes into its agent/functions/; null disables skill authoring.</summary>
    public string? SkillsDirectory { get => _skillTools.SkillsDirectory; init => _skillTools.SkillsDirectory = value; }

    /// <summary>Reasoning effort for each turn (the model's thinking is streamed to hosts); null disables thinking.</summary>
    public ReasoningEffort? Thinking { get; init; }

    /// <summary>Rebuilds the skill set from disk for <see cref="ReloadSkills"/>; null disables reloading.</summary>
    public Func<ISkillProvider>? SkillReloader { get => _skillTools.SkillReloader; init => _skillTools.SkillReloader = value; }

    /// <summary>Approved agent-built tools live here (*.tool.md); new ones wait in its 'pending' subfolder.</summary>
    public string? ToolsDirectory => _agentBuiltTools.ToolsDirectory;

    private readonly AgentBuiltTools _agentBuiltTools;

    /// <param name="toolsets">
    /// Objects whose public methods become the agent's tools (plus <see cref="ExecuteSkill"/>): a session's catalogue
    /// toolsets (<c>AgentEngineFactory</c>), or another agent's own tools, such as the orchestrator's. Null registers no
    /// tool object, only <see cref="ExecuteSkill"/> and the self-extension tools.
    /// </param>
    /// <param name="usageSink">Where one usage record per model request goes; null records nothing.</param>
    /// <param name="usageMapper">The provider's usage mapping (<c>LlmClientFactory.CreateUsageMapper</c>); null uses the standard one.</param>
    /// <param name="concurrentToolCalls">Run a round's tool calls at once (F34); only for toolsets that allow it.</param>
    /// <param name="selfExtension">
    /// On a toolsets engine, also register the self-extension tools (CreateSkillFile, ReloadSkills, CreateTool,
    /// ListAgentTools): the <c>skills</c> toolset. Ignored when <paramref name="toolsets"/> is null, which always registers them.
    /// </param>
    public AgentEngine(ISkillProvider skillProvider, IChatClient chatClient, AgentOptions options, bool supportsTools = true,
        IEnumerable<object>? toolsets = null,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null, bool selfExtension = false, bool concurrentToolCalls = false)
    {
        _options = options;
        SupportsTools = supportsTools;
        WorkspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        _python = new PythonTools(WorkspaceRoot);
        _powerShell = new PowerShellTools(WorkspaceRoot);
        _bash = new BashTools(WorkspaceRoot);
        _skillTools = new SkillTools(skillProvider, options, WorkspaceRoot, _python, _powerShell);
        _agentBuiltTools = new AgentBuiltTools(_registry, supportsTools, WorkspaceRoot, _python, _powerShell, () => SkillsDirectory);

        // Summaries call the model directly, outside the tool loop, so they are recorded by their own recorder.
        RecordsUsage = usageSink is not null;
        _innerChatClient = usageSink is null ? chatClient : new UsageRecordingChatClient(chatClient, usageSink, mapper: usageMapper);
        _guards = new ToolCallGuards(_registry, _innerChatClient, options.SubSessionPromptBudget);
        var pipeline = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation(configure: c =>
            {
                c.MaximumIterationsPerRequest = 10;
                // F34: a round's calls at once, for an engine whose toolsets all allow it (ToolsetCatalog.RunsInParallel).
                c.AllowConcurrentInvocation = concurrentToolCalls;
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
            Log.Log(LogLevel.Warning, LogMessage.ModelWithoutTools);
            return;
        }

        _registry.Add(Function(_skillTools.ExecuteSkill, nameof(ExecuteSkill)));
        // Today's date and time (F22): every session, whatever its toolsets, since a model doesn't know what day it is.
        _registry.AddPublicMethods(new ClockTools());
        if (toolsets is not null)
        {
            foreach (object toolset in toolsets) _registry.AddPublicMethods(toolset);
            if (selfExtension) AddSelfExtensionTools();
            return;
        }

        AddSelfExtensionTools();
    }

    /// <summary>
    /// Keeps the one tool named and drops every other, the clock and <see cref="ExecuteSkill"/> included (F30: a minion
    /// gets one task and one tool, so its prompt lists exactly what it may call). Before the engine's first turn.
    /// </summary>
    public void KeepOnlyTool(string name)
    {
        foreach (var other in _registry.Live.Where(t => !string.Equals(t.Name, name, StringComparison.Ordinal)).ToList())
            _registry.Remove(other);
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
            Log.Log(LogLevel.Warning, LogMessage.McpCannotRegister, new { Name = name }, tag: "MCP");
            return false;
        }

        return _registry.RegisterDiscovered(name, description);
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

            Log.Log(LogLevel.Warning, LogMessage.ReplyCutOff, new { MaxOutputTokens });
            ToolCallRecords.TruncatedToolCall("(streamed reply)", string.Empty, MaxOutputTokens);
            history.Add(new ChatMessage(ChatRole.User, ContinueAfterCutOffMessage));
            // The rest of this turn's requests exist only because of the cut-off; the host's scope says so.
            if (UsageScope.Current is { } scope) scope.Purpose = UsagePurpose.Continue;
        }
    }

    private const int MaxCutOffContinuations = 3;

    internal static string ContinueAfterCutOffMessage => MessageText.Format(ToolMessage.ContinueAfterCutOff);

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

    /// <summary>Builds an agent tool (the <c>CreateTool</c> tool; see <see cref="AgentBuiltTools.CreateTool"/>).</summary>
    public string CreateTool(string definition, bool overwrite = false) => _agentBuiltTools.CreateTool(definition, overwrite);

    /// <summary>The agent-built tools (the <c>ListAgentTools</c> tool; see <see cref="AgentBuiltTools.ListAgentTools"/>).</summary>
    public string ListAgentTools() => _agentBuiltTools.ListAgentTools();

    /// <summary>Loads approved tools from <see cref="ToolsDirectory"/>. Called once by the host after construction.</summary>
    public int LoadApprovedTools() => _agentBuiltTools.LoadApprovedTools();

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
            "/tools" => ListAgentTools() + PendingSkillFileLines() + MessageText.Format(UserText.ToolsHint),
            "/approve" => Pending(argument, _agentBuiltTools.ApproveTool, _skillTools.ApproveSkillFile),
            "/reject" => Pending(argument, _agentBuiltTools.RejectTool, _skillTools.RejectSkillFile),
            "/usage" => sessionUsage is null ? MessageText.Format(UserText.UsageNotTracked) : UsageText.Summary(sessionUsage, Display),
            _ => null,
        };
    }

    /// <summary>
    /// /approve and /reject reach an agent-built tool or a skill file the agent wrote (FX20), by name: a tool when one
    /// is pending, else the skill file, else the tool's own "nothing pending" reply; a name that is both is refused.
    /// </summary>
    private string Pending(string name, Func<string, string> onTool, Func<string, string> onSkillFile)
    {
        bool tool = !string.IsNullOrWhiteSpace(name) && _agentBuiltTools.HasPendingTool(name);
        bool skillFile = !string.IsNullOrWhiteSpace(name) && _skillTools.HasPendingSkillFile(name);
        if (tool && skillFile)
            return MessageText.Format(UserText.PendingToolAndSkillFile, new { Name = name, Tool = $"{name}.tool.md", File = $"{name}.md" });
        return skillFile ? onSkillFile(name) : onTool(name);
    }

    private string PendingSkillFileLines() =>
        _skillTools.ListPendingSkillFiles() is { Length: > 0 } lines ? "\n" + lines : "";

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
    
    /// <summary>The tools that let the agent extend itself: skills and agent-built tools (toolset "skills", F5).</summary>
    private void AddSelfExtensionTools()
    {
        _registry.Add(Function(_skillTools.CreateSkillFile, nameof(CreateSkillFile)));
        _registry.Add(Function(_skillTools.ReloadSkills, nameof(ReloadSkills)));
        _registry.Add(Function(_agentBuiltTools.CreateTool, nameof(CreateTool)));
        _registry.Add(Function(_agentBuiltTools.ListAgentTools, nameof(ListAgentTools)));
    }
}
