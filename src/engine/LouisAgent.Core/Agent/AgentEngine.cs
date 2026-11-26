using LouisAgent.Composition.Logging;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Commands;
using LouisAgent.Core.Agent.EngineTools;
using LouisAgent.Core.Agent.EngineTools.Skills;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Agent.Turns.Pacing;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Thinking;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers.Agent.Door;
using LouisAgent.Core.Skills.Headers.Agent.Library;
using LouisAgent.Core.Skills.Headers.Agent.Running;
using LouisAgent.Composition.Tools.Scripts;
using LouisAgent.Core.Agent.Turns.Guards;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent;

/// <summary>
/// One agent: its tools, its skills and the model behind <see cref="IChatClient"/>, wired once (S, CL15: the engine is
/// the facade). A turn runs on <see cref="TurnRunner"/> over the <see cref="ChatPipeline"/>, where
/// <see cref="FunctionInvokingChatClient"/> runs the tool-call loop until the model produces a final answer; what the
/// user types as a command is <see cref="UserCommands"/>'. Provider specifics live entirely behind
/// <see cref="IChatClient"/>.
/// </summary>
public class AgentEngine
{
    /// <summary>The engine's own lines: a model without tools.</summary>
    private static readonly ILogger Log = AgentLogging.For<AgentEngine>();

    /// <summary>The skills: the system prompt, running one, writing and reloading them.</summary>
    private readonly SkillTools _skillTools;

    /// <summary>The tools the model is offered.</summary>
    private readonly ToolRegistry _registry;

    /// <summary>What stops a cut-off call and summarises an oversized result.</summary>
    private readonly ToolCallGuards _guards;

    /// <summary>One turn on the pipeline.</summary>
    private readonly TurnRunner _turns;

    /// <summary>
    /// Whether a turn's text is paced on its way out (STREAM_PACE, F155), and the clock that paces it.
    /// </summary>
    private readonly bool _paced;

    private readonly TimeProvider _clock;

    /// <summary>What the user types that the model never sees.</summary>
    private readonly UserCommands _commands;

    /// <summary>What a chat resumed on moved tools does with its saved thinking: the provider's way.</summary>
    private readonly IStaleThinking _staleThinking;

    /// <summary>Combined default instructions and skill documentation used as the system prompt.</summary>
    public string SkillDocumentation => _skillTools.Documentation;

    /// <summary>
    /// Which agent this is, as a trace names it (F52): the factory gives a role's engine its role and a session's
    /// engine the default mode; one built by hand is named for its host.
    /// </summary>
    public string Name { get; init; } = AgentLogging.ServiceName;

    /// <summary>The folder the agent's file and git tools work in.</summary>
    public string WorkspaceRoot { get; }

    /// <summary>Whether the model takes tools; without, it gets none and is told so.</summary>
    public bool SupportsTools { get; }

    /// <summary>
    /// Whether model requests are recorded (a usage sink was given). Hosts show usage only then; otherwise /usage says
    /// usage isn't tracked rather than "nothing yet".
    /// </summary>
    public bool RecordsUsage { get; }

    /// <summary>
    /// The currency usage lines and /usage show costs in (F14), set by <c>AgentHost.Build</c>; null shows them as
    /// charged. Hosts give it to their <see cref="UsageSession"/>s.
    /// </summary>
    public DisplayCurrency? Display { get; set; }

    /// <summary>The tools the model is offered now, agent-built ones included.</summary>
    public IReadOnlyList<AITool> Tools => _registry.Live;

    /// <summary>The tool-call guards in this engine's pipeline (tests call them directly).</summary>
    internal ToolCallGuards Guards => _guards;

    /// <summary>The currently loaded skills (refreshed by <see cref="ReloadSkills"/>).</summary>
    public IReadOnlyCollection<MarkdownSkillLoader.SkillDef> Skills => _skillTools.Skills;

    /// <summary>
    /// Raised after agent-built tools are added or removed, so hosts that publish <see cref="Tools"/> can resync.
    /// </summary>
    public event Action? ToolsChanged
    {
        add => _agentBuiltTools.ToolsChanged += value;
        remove => _agentBuiltTools.ToolsChanged -= value;
    }

    /// <summary>
    /// The skills folder; <see cref="CreateSkillFile"/> writes into its agents/functions/; null disables skill
    /// authoring.
    /// </summary>
    public string? SkillsDirectory
    {
        get => _skillTools.SkillsDirectory;
        init => _skillTools.SkillsDirectory = value;
    }

    /// <summary>
    /// Reasoning effort for each turn (the model's thinking is streamed to hosts); null disables thinking.
    /// </summary>
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

    /// <summary>The tools the agent wrote itself, approved or waiting.</summary>
    private readonly AgentBuiltTools _agentBuiltTools;

    /// <summary>
    /// The skills in the skills folder, read when first needed, since the folder is set after the engine is built
    /// (F97.1).
    /// </summary>
    private readonly Lazy<SkillLibrary> _library;

    /// <summary>Builds the agent's parts once and registers its tools; a model without tools gets none.</summary>
    /// <param name="toolsets">
    /// Objects whose public methods become the agent's tools (plus <see cref="ExecuteSkill"/>): a session's catalogue
    /// toolsets (<c>AgentEngineFactory</c>), or another agent's own tools, such as the orchestrator's. Null registers
    /// no tool object, only <see cref="ExecuteSkill"/> and the self-extension tools.
    /// </param>
    /// <param name="usageSink">Where one usage record per model request goes; null records nothing.</param>
    /// <param name="usageMapper">
    /// The provider's usage mapping (<c>LlmClientFactory.CreateUsageMapper</c>); null uses the standard one.
    /// </param>
    /// <param name="concurrentToolCalls">
    /// Run a round's tool calls at once (F34); only for toolsets that allow it.
    /// </param>
    /// <param name="staleThinking">
    /// The provider's way with a resumed chat's thinking once its tools moved
    /// (<c>LlmClientFactory.CreateStaleThinking</c>); null strips it, which every provider takes.
    /// </param>
    /// <param name="selfExtension">
    /// On a toolsets engine, also register the self-extension tools (CreateSkillFile, ReloadSkills, CreateTool,
    /// ListAgentTools): the <c>skills</c> toolset. Ignored when <paramref name="toolsets"/> is null, which always
    /// registers them.
    /// </param>
    public AgentEngine(ISkillProvider skillProvider, IChatClient chatClient, AgentOptions options,
        bool supportsTools = true,
        IEnumerable<object>? toolsets = null,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null, bool selfExtension = false,
        bool concurrentToolCalls = false, TimeProvider? clock = null, Sessions.Replay.Store.ReplayStore? replay = null,
        IStaleThinking? staleThinking = null)
    {
        SupportsTools = supportsTools;
        _staleThinking = staleThinking ?? StripStaleThinking.Instance;
        WorkspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        _registry = new ToolRegistry(supportsTools, replay);
        var python = new PythonRunner(WorkspaceRoot);
        var powerShell = new PowerShellRunner(WorkspaceRoot);
        _skillTools = new SkillTools(skillProvider, options, WorkspaceRoot, python, powerShell);
        _agentBuiltTools = new AgentBuiltTools(_registry, supportsTools, WorkspaceRoot, python, powerShell,
            () => SkillsDirectory, () => ReservedToolNames);
        // The one door to every skill (F97.1): the library and the door are made when first used.
        _library = new Lazy<SkillLibrary>(() => SkillLibrary.Read(SkillsDirectory ?? ""));
        _commands = new UserCommands(_agentBuiltTools, _skillTools, () => _library.Value, () => Display);
        var skillRunner = SkillRunner.For(python, powerShell, new BashRunner(WorkspaceRoot));
        var door = new Lazy<SkillDoor>(() => new SkillDoor(_library.Value, skillRunner));
        var doorTools = new SkillDoorTools(() => door.Value);

        RecordsUsage = usageSink is not null;
        _guards = new ToolCallGuards(_registry,
            ChatPipeline.BuildSummaryClient(chatClient, usageSink, usageMapper, replay),
            options.SubSessionPromptBudget);
        _paced = options.StreamPace;
        _clock = clock ?? TimeProvider.System;
        _turns = new TurnRunner(
            ChatPipeline.Build(chatClient, _guards, usageSink, usageMapper, concurrentToolCalls, replay), _registry,
            () => SkillDocumentation, () => Thinking, supportsTools, () => Name, _staleThinking);

        if (!supportsTools)
        {
            Log.Log(LogLevel.Warning, AgentLogMessage.ModelWithoutTools);
            return;
        }

        var listing = () => SkillListing.LinesFor(_library.Value.Approved);
        OwnTools.Register(_registry, _skillTools, _agentBuiltTools, OwnTools.Door(doorTools, listing), options,
            toolsets, selfExtension, clock);
    }

    /// <summary>
    /// Starts a builder for an agent: its skills, its model and its settings are given here, and each other part is a
    /// fluent call on <see cref="AgentEngineBuilder"/>. Prefer it to the constructor, which it calls with the defaults.
    /// </summary>
    public static AgentEngineBuilder CreateBuilder(ISkillProvider skillProvider, IChatClient chatClient,
        AgentOptions options) =>
        new(skillProvider, chatClient, options);

    /// <summary>
    /// Keeps the one tool named and drops every other, for a lookup agent's one task (F30; see
    /// <see cref="ToolRegistry.KeepOnly"/>).
    /// </summary>
    public void KeepOnlyTool(string name) => _registry.KeepOnly(name);

    /// <summary>
    /// Registers a tool discovered outside, e.g. in Rider's MCP; false when it's a duplicate or tools are off.
    /// </summary>
    public bool RegisterDiscoveredTool(string name, string description) =>
        _registry.RegisterDiscovered(name, description);

    /// <summary>
    /// A new chat, opening with the system prompt, and the no-tools notice when the model takes none.
    /// </summary>
    public Conversation StartConversation() => _turns.NewConversation();

    /// <summary>
    /// A saved chat brought back: it opens with the system prompt it was saved with, not today's, and sends today's
    /// tools, knowing whether they differ from those its saved turns were sent with. The host adds the turns after.
    /// </summary>
    /// <param name="systemPrompt">The system prompt the chat was saved with.</param>
    /// <param name="savedTools">The mark of the tools its saved turns were sent with; none when it had no turn.</param>
    /// <param name="savedMessages">Its saved turns' messages, in order.</param>
    public Conversation ResumeConversation(string systemPrompt, ToolsFingerprint? savedTools,
        IReadOnlyList<ChatMessage> savedMessages)
    {
        var conversation = _turns.ResumedConversation(systemPrompt, savedTools);
        // Thinking saved under other tools may no longer match them; the provider's strategy says what to send.
        var said = conversation.ToolsChangedSinceSaved ? _staleThinking.Restore(savedMessages) : savedMessages;
        conversation.AddRange(said);
        return conversation;
    }

    /// <summary>
    /// One prompt, the reply whole (<see cref="TurnRunner.ProcessAsync"/>): the turn, tool calls included, lands in
    /// <paramref name="conversation"/>.
    /// </summary>
    public Task<string> ProcessPromptAsync(Conversation conversation, string userInput,
        CancellationToken cancellationToken = default) =>
        _turns.ProcessAsync(conversation, userInput, cancellationToken);

    /// <summary>
    /// One prompt, the reply streamed (<see cref="TurnRunner.StreamAsync"/>): text, each tool call and its outcome as
    /// they happen; the text at a steady pace in whole words when the host turned STREAM_PACE on (F155), the turn
    /// itself and its conversation untouched by it.
    /// </summary>
    public IAsyncEnumerable<ChatResponseUpdate> StreamPromptAsync(Conversation conversation, string userInput,
        CancellationToken cancellationToken = default)
    {
        var turn = _turns.StreamAsync(conversation, userInput, cancellationToken);
        return _paced ? turn.Paced(_clock, cancellationToken) : turn;
    }

    /// <summary>
    /// Runs a loaded skill (the <c>ExecuteSkill</c> tool; see <see cref="SkillTools.ExecuteSkill"/>).
    /// </summary>
    public string ExecuteSkill(string skillName, string jsonArgs = "{}") =>
        _skillTools.ExecuteSkill(skillName, jsonArgs);

    /// <summary>
    /// Saves and loads a skills file (the <c>CreateSkillFile</c> tool; see <see cref="SkillTools.CreateSkillFile"/>).
    /// </summary>
    public string CreateSkillFile(string name, string markdown, bool overwrite = false) =>
        _skillTools.CreateSkillFile(name, markdown, overwrite);

    /// <summary>
    /// Re-reads the skills from disk (the <c>ReloadSkills</c> tool; see <see cref="SkillTools.ReloadSkills"/>).
    /// </summary>
    public string ReloadSkills() => _skillTools.ReloadSkills();

    /// <summary>
    /// Builds an agent tool (the <c>CreateTool</c> tool; see <see cref="AgentBuiltTools.CreateTool"/>).
    /// </summary>
    public string CreateTool(string definition, bool overwrite = false) =>
        _agentBuiltTools.CreateTool(definition, overwrite);

    /// <summary>
    /// The agent-built tools (the <c>ListAgentTools</c> tool; see <see cref="AgentBuiltTools.ListAgentTools"/>).
    /// </summary>
    public string ListAgentTools() => _agentBuiltTools.ListAgentTools();

    /// <summary>
    /// Loads approved tools from <see cref="ToolsDirectory"/>. Called once by the host after construction.
    /// </summary>
    public int LoadApprovedTools() => _agentBuiltTools.LoadApprovedTools();

    /// <summary>
    /// Handles commands typed by the user (never exposed to the model): /tools, /approve &lt;Name&gt;,
    /// /reject &lt;Name&gt;, /usage (<see cref="UserCommands"/>). Returns the reply, or null when the input is not one
    /// of these commands.
    /// </summary>
    /// <param name="sessionUsage">
    /// The session's usage records so far, for /usage (F3); null when the host doesn't track usage.
    /// </param>
    public string? HandleUserCommand(string input, IReadOnlyList<UsageRecord>? sessionUsage = null) =>
        _commands.Handle(input, sessionUsage);

    /// <summary>
    /// What the model is sent must be the same bytes on every machine. Text read from the skill files, and a
    /// [Description] written as a multi-line raw string, take the line endings of the checkout (CRLF on Windows with
    /// autocrlf, LF on Linux); everything the engine sends uses LF.
    /// </summary>
    internal static string Normalise(string text) => text.ReplaceLineEndings("\n");
}
