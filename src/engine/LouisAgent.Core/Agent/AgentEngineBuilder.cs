using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Thinking;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent;

/// <summary>
/// Builds one <see cref="AgentEngine"/>. The three things every agent needs (its skills, its model and its settings)
/// are given to <see cref="AgentEngine.CreateBuilder"/>; each other part is a fluent call, and its default is the one
/// the constructor always used, so an engine built with no call is the same as one built with none of them. Each call
/// returns the builder, so a call site reads as the list of what differs from the defaults.
/// </summary>
/// <remarks>
/// The Gang of Four's Builder (F97.1, Louis's pick, 2026-11-26): the engine's constructor takes a dozen optional parts,
/// which a call site passed by position or set afterwards, so a reader couldn't see which differed from the defaults.
/// Each call here names its part. Chosen over the constructor and its properties alone, as Microsoft's guidelines keep
/// optional settings, since the engine has too many for one call to read well. A call that sets a part twice keeps the
/// last one, and the builder keeps no state after <see cref="Build"/>: a second Build makes another engine.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/builder">Refactoring.Guru: Builder</seealso>
public sealed class AgentEngineBuilder
{
    private readonly ISkillProvider _skillProvider;
    private readonly IChatClient _chatClient;
    private readonly AgentOptions _options;

    private bool _supportsTools = true;
    private IEnumerable<object>? _toolsets;
    private IUsageSink? _usageSink;
    private IUsageMapper? _usageMapper;
    private bool _selfExtension;
    private bool _concurrentToolCalls;
    private TimeProvider? _clock;
    private ReplayStore? _replay;
    private IStaleThinking? _staleThinking;
    private string? _name;
    private ReasoningEffort? _thinking;
    private DisplayCurrency? _display;
    private string? _skillsDirectory;
    private Func<ISkillProvider>? _skillReloader;
    private IReadOnlySet<string>? _reservedToolNames;

    /// <summary>Starts a builder; <see cref="AgentEngine.CreateBuilder"/> is the way to get one.</summary>
    internal AgentEngineBuilder(ISkillProvider skillProvider, IChatClient chatClient, AgentOptions options)
    {
        _skillProvider = skillProvider ?? throw new ArgumentNullException(nameof(skillProvider));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Whether the model takes tools; without, it gets none and is told so. Default: true.</summary>
    public AgentEngineBuilder WithSupportsTools(bool supportsTools)
    {
        _supportsTools = supportsTools;
        return this;
    }

    /// <summary>
    /// Objects whose public methods become the agent's tools (plus <c>ExecuteSkill</c>). Null registers no tool object.
    /// </summary>
    public AgentEngineBuilder WithToolsets(IEnumerable<object>? toolsets)
    {
        _toolsets = toolsets;
        return this;
    }

    /// <summary>
    /// Where one usage record per model request goes, and how the provider reports usage. A null sink records nothing;
    /// a null mapper uses the standard one.
    /// </summary>
    public AgentEngineBuilder WithUsage(IUsageSink? usageSink, IUsageMapper? usageMapper = null)
    {
        _usageSink = usageSink;
        _usageMapper = usageMapper;
        return this;
    }

    /// <summary>
    /// Whether the self-extension tools (CreateSkillFile, ReloadSkills, CreateTool, ListAgentTools) are registered,
    /// on a toolsets engine. Default: false.
    /// </summary>
    public AgentEngineBuilder WithSelfExtension(bool selfExtension)
    {
        _selfExtension = selfExtension;
        return this;
    }

    /// <summary>
    /// Whether a round's tool calls run at once (F34); only for toolsets that allow it. Default: false.
    /// </summary>
    public AgentEngineBuilder WithConcurrentToolCalls(bool concurrentToolCalls)
    {
        _concurrentToolCalls = concurrentToolCalls;
        return this;
    }

    /// <summary>The clock the engine reads time from; null uses the system clock.</summary>
    public AgentEngineBuilder WithClock(TimeProvider? clock)
    {
        _clock = clock;
        return this;
    }

    /// <summary>Where recordings are kept and replayed from; null when nothing is recorded.</summary>
    public AgentEngineBuilder WithReplay(ReplayStore? replay)
    {
        _replay = replay;
        return this;
    }

    /// <summary>
    /// The provider's way with a resumed chat's thinking once its tools moved; null strips it, which every provider
    /// takes.
    /// </summary>
    public AgentEngineBuilder WithStaleThinking(IStaleThinking? staleThinking)
    {
        _staleThinking = staleThinking;
        return this;
    }

    /// <summary>The name a trace gives this agent; null uses the host's service name.</summary>
    public AgentEngineBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    /// <summary>Reasoning effort for each turn; null disables thinking.</summary>
    public AgentEngineBuilder WithThinking(ReasoningEffort? thinking)
    {
        _thinking = thinking;
        return this;
    }

    /// <summary>The currency usage lines and /usage show costs in; null shows them as charged.</summary>
    public AgentEngineBuilder WithDisplay(DisplayCurrency? display)
    {
        _display = display;
        return this;
    }

    /// <summary>The skills folder <c>CreateSkillFile</c> writes into; null disables skill authoring.</summary>
    public AgentEngineBuilder WithSkillsDirectory(string? skillsDirectory)
    {
        _skillsDirectory = skillsDirectory;
        return this;
    }

    /// <summary>Rebuilds the skill set from disk for <c>ReloadSkills</c>; null disables reloading.</summary>
    public AgentEngineBuilder WithSkillReloader(Func<ISkillProvider>? skillReloader)
    {
        _skillReloader = skillReloader;
        return this;
    }

    /// <summary>
    /// The names an agent-built tool may not take. Null uses core's catalogue names; a host passes its own, so a
    /// pack's names are reserved too.
    /// </summary>
    public AgentEngineBuilder WithReservedToolNames(IReadOnlySet<string>? reservedToolNames)
    {
        _reservedToolNames = reservedToolNames;
        return this;
    }

    /// <summary>Builds the engine from the parts given so far.</summary>
    public AgentEngine Build() =>
        new(_skillProvider, _chatClient, _options, _supportsTools, _toolsets, _usageSink, _usageMapper,
            _selfExtension, _concurrentToolCalls, _clock, _replay, _staleThinking)
        {
            Name = _name ?? AgentLogging.ServiceName,
            Thinking = _thinking,
            Display = _display,
            SkillsDirectory = _skillsDirectory,
            SkillReloader = _skillReloader,
            ReservedToolNames = _reservedToolNames ?? AgentCatalog.CoreOnly.ToolNames,
        };
}
