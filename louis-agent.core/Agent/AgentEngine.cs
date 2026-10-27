
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Commands;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent;
/// <summary>
/// One agent: its tools, its skills and the model behind <see cref="IChatClient"/>, wired once (S, CL15: the engine is the
/// facade). A turn runs on <see cref="TurnRunner"/> over the <see cref="ChatPipeline"/>, where
/// <see cref="FunctionInvokingChatClient"/> runs the tool-call loop until the model produces a final answer; what the
/// user types as a command is <see cref="UserCommands"/>'. Provider specifics live entirely behind <see cref="IChatClient"/>.
/// </summary>
public class AgentEngine
{
    private static readonly ILogger Log = AgentLogging.For<AgentEngine>();

    private readonly SkillTools _skillTools;
    private readonly PythonTools _python;
    private readonly PowerShellTools _powerShell;
    private readonly ToolRegistry _registry = new();
    private readonly ToolCallGuards _guards;
    private readonly TurnRunner _turns;
    private readonly UserCommands _commands;

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
    public string? SkillsDirectory
    {
        get => _skillTools.SkillsDirectory;
        init => _skillTools.SkillsDirectory = value;
    }

    /// <summary>Reasoning effort for each turn (the model's thinking is streamed to hosts); null disables thinking.</summary>
    public ReasoningEffort? Thinking { get; init; }

    /// <summary>Rebuilds the skill set from disk for <see cref="ReloadSkills"/>; null disables reloading.</summary>
    public Func<ISkillProvider>? SkillReloader
    {
        get => _skillTools.SkillReloader;
        init => _skillTools.SkillReloader = value;
    }

    /// <summary>Approved agent-built tools live here (*.tool.md); new ones wait in its 'pending' subfolder.</summary>
    public string? ToolsDirectory => _agentBuiltTools.ToolsDirectory;

    /// <summary>
    /// The names an agent-built tool may not take (CL15): every tool a toolset of the host's catalogue can register,
    /// loaded here or not. Core's by default; <c>AgentEngineFactory</c> sets the catalogue's, so a pack's count too.
    /// </summary>
    public IReadOnlySet<string> ReservedToolNames { get; init; } = AgentCatalog.CoreOnly.ToolNames;

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
    public AgentEngine(ISkillProvider skillProvider, IChatClient chatClient, AgentOptions options,
        bool supportsTools = true,
        IEnumerable<object>? toolsets = null,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null, bool selfExtension = false,
        bool concurrentToolCalls = false, TimeProvider? clock = null, Sessions.Replay.Store.ReplayStore? replay = null)
    {
        SupportsTools = supportsTools;
        WorkspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        _python = new PythonTools(WorkspaceRoot);
        _powerShell = new PowerShellTools(WorkspaceRoot);
        _skillTools = new SkillTools(skillProvider, options, WorkspaceRoot, _python, _powerShell);
        _agentBuiltTools = new AgentBuiltTools(_registry, supportsTools, WorkspaceRoot, _python, _powerShell,
            () => SkillsDirectory, () => ReservedToolNames);
        _commands = new UserCommands(_agentBuiltTools, _skillTools, () => Display);

        RecordsUsage = usageSink is not null;
        _guards = new ToolCallGuards(_registry,
            ChatPipeline.BuildSummaryClient(chatClient, usageSink, usageMapper, replay),
            options.SubSessionPromptBudget);
        _turns = new TurnRunner(
            ChatPipeline.Build(chatClient, _guards, usageSink, usageMapper, concurrentToolCalls, replay), _registry,
            () => SkillDocumentation, () => Thinking, supportsTools);

        if (!supportsTools)
        {
            Log.Log(LogLevel.Warning, AgentLogMessage.ModelWithoutTools);
            return;
        }

        OwnTools.Register(_registry, _skillTools, _agentBuiltTools, options, toolsets, selfExtension, clock);
    }

    /// <summary>
    /// Keeps the one tool named and drops every other, the clock and <see cref="ExecuteSkill"/> included (F30: a minion
    /// gets one task and one tool, so its prompt lists exactly what it may call). Before the engine's first turn.
    /// </summary>
    public void KeepOnlyTool(string name)
    {
        foreach (var other in _registry.Live.Where(t => !string.Equals(t.Name, name, StringComparison.Ordinal))
                     .ToList())
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
            Log.Log(LogLevel.Warning, AgentLogMessage.McpCannotRegister, new { Name = name }, tag: "MCP");
            return false;
        }

        return _registry.RegisterDiscovered(name, description);
    }

    /// <summary>A history seeded with the system prompt (and the no-tools notice when applicable).</summary>
    public List<ChatMessage> NewHistory() => _turns.NewHistory();

    /// <summary>One prompt, the reply whole (<see cref="TurnRunner.ProcessAsync"/>): the turn, tool calls included, lands in <paramref name="history"/>.</summary>
    public Task<string> ProcessPromptAsync(List<ChatMessage> history, string userInput,
        CancellationToken cancellationToken = default) =>
        _turns.ProcessAsync(history, userInput, cancellationToken);

    /// <summary>One prompt, the reply streamed (<see cref="TurnRunner.StreamAsync"/>): text, each tool call and its outcome as they happen.</summary>
    public IAsyncEnumerable<ChatResponseUpdate> StreamPromptAsync(List<ChatMessage> history, string userInput,
        CancellationToken cancellationToken = default) =>
        _turns.StreamAsync(history, userInput, cancellationToken);

    /// <summary>Runs a loaded skill (the <c>ExecuteSkill</c> tool; see <see cref="SkillTools.ExecuteSkill"/>).</summary>
    public string ExecuteSkill(string skillName, string jsonArgs = "{}") =>
        _skillTools.ExecuteSkill(skillName, jsonArgs);

    /// <summary>Saves and loads a skills file (the <c>CreateSkillFile</c> tool; see <see cref="SkillTools.CreateSkillFile"/>).</summary>
    public string CreateSkillFile(string name, string markdown, bool overwrite = false) =>
        _skillTools.CreateSkillFile(name, markdown, overwrite);

    /// <summary>Re-reads the skills from disk (the <c>ReloadSkills</c> tool; see <see cref="SkillTools.ReloadSkills"/>).</summary>
    public string ReloadSkills() => _skillTools.ReloadSkills();

    /// <summary>Builds an agent tool (the <c>CreateTool</c> tool; see <see cref="AgentBuiltTools.CreateTool"/>).</summary>
    public string CreateTool(string definition, bool overwrite = false) =>
        _agentBuiltTools.CreateTool(definition, overwrite);

    /// <summary>The agent-built tools (the <c>ListAgentTools</c> tool; see <see cref="AgentBuiltTools.ListAgentTools"/>).</summary>
    public string ListAgentTools() => _agentBuiltTools.ListAgentTools();

    /// <summary>Loads approved tools from <see cref="ToolsDirectory"/>. Called once by the host after construction.</summary>
    public int LoadApprovedTools() => _agentBuiltTools.LoadApprovedTools();

    /// <summary>
    /// Handles commands typed by the user (never exposed to the model): /tools, /approve &lt;Name&gt;, /reject &lt;Name&gt;,
    /// /usage (<see cref="UserCommands"/>). Returns the reply, or null when the input is not one of these commands.
    /// </summary>
    /// <param name="sessionUsage">The session's usage records so far, for /usage (F3); null when the host doesn't track usage.</param>
    public string? HandleUserCommand(string input, IReadOnlyList<UsageRecord>? sessionUsage = null) =>
        _commands.Handle(input, sessionUsage);

    /// <summary>
    /// What the model is sent must be the same bytes on every machine. Text read from the skill files, and a
    /// [Description] written as a multi-line raw string, take the line endings of the checkout (CRLF on Windows with
    /// autocrlf, LF on Linux); everything the engine sends uses LF.
    /// </summary>
    internal static string Normalise(string text) => text.ReplaceLineEndings("\n");
}
