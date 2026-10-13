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
/// only their guides in its system prompt. Engines are cached by their normalised list, its personality and whether it has
/// the router (F21), so every session with the same three shares one engine, one tool list and one cacheable prefix. Everything else (the model client, the usage
/// ledger, the display currency, thinking) is shared by all of them.
/// </summary>
/// <remarks>
/// In <c>Hosting/</c>, not <c>Agent/</c>: it loads the skills through <see cref="AgentHost"/>, a layer above the engine.
/// </remarks>
public sealed class AgentEngineFactory
{
    /// <summary>How many profiles one instance keeps; engines are never disposed, so this bounds memory.</summary>
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
        Context = new ToolsetContext(agent, webSearch ?? WebSearchProviderFactory.Create(agent), rates);
    }

    /// <summary>What the catalogue builds this instance's tools from (settings, the search provider).</summary>
    public ToolsetContext Context { get; }

    /// <summary>Every toolset this instance can load: those whose settings are all set.</summary>
    public IReadOnlyList<string> Available => ToolsetCatalog.All.Where(t => t.IsAvailable(Context)).Select(t => t.Name).ToList();

    /// <summary>The named personalities a session can choose (F21), built in and in the skills folder.</summary>
    public IReadOnlyList<string> AvailablePersonalities => Personalities.Available(_agent.SkillsDirectory ?? AgentHost.FindSkillsDirectory(_agent));

    /// <summary>The profiles built so far: each its toolsets joined with commas, then |personality and |no-router when set.</summary>
    public IReadOnlyCollection<string> Profiles => _engines.Keys.ToList();

    /// <summary>
    /// The engine for <paramref name="toolsets"/> (any order or case; duplicates ignored), <paramref name="personality"/>
    /// (null: personality.md) and <paramref name="router"/> (default.md in the prompt), built on first use. Throws
    /// <see cref="ToolsetException"/> for an unknown or unavailable toolset, or when <see cref="DefaultCapacity"/>
    /// profiles exist already, and <see cref="PersonalityException"/> for an unknown personality, before anything is built.
    /// </summary>
    public AgentEngine GetOrCreate(IEnumerable<string>? toolsets, string? personality = null, bool router = true)
    {
        var resolved = ToolsetCatalog.Resolve(toolsets, Context);
        if (string.IsNullOrEmpty(personality)) personality = null;
        // Checked here, not in Build: a Lazy that throws keeps the exception, and the key would stay broken.
        AgentHost.PersonalityFile(_agent, personality);

        string key = string.Join(",", resolved.Select(t => t.Name)) + (personality is null ? "" : "|" + personality) + (router ? "" : "|no-router");
        if (_engines.TryGetValue(key, out var existing)) return existing.Value;
        if (_engines.Count >= _capacity)
            throw new ToolsetException(MessageText.Format(ErrorMessage.TooManyToolsetProfiles, new { Capacity = _capacity }));

        return _engines.GetOrAdd(key, _ => new Lazy<AgentEngine>(() => Build(resolved, personality, router))).Value;
    }

    private AgentEngine Build(IReadOnlyList<ToolsetDefinition> resolved, string? personality, bool router)
    {
        bool selfExtension = resolved.Any(t => t.IsEngineProvided);
        var engine = new AgentEngine(AgentHost.LoadSkills(_agent, resolved, personality, router), _chatClient, _agent, _supportsTools,
            toolsets: ToolsetCatalog.BuildTools(resolved, Context), _usageSink, _usageMapper, selfExtension)
        {
            SkillsDirectory = AgentHost.FindSkillsDirectory(_agent),
            // ReloadSkills reloads this profile's prompt (its guides, personality and router), never another's.
            SkillReloader = () => AgentHost.LoadSkills(_agent, resolved, personality, router),
            Thinking = _thinking,
            Display = _display,
        };
        // Approved agent-built tools come with the skills toolset (CreateTool, /approve).
        if (selfExtension) engine.LoadApprovedTools();

        string toolsJson = JsonSerializer.Serialize(engine.Tools.OfType<AIFunction>().Select(t => new { t.Name, t.Description, t.JsonSchema }));
        Log.Log(LogLevel.Information, LogMessage.ToolsetProfileBuilt, new
        {
            Toolsets = (resolved.Count == 0 ? "(none)" : string.Join(", ", resolved.Select(t => t.Name)))
                + (personality is null ? "" : $" · personality {personality}") + (router ? "" : " · no router"),
            Tools = engine.Tools.Count,
            PromptChars = engine.SkillDocumentation.Length,
            // About four characters a token: an estimate of the fixed prefix every request of this profile sends.
            PrefixTokens = (engine.SkillDocumentation.Length + toolsJson.Length) / 4,
        });
        return engine;
    }
}