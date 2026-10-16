namespace LouisAgent.Core.Hosting;

using System.Collections.Concurrent;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

/// <summary>
/// One engine per toolset profile (F5): a session names its toolsets, and gets an engine with only those tools and
/// only their guides in its system prompt. Engines are cached by their normalised list, its personality and its level
/// (F21, CL8), so every session with the same three shares one engine, one tool list and one cacheable prefix. Everything else (the model client, the usage
/// ledger, the display currency, thinking) is shared by all of them.
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
    private readonly ConcurrentDictionary<string, Lazy<AgentEngine>> _engines = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _lastUsed = new(StringComparer.Ordinal);
    private readonly Lock _evicting = new();
    private long _clock;

    public AgentEngineFactory(IChatClient chatClient, AgentOptions agent, bool supportsTools = true,
        IUsageSink? usageSink = null, IUsageMapper? usageMapper = null, DisplayCurrency? display = null,
        ReasoningEffort? thinking = null, IWebSearchProvider? webSearch = null, int capacity = DefaultCapacity,
        ExchangeRates? rates = null)
    {
        _chatClient = chatClient;
        _agent = agent;
        _supportsTools = supportsTools;
        _usageSink = usageSink;
        _usageMapper = usageMapper;
        _display = display;
        _thinking = thinking;
        _capacity = capacity;
        Context = new ToolsetContext(agent, webSearch ?? WebSearchProviderFactory.Create(agent), rates, usageSink);
    }

    /// <summary>What the catalogue builds this instance's tools from (settings, the search provider).</summary>
    public ToolsetContext Context { get; }

    /// <summary>Every toolset this instance can load: those whose settings are all set.</summary>
    public IReadOnlyList<string> Available => ToolsetCatalog.All.Where(t => t.IsAvailable(Context)).Select(t => t.Name).ToList();

    /// <summary>The named personalities a session at <paramref name="level"/> can choose (F21, CL8), all built in.</summary>
    public IReadOnlyList<string> AvailablePersonalities(SkillLevel level) => AgentHost.EnabledPersonalities(_agent, level);

    /// <summary>
    /// The role for a sub-session with <paramref name="toolsets"/> when its parent named none: from
    /// <c>specialists/roles.json</c> (built in), and only a role this instance has; null otherwise.
    /// </summary>
    public string? RoleFor(IEnumerable<string> toolsets)
    {
        if (AgentHost.ReadSkillFile(_agent, Personalities.RolesFile) is not { } roles) return null;
        string? role = Personalities.RoleFor(roles.Text, toolsets);
        if (role is null || !Personalities.IsValidName(role)) return null;
        try { AgentHost.PersonalityFile(_agent, role, SkillLevel.Specialist); return role; }
        catch (PersonalityException) { return null; }
    }

    /// <summary>The profiles built so far: each its toolsets joined with commas, then |personality and |level below level 0.</summary>
    public IReadOnlyCollection<string> Profiles => _engines.Keys.ToList();

    /// <summary>
    /// The engine for <paramref name="toolsets"/> (any order or case; duplicates ignored), <paramref name="personality"/>
    /// (null: personality.md) and <paramref name="level"/> (CL8: where the personality is; the router only at level 0),
    /// built on first use. Throws
    /// <see cref="ToolsetException"/> for an unknown or unavailable toolset and <see cref="PersonalityException"/> for an
    /// unknown personality or one from another level, before anything is built. A full cache drops its least recently used profile (on a specialist
    /// the model chooses the lists, so they are many).
    /// </summary>
    public AgentEngine GetOrCreate(IEnumerable<string>? toolsets, string? personality = null, SkillLevel level = SkillLevel.Agent)
    {
        var resolved = ToolsetCatalog.Resolve(toolsets, Context);
        if (string.IsNullOrEmpty(personality)) personality = null;
        // Checked here, not in Build: a Lazy that throws keeps the exception, and the key would stay broken.
        AgentHost.PersonalityFile(_agent, personality, level);

        string key = string.Join(",", resolved.Select(t => t.Name)) + (personality is null ? "" : "|" + personality) + LevelSuffix(level);
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
                Log.Log(LogLevel.Information, LogMessage.ToolsetProfileDropped, new { Toolsets = oldest.Length == 0 ? "(none)" : oldest, Capacity = _capacity });
            }
            engine = _engines.GetOrAdd(key, _ => new Lazy<AgentEngine>(() => Build(resolved, personality, level)));
        }
        return engine.Value;   // built outside the lock: building one profile doesn't hold up another
    }

    /// <summary>Level 0 adds nothing to the key, so its profiles keep the names they had before CL8.</summary>
    private static string LevelSuffix(SkillLevel level) => level == SkillLevel.Agent ? "" : "|" + level.ToString().ToLowerInvariant();

    private AgentEngine Build(IReadOnlyList<ToolsetDefinition> resolved, string? personality, SkillLevel level)
    {
        bool selfExtension = resolved.Any(t => t.IsEngineProvided);
        var engine = new AgentEngine(AgentHost.LoadSkills(_agent, resolved, personality, level), _chatClient, _agent, _supportsTools,
            toolsets: ToolsetCatalog.BuildTools(resolved, Context), _usageSink, _usageMapper, selfExtension)
        {
            SkillsDirectory = AgentHost.FindSkillsDirectory(_agent),
            // ReloadSkills reloads this profile's prompt (its guides, personality and router), never another's.
            SkillReloader = () => AgentHost.LoadSkills(_agent, resolved, personality, level),
            Thinking = _thinking,
            Display = _display,
        };
        // Approved agent-built tools come with the skills toolset (CreateTool, /approve).
        if (selfExtension) engine.LoadApprovedTools();

        string toolsJson = JsonSerializer.Serialize(engine.Tools.OfType<AIFunction>().Select(t => new { t.Name, t.Description, t.JsonSchema }));
        Log.Log(LogLevel.Information, LogMessage.ToolsetProfileBuilt, new
        {
            Toolsets = (resolved.Count == 0 ? "(none)" : string.Join(", ", resolved.Select(t => t.Name)))
                + (personality is null ? "" : $" · personality {personality}") + (level == SkillLevel.Agent ? "" : $" · {level.ToString().ToLowerInvariant()}"),
            Tools = engine.Tools.Count,
            PromptChars = engine.SkillDocumentation.Length,
            // About four characters a token: an estimate of the fixed prefix every request of this profile sends.
            PrefixTokens = (engine.SkillDocumentation.Length + toolsJson.Length) / 4,
        });
        return engine;
    }
}