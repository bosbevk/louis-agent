using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Hosting.Engines;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.EngineTools.Skills;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Hosting.Messages;

namespace LouisAgent.Core.Hosting;

/// <summary>
/// One engine per toolset profile (F5): a session names its toolsets, and gets an engine with only those tools and
/// only their guides in its system prompt. Engines are cached by their normalised list, its role, its level
/// (F21, CL8), the LLM profile it names and, for a minion, its one tool (F30), so every session with the same five
/// shares one engine, one tool list and one cacheable prefix. The model client and thinking are the instance's
/// unless the session names a profile; the usage ledger and the display currency are shared by all of them. Each
/// engine is named for its role, or the default mode, so a trace tells the agents apart (F52).
/// </summary>
/// <remarks>
/// In <c>Hosting/</c>, not <c>Agent/</c>: it loads the skills through <see cref="AgentHost"/>, a layer above the
/// engine.
/// </remarks>
public sealed class AgentEngineFactory
{
    /// <summary>
    /// How many profiles one instance keeps cached; past it the least recently used is dropped (a session keeps its own
    /// engine, so dropping one never breaks a chat; the next session with that list builds it again).
    /// </summary>
    public const int DefaultCapacity = 16;

    /// <summary>The factory's own lines: a profile built, with its size.</summary>
    private static readonly ILogger Log = AgentLogging.For<AgentEngineFactory>();

    /// <summary>The instance's model client.</summary>
    private readonly IChatClient _chatClient;

    /// <summary>The instance's settings.</summary>
    private readonly AgentOptions _agent;

    /// <summary>Whether the instance's model takes tools.</summary>
    private readonly bool _supportsTools;

    /// <summary>The usage ledger every engine records into; none records nothing.</summary>
    private readonly IUsageSink? _usageSink;

    /// <summary>How the instance's provider reports usage; none uses the standard mapping.</summary>
    private readonly IUsageMapper? _usageMapper;

    /// <summary>The currency every engine shows costs in.</summary>
    private readonly DisplayCurrency? _display;

    /// <summary>The instance's thinking, for engines on its own model.</summary>
    private readonly ReasoningEffort? _thinking;

    /// <summary>The LLM profiles a session may name, by name.</summary>
    private readonly Dictionary<string, SessionLlm> _sessionLlms;

    /// <summary>The engines built so far, least recently used dropped first.</summary>
    private readonly EngineCache _engines;

    /// <summary>The replay's clock, shared by every engine of the process; none outside a replay.</summary>
    private readonly TimeProvider? _clock;

    /// <summary>Where recordings are kept and replayed from; none when nothing is recorded.</summary>
    private readonly ReplayStore? _replay;

    /// <summary>
    /// The factory over the instance's model, settings, ledger, prices and packs: one store, one browser and one
    /// clock for every engine it builds.
    /// </summary>
    public AgentEngineFactory(IChatClient chatClient, AgentOptions agent, bool supportsTools = true,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null, DisplayCurrency? display = null,
        ReasoningEffort? thinking = null, HostServices? services = null, int capacity = DefaultCapacity,
        ExchangeRates? rates = null, IReadOnlyList<SessionLlm>? sessionLlms = null, AgentCatalog? catalog = null)
    {
        Catalog = catalog ?? AgentCatalog.CoreOnly;
        _chatClient = chatClient;
        _agent = agent;
        _agent.DefaultVoice ??= Catalog.DefaultVoice;
        _supportsTools = supportsTools;
        _usageSink = usageSink;
        _usageMapper = usageMapper;
        _display = display;
        _thinking = thinking;
        _engines = new EngineCache(capacity);
        _sessionLlms = (sessionLlms ?? []).ToDictionary(l => l.Name, StringComparer.Ordinal);
        // One store where recordings are kept for every engine of this process (F18 step 4b), and one clock, so a
        // trial's frozen time is shared by every engine and sub-session. The web (one browser, one robots policy, the
        // search provider) is the host's to build from them (CL33); without a host's web library it isn't set up.
        var replay = _replay = ReplayStore.From(agent);
        _clock = replay is null ? null : new ReplayClock(replay);
        Context = new ToolsetContext(agent, (services ?? HostServices.None).WebOrNone(agent, replay), rates,
            usageSink, replay)
        { Identities = Catalog.Identities };
    }

    /// <summary>What the catalogue builds this instance's tools from (settings, the search provider).</summary>
    public ToolsetContext Context { get; }

    /// <summary>
    /// The toolsets this instance can load (CL15): core's and its packs'; <see cref="AgentCatalog.CoreOnly"/> when a
    /// host passes none.
    /// </summary>
    public AgentCatalog Catalog { get; }

    /// <summary>Every toolset this instance can load: those whose settings are all set.</summary>
    public IReadOnlyList<string> Available =>
        Catalog.Toolsets.Where(t => t.IsAvailable(Context)).Select(t => t.Name).ToList();

    /// <summary>
    /// The LLM profiles a session can name besides the instance's own model (F30, <c>LLM_SESSION_PROFILES</c>).
    /// </summary>
    public IReadOnlyList<string> SessionLlms => _sessionLlms.Keys.ToList();

    /// <summary>The named roles a session at <paramref name="level"/> can choose (F21, CL8), all built in.</summary>
    public IReadOnlyList<string> AvailableRoles(RoleLevel level) => RoleFiles.Enabled(_agent, level);

    /// <summary>The mode of a level-0 session that names none (CL9): <c>GET /v1/roles</c>' <c>default</c>.</summary>
    public string? DefaultVoice => _agent.DefaultVoice;

    /// <summary>
    /// The profiles built so far: each its toolsets joined with commas, then |role, |level below level 0,
    /// |llm:{profile} and |tool:{name} when set (<see cref="ProfileKey.Text"/>; the cache that holds them is
    /// <see cref="EngineCache"/>).
    /// </summary>
    public IReadOnlyCollection<string> Profiles => _engines.Keys;

    /// <summary>
    /// The engine for <paramref name="toolsets"/> (any order or case; duplicates ignored), <paramref name="role"/>
    /// (null: the default mode) and <paramref name="level"/> (CL8: where the role is; the router only at level 0),
    /// built on first use. Throws <see cref="ToolsetException"/> for an unknown or unavailable toolset and
    /// <see cref="RoleException"/> for an unknown role or one from another level, and
    /// <see cref="SessionLlmException"/> for an LLM profile it doesn't offer (<paramref name="llm"/>, F30; null: the
    /// instance's own model), before anything is built. A full cache drops its least recently used profile (on a
    /// specialist the model chooses the lists, so they are many).
    /// </summary>
    /// <param name="tool">
    /// A minion's one tool (F30): the engine keeps only it, with no toolset guide and no router, so a small model
    /// reads a prompt it can follow. It must be a tool of <paramref name="toolsets"/>, else
    /// <see cref="ToolsetException"/>. Null: every tool of the toolsets.
    /// </param>
    public AgentEngine GetOrCreate(IEnumerable<string>? toolsets, string? role = null,
        RoleLevel level = RoleLevel.Agent,
        string? llm = null, string? tool = null)
    {
        var resolved = Catalog.Resolve(toolsets, Context);
        var key = ProfileKey.Of(resolved, role, level, llm, tool);
        // Checked here, not in Build: a Lazy that throws keeps the exception, and the key would stay broken.
        RoleFiles.FileOf(_agent, key.Role, key.Level);
        var model = FindLlm(key.Llm);
        if (key.Tool is not null && !Catalog.ToolNamesOf(resolved).Contains(key.Tool))
            throw new ToolsetException(MessageText.Format(HostingErrorMessage.ToolNotInToolsets,
                new
                {
                    Tool = key.Tool,
                    Toolsets = resolved.Count == 0 ? "(none)" : string.Join(", ", resolved.Select(t => t.Name))
                }));

        return _engines.GetOrAdd(key, () => Build(resolved, key, model));
    }

    /// <summary>The LLM profile a session names; none for the instance's own model, an unknown one refused.</summary>
    /// <remarks>Guard clauses: no name, a known one, then the refusal.</remarks>
    private SessionLlm? FindLlm(string? llm)
    {
        if (llm is null) return null;
        if (_sessionLlms.TryGetValue(llm, out var model)) return model;
        throw new SessionLlmException(MessageText.Format(HostingErrorMessage.UnknownSessionLlm,
            new
            {
                Name = llm,
                Available = _sessionLlms.Count == 0 ? "none" : string.Join(", ", _sessionLlms.Keys)
            }));
    }

    /// <summary>
    /// Builds a profile's engine: its prompt from its guides, role and level, its tools, its model, and its name; a
    /// minion's keeps its one tool. Logs the profile's size, an estimate of the prefix every request sends.
    /// </summary>
    private AgentEngine Build(IReadOnlyList<ToolsetDefinition> resolved, ProfileKey key, SessionLlm? model)
    {
        var (_, role, level, _, tool) = key;
        bool selfExtension = resolved.Any(t => t.IsEngineProvided);
        // A minion's prompt has no toolset guide (F30): its role says what to do with its one tool.
        var guided = tool is null ? resolved : [];
        // A session's own model (F30) brings its provider's client, usage mapping, tool support and thinking; the
        // ledger, prices and display currency are the instance's.
        // ReadSkill comes only with an identity that has a skill to read on demand, and holds only its skills (F129).
        var tools = ToolsetCatalog.BuildTools(resolved, Context);
        var skills = new SkillReadTools(SkillLoader.SkillsOf(_agent, role, level));
        var engine = new AgentEngine(SkillLoader.Load(_agent, guided, role, level), model?.Client ?? _chatClient,
            _agent,
            model?.SupportsTools ?? _supportsTools, toolsets: skills.HasAny ? [.. tools, skills] : tools, _usageSink,
            model is null ? _usageMapper : model.UsageMapper, selfExtension, ToolsetCatalog.RunsInParallel(resolved),
            clock: _clock, replay: _replay)
        {
            SkillsDirectory = SkillFolder.Find(_agent),
            // An agent-built tool may take no name of the catalogue's, a pack's included (CL15).
            ReservedToolNames = Catalog.ToolNames,
            // ReloadSkills reloads this profile's prompt (its guides, role and router), never another's.
            SkillReloader = () => SkillLoader.Load(_agent, guided, role, level),
            Thinking = model is null ? _thinking : model.Thinking,
            Display = _display,
            // A trace names the agent (F52): the role it plays, or the mode a session without one runs.
            Name = role ?? DefaultVoice ?? AgentLogging.ServiceName,
        };
        // Approved agent-built tools come with the skills toolset (CreateTool, /approve).
        if (selfExtension) engine.LoadApprovedTools();
        if (tool is not null) engine.KeepOnlyTool(tool);

        string toolsJson = JsonSerializer.Serialize(engine.Tools.OfType<AIFunction>()
            .Select(t => new
            {
                t.Name,
                t.Description,
                t.JsonSchema
            }));
        Log.Log(LogLevel.Information, HostingLogMessage.ToolsetProfileBuilt, new
        {
            Toolsets = (resolved.Count == 0 ? "(none)" : string.Join(", ", resolved.Select(t => t.Name)))
                       + (role is null ? "" : $" · role {role}") +
                       (level == RoleLevel.Agent ? "" : $" · {level.Wire()}")
                       + (model is null ? "" : $" · llm {model.Name}") + (tool is null ? "" : $" · tool {tool}"),
            Tools = engine.Tools.Count,
            PromptChars = engine.SkillDocumentation.Length,
            // About four characters a token: an estimate of the fixed prefix every request of this profile sends.
            PrefixTokens = (engine.SkillDocumentation.Length + toolsJson.Length) / 4,
        });
        return engine;
    }
}
