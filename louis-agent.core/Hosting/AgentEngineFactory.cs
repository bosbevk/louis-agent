namespace LouisAgent.Core.Hosting;

using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using System.Collections.Concurrent;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Hosting.Messages;

/// <summary>
/// One engine per toolset profile (F5): a session names its toolsets, and gets an engine with only those tools and
/// only their guides in its system prompt. Engines are cached by their normalised list, its role, its level
/// (F21, CL8), the LLM profile it names and, for a minion, its one tool (F30), so every session with the same five shares
/// one engine, one tool list and one cacheable prefix. The model client and thinking are the instance's unless the session names a profile; the
/// usage ledger and the display currency are shared by all of them.
/// </summary>
/// <remarks>
/// In <c>Hosting/</c>, not <c>Agent/</c>: it loads the skills through <see cref="AgentHost"/>, a layer above the engine.
/// </remarks>
public sealed class AgentEngineFactory
{
    /// <summary>
    /// How many profiles one instance keeps cached; past it the least recently used is dropped (a session keeps its own
    /// engine, so dropping one never breaks a chat; the next session with that list builds it again).
    /// </summary>
    public const int DefaultCapacity = 16;

    private static readonly ILogger Log = AgentLogging.For<AgentEngineFactory>();

    private readonly IChatClient _chatClient;
    private readonly AgentOptions _agent;
    private readonly bool _supportsTools;
    private readonly IUsageSink? _usageSink;
    private readonly IUsageMapper? _usageMapper;
    private readonly DisplayCurrency? _display;
    private readonly ReasoningEffort? _thinking;
    private readonly int _capacity;
    private readonly Dictionary<string, SessionLlm> _sessionLlms;
    private readonly ConcurrentDictionary<string, Lazy<AgentEngine>> _engines = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _lastUsed = new(StringComparer.Ordinal);
    private readonly Lock _evicting = new();
    private long _clock;

    public AgentEngineFactory(IChatClient chatClient, AgentOptions agent, bool supportsTools = true,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null, DisplayCurrency? display = null,
        ReasoningEffort? thinking = null, IWebSearchProvider? webSearch = null, int capacity = DefaultCapacity,
        ExchangeRates? rates = null, IReadOnlyList<SessionLlm>? sessionLlms = null, AgentCatalog? catalog = null)
    {
        Catalog = catalog ?? AgentCatalog.CoreOnly;
        _chatClient = chatClient;
        _agent = agent;
        _supportsTools = supportsTools;
        _usageSink = usageSink;
        _usageMapper = usageMapper;
        _display = display;
        _thinking = thinking;
        _capacity = capacity;
        _sessionLlms = (sessionLlms ?? []).ToDictionary(l => l.Name, StringComparer.Ordinal);
        // One browser, and where recordings are kept one store, for every engine of this process (F18 step 4a): the shops and the page renderer read through it.
        Context = new ToolsetContext(agent, webSearch ?? WebSearchProviderFactory.Create(agent), rates, usageSink, WebBrowser: WebBrowserFactory.Create(agent));
    }

    /// <summary>What the catalogue builds this instance's tools from (settings, the search provider).</summary>
    public ToolsetContext Context { get; }

    /// <summary>The toolsets this instance can load (CL15): core's and its packs'; <see cref="AgentCatalog.CoreOnly"/> when a host passes none.</summary>
    public AgentCatalog Catalog { get; }

    /// <summary>Every toolset this instance can load: those whose settings are all set.</summary>
    public IReadOnlyList<string> Available => Catalog.Toolsets.Where(t => t.IsAvailable(Context)).Select(t => t.Name).ToList();

    /// <summary>The LLM profiles a session can name besides the instance's own model (F30, <c>LLM_SESSION_PROFILES</c>).</summary>
    public IReadOnlyList<string> SessionLlms => _sessionLlms.Keys.ToList();

    /// <summary>The named roles a session at <paramref name="level"/> can choose (F21, CL8), all built in.</summary>
    public IReadOnlyList<string> AvailableRoles(RoleLevel level) => RoleFiles.Enabled(_agent, level);

    /// <summary>The mode of a level-0 session that names none (CL9): <c>GET /roles</c>' <c>default</c>.</summary>
    public string DefaultVoice => RoleCatalog.DefaultVoice;

    /// <summary>The profiles built so far: each its toolsets joined with commas, then |role, |level below level 0,
    /// |llm:{profile} and |tool:{name} when set.</summary>
    public IReadOnlyCollection<string> Profiles => _engines.Keys.ToList();

    /// <summary>
    /// The engine for <paramref name="toolsets"/> (any order or case; duplicates ignored), <paramref name="role"/>
    /// (null: the default mode) and <paramref name="level"/> (CL8: where the role is; the router only at level 0),
    /// built on first use. Throws
    /// <see cref="ToolsetException"/> for an unknown or unavailable toolset and <see cref="RoleException"/> for an
    /// unknown role or one from another level, and <see cref="SessionLlmException"/> for an LLM profile it doesn't
    /// offer (<paramref name="llm"/>, F30; null: the instance's own model), before anything is built. A full cache drops
    /// its least recently used profile (on a specialist the model chooses the lists, so they are many).
    /// </summary>
    /// <param name="tool">A minion's one tool (F30): the engine keeps only it, with no toolset guide and no router, so a
    /// small model reads a prompt it can follow. It must be a tool of <paramref name="toolsets"/>, else
    /// <see cref="ToolsetException"/>. Null: every tool of the toolsets.</param>
    public AgentEngine GetOrCreate(IEnumerable<string>? toolsets, string? role = null, RoleLevel level = RoleLevel.Agent,
        string? llm = null, string? tool = null)
    {
        var resolved = Catalog.Resolve(toolsets, Context);
        if (string.IsNullOrEmpty(role)) role = null;
        // Checked here, not in Build: a Lazy that throws keeps the exception, and the key would stay broken.
        RoleFiles.FileOf(_agent, role, level);
        if (string.IsNullOrEmpty(llm)) llm = null;
        SessionLlm? model = null;
        if (llm is not null && !_sessionLlms.TryGetValue(llm, out model))
            throw new SessionLlmException(MessageText.Format(HostingErrorMessage.UnknownSessionLlm,
                new { Name = llm, Available = _sessionLlms.Count == 0 ? "none" : string.Join(", ", _sessionLlms.Keys) }));
        if (string.IsNullOrWhiteSpace(tool)) tool = null;
        if (tool is not null && !Catalog.ToolNamesOf(resolved).Contains(tool))
            throw new ToolsetException(MessageText.Format(HostingErrorMessage.ToolNotInToolsets,
                new { Tool = tool, Toolsets = resolved.Count == 0 ? "(none)" : string.Join(", ", resolved.Select(t => t.Name)) }));

        string key = string.Join(",", resolved.Select(t => t.Name)) + (role is null ? "" : "|" + role) + LevelSuffix(level)
            + (llm is null ? "" : "|llm:" + llm) + (tool is null ? "" : "|tool:" + tool);
        _lastUsed[key] = Interlocked.Increment(ref _clock);
        if (_engines.TryGetValue(key, out var existing)) return existing.Value;

        Lazy<AgentEngine> engine;
        lock (_evicting)
        {
            while (_engines.Count >= _capacity && !_engines.ContainsKey(key))
            {
                string oldest = _engines.Keys.Where(k => k != key).MinBy(k => _lastUsed.GetValueOrDefault(k))!;
                _engines.TryRemove(oldest, out _);
                _lastUsed.TryRemove(oldest, out _);
                Log.Log(LogLevel.Information, HostingLogMessage.ToolsetProfileDropped, new { Toolsets = oldest.Length == 0 ? "(none)" : oldest, Capacity = _capacity });
            }
            engine = _engines.GetOrAdd(key, _ => new Lazy<AgentEngine>(() => Build(resolved, role, level, model, tool)));
        }
        return engine.Value;   // built outside the lock: building one profile doesn't hold up another
    }

    /// <summary>Level 0 adds nothing to the key, so its profiles keep the names they had before CL8.</summary>
    private static string LevelSuffix(RoleLevel level) => level == RoleLevel.Agent ? "" : "|" + level.ToString().ToLowerInvariant();

    private AgentEngine Build(IReadOnlyList<ToolsetDefinition> resolved, string? role, RoleLevel level, SessionLlm? model, string? tool)
    {
        bool selfExtension = resolved.Any(t => t.IsEngineProvided);
        // A minion's prompt has no toolset guide (F30): its role says what to do with its one tool.
        var guided = tool is null ? resolved : [];
        // A session's own model (F30) brings its provider's client, usage mapping, tool support and thinking; the
        // ledger, prices and display currency are the instance's.
        var engine = new AgentEngine(SkillLoader.Load(_agent, guided, role, level), model?.Client ?? _chatClient, _agent,
            model?.SupportsTools ?? _supportsTools, toolsets: ToolsetCatalog.BuildTools(resolved, Context), _usageSink,
            model is null ? _usageMapper : model.UsageMapper, selfExtension, ToolsetCatalog.RunsInParallel(resolved))
        {
            SkillsDirectory = SkillFolder.Find(_agent),
            // An agent-built tool may take no name of the catalogue's, a pack's included (CL15).
            ReservedToolNames = Catalog.ToolNames,
            // ReloadSkills reloads this profile's prompt (its guides, role and router), never another's.
            SkillReloader = () => SkillLoader.Load(_agent, guided, role, level),
            Thinking = model is null ? _thinking : model.Thinking,
            Display = _display,
        };
        // Approved agent-built tools come with the skills toolset (CreateTool, /approve).
        if (selfExtension) engine.LoadApprovedTools();
        if (tool is not null) engine.KeepOnlyTool(tool);

        string toolsJson = JsonSerializer.Serialize(engine.Tools.OfType<AIFunction>().Select(t => new { t.Name, t.Description, t.JsonSchema }));
        Log.Log(LogLevel.Information, HostingLogMessage.ToolsetProfileBuilt, new
        {
            Toolsets = (resolved.Count == 0 ? "(none)" : string.Join(", ", resolved.Select(t => t.Name)))
                + (role is null ? "" : $" · role {role}") + (level == RoleLevel.Agent ? "" : $" · {level.ToString().ToLowerInvariant()}")
                + (model is null ? "" : $" · llm {model.Name}") + (tool is null ? "" : $" · tool {tool}"),
            Tools = engine.Tools.Count,
            PromptChars = engine.SkillDocumentation.Length,
            // About four characters a token: an estimate of the fixed prefix every request of this profile sends.
            PrefixTokens = (engine.SkillDocumentation.Length + toolsJson.Length) / 4,
        });
        return engine;
    }
}