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
using LouisAgent.Composition.Tools.Scripts;
using LouisAgent.Core.Agent.Turns.Guards;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Headers.Agent.Door;
using LouisAgent.Core.Prompting.Headers.Agent.Drafting;
using LouisAgent.Core.Prompting.Headers.Agent.Library;
using LouisAgent.Core.Prompting.Headers.Agent.Running;
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

    /// <summary>The reload of the skills folder, the model's <c>ReloadSkills</c> and the engine's.</summary>
    private readonly SkillReloadTools _reload;

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

    /// <summary>
    /// The system prompt: the principles, the role, the identity's skills and the guides, read once when the engine
    /// is built, since all of it is built in.
    /// </summary>
    public string SkillDocumentation { get; }

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

    /// <summary>
    /// Raised when a tool's description changes, so hosts that publish <see cref="Tools"/> can resync: since F97.1, an
    /// approval or a reload changes the approved skills <c>UseSkill</c> lists.
    /// </summary>
    public event Action? ToolsChanged
    {
        add => _library.Value.Changed += value;
        remove => _library.Value.Changed -= value;
    }

    /// <summary>
    /// The skills folder: the approved skills <c>UseSkill</c> reaches, and the drafts <c>CreateSkill</c> writes into
    /// its <c>drafts/</c>; null leaves the agent no skills of its own.
    /// </summary>
    public string? SkillsDirectory { get; init; }

    /// <summary>
    /// Reasoning effort for each turn (the model's thinking is streamed to hosts); null disables thinking.
    /// </summary>
    public ReasoningEffort? Thinking { get; init; }

    /// <summary>
    /// The skills in the skills folder, read when first needed, since the folder is set after the engine is built
    /// (F97.1).
    /// </summary>
    private readonly Lazy<SkillLibrary> _library;

    /// <summary>Builds the agent's parts once and registers its tools; a model without tools gets none.</summary>
    /// <param name="toolsets">
    /// Objects whose public methods become the agent's tools (plus the clock): a session's catalogue toolsets
    /// (<c>AgentEngineFactory</c>), or another agent's own tools, such as the orchestrator's. Null registers no tool
    /// object, only the clock and the self-extension tools.
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
    /// On a toolsets engine, also register the self-extension tools (UseSkill, CreateSkill, ReloadSkills): the
    /// <c>skills</c> toolset. Ignored when <paramref name="toolsets"/> is null, which always
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
        SkillDocumentation = Normalise(skillProvider.Documentation);
        // The one door to every skill (F97.1): the library and the door are made when first used.
        _library = new Lazy<SkillLibrary>(() => SkillLibrary.Read(SkillsDirectory ?? ""));
        _commands = new UserCommands(() => _library.Value, () => Display);
        var skillRunner = SkillRunner.For(python, powerShell, new BashRunner(WorkspaceRoot));
        var door = new Lazy<SkillDoor>(() => new SkillDoor(_library.Value, skillRunner));
        var doorTools = new SkillDoorTools(() => door.Value);
        var making = new SkillMakingTools(() => new SkillDrafting(SkillsDirectory ?? "", skillRunner));
        _reload = new SkillReloadTools(() => _library.Value.Reload());

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

        var agentSkills = OwnTools.AgentSkills(doorTools, () => SkillListing.LinesFor(_library.Value.Approved),
            making, _reload);
        OwnTools.Register(_registry, agentSkills, options, toolsets, selfExtension, clock);
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
    /// Reads the skills folder again (the <c>ReloadSkills</c> tool, F97.1), so a skill dropped in by hand is reachable
    /// now and listed in the next chat.
    /// </summary>
    public string ReloadSkills() => _reload.ReloadSkills();

    /// <summary>
    /// Handles commands typed by the user (never exposed to the model): /skills, /approve &lt;Name&gt;,
    /// /reject &lt;Name&gt;, /usage (<see cref="UserCommands"/>). Returns the reply, or null when the input is not one
    /// of these commands.
    /// </summary>
    /// <param name="sessionUsage">
    /// The session's usage records so far, for /usage (F3); null when the host doesn't track usage.
    /// </param>
    /// <param name="chat">
    /// The chat the command was typed in, which tells its model what /approve or /reject did.
    /// </param>
    public string? HandleUserCommand(string input, IReadOnlyList<UsageRecord>? sessionUsage = null,
        Conversation? chat = null) =>
        _commands.Handle(input, sessionUsage, chat);

    /// <summary>
    /// What the model is sent must be the same bytes on every machine. Text read from the skill files, and a
    /// [Description] written as a multi-line raw string, take the line endings of the checkout (CRLF on Windows with
    /// autocrlf, LF on Linux); everything the engine sends uses LF.
    /// </summary>
    internal static string Normalise(string text) => text.ReplaceLineEndings("\n");
}
