namespace LouisAgent.Core.Config;

using System.Globalization;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>Agent runtime settings, resolved once at startup instead of read from the environment inside core.</summary>
public sealed class AgentOptions
{
    private static readonly ILogger Log = AgentLogging.For<AgentOptions>();

    public string WorkspaceRoot { get; set; } = Environment.CurrentDirectory;

    /// <summary>The skills folder (agent/, specialists/, toolsets/); null means the working dir, then /app.</summary>
    public string? SkillsDirectory { get; set; }


    public string? PaymoApiKey { get; set; }

    /// <summary>API key for DevOps operations (work item retrieval, project management, etc.).</summary>
    public string? DevopsApiKey { get; set; }

    /// <summary>DEVOPS_ORGANIZATION: the Azure DevOps organisation (dev.azure.com/{organisation}).</summary>
    public string? DevopsOrganization { get; set; }

    /// <summary>DEVOPS_PROJECT: the project whose work items the DevOps tools query.</summary>
    public string? DevopsProject { get; set; }

    /// <summary>DEVOPS_TEAM (optional): the team whose sprints are used; sprints live under {project}\{team}.</summary>
    public string? DevopsTeam { get; set; }

    /// <summary>WEB_SEARCH_PROVIDER: tavily, google, searxng or duckduckgo; null picks Tavily when its key is set, else Google when its key and engine ID are, else SearXNG when its URL is, else DuckDuckGo.</summary>
    public string? WebSearchProvider { get; set; }

    /// <summary>SEARXNG_URL (F32): the SearXNG we run ourselves, for development, tests and benchmarks (ADR 0023).</summary>
    public string? SearxngUrl { get; set; }

    /// <summary>BROWSER_URL (F32): the headless Chrome we run, its DevTools address (http://puls-browser:9222 in the Puls stack).</summary>
    public string? BrowserUrl { get; set; }

    /// <summary>PAGE_RENDERER (F32): chrome or tavily; null picks Chrome when BROWSER_URL is set, else Tavily when its key is.</summary>
    public string? PageRenderer { get; set; }

    /// <summary>Tavily search API key (TAVILY_API_KEY), F25.</summary>
    public string? TavilyApiKey { get; set; }

    /// <summary>Google Custom Search JSON API key (GOOGLE_SEARCH_API_KEY).</summary>
    public string? GoogleSearchApiKey { get; set; }

    /// <summary>Google Programmable Search Engine ID, the "cx" value (GOOGLE_SEARCH_ENGINE_ID).</summary>
    public string? GoogleSearchEngineId { get; set; }

    /// <summary>Rider MCP server endpoint (default: http://127.0.0.1:64482).</summary>
    public string? RiderMcpEndpoint { get; set; }

    /// <summary>Enable auto-discovery of tools from Rider's MCP server on startup (default: true).</summary>
    public bool RiderMcpAutoDiscover { get; set; } = true;

    /// <summary>Project path to send to Rider MCP server (for Docker: the HOST path, not container path).</summary>
    public string? RiderMcpProjectPath { get; set; }

    /// <summary>LOG_DIRECTORY: where run logs and oversized-tool-result records are written; null disables file logging.</summary>
    public string? LogDirectory { get; set; }

    /// <summary>USAGE_LEDGER: on (default) records every model request to {LOG_DIRECTORY}/usage-YYYY-MM.jsonl; off disables it.</summary>
    public bool UsageLedger { get; set; } = true;

    /// <summary>
    /// USAGE_IN_CHAT: true adds each turn's usage line to the chat in Rider (F3), after the answer; off by default, since
    /// it is part of the conversation there rather than beside it. The line always goes to the ACP server's stderr log.
    /// </summary>
    public bool UsageInChat { get; set; }

    /// <summary>PRICES_FILE: the price table for the ledger's costs; null means config/prices.json, found like config/.env.</summary>
    public string? PricesFile { get; set; }

    /// <summary>
    /// PRICES_URL: fetch the price table over HTTP instead, e.g. louis-agent.api's GET /prices; if it can't be reached,
    /// the file (PricesFile or config/prices.json) is used.
    /// </summary>
    public string? PricesUrl { get; set; }

    /// <summary>Where exchange rates come from by default: Frankfurter, the ECB's daily reference rates (free, no key).</summary>
    /// <remarks><c>base</c> is v1's documented parameter (<c>from</c> works as an alias today, but means a date in v2).</remarks>
    public const string DefaultRatesUrl = "https://api.frankfurter.dev/v1/latest?base=USD";

    /// <summary>
    /// RATES_URL: where a host fetches exchange rates for showing costs in another currency (F14), e.g. Frankfurter (the
    /// default) or louis-agent.api's GET /rates; "off" skips the fetch. If it can't be reached, RATES_FILE is used.
    /// </summary>
    public string? RatesUrl { get; set; } = DefaultRatesUrl;

    /// <summary>RATES_FORMAT: the format of RATES_URL's reply, turned into ours by its mapper: frankfurter (default) or rates-json.</summary>
    public string RatesFormat { get; set; } = "frankfurter";

    /// <summary>RATES_FILE: the fallback rates, in our rates.json format; null means config/rates.json, found like config/.env.</summary>
    public string? RatesFile { get; set; }

    /// <summary>
    /// HOST_CULTURE: the PC's Windows culture (e.g. en-ZA), whose region decides the currency costs are shown in (F14).
    /// Containers can't see the PC's region, so it's written to config/.env once (usage-probe --host-culture) and passed
    /// as a value: their own culture stays invariant, so tools and tests format numbers the same everywhere.
    /// </summary>
    public string? HostCulture { get; set; }

    /// <summary>DISPLAY_CURRENCY: show costs in this currency (an ISO code, e.g. EUR) instead of the region's.</summary>
    public string? DisplayCurrency { get; set; }

    /// <summary>
    /// AGENT_PEERS: other louis-agent APIs this agent may open sub-sessions on (F5-S5), as <c>name=url</c> pairs separated by
    /// commas, e.g. <c>coder=http://louis-agent-coder:8080</c>. Empty disables the <c>agents</c> toolset.
    /// </summary>
    public IReadOnlyDictionary<string, Uri> AgentPeers { get; set; } = new Dictionary<string, Uri>();

    /// <summary>
    /// ROLES_ENABLED: the roles this instance may use (F29), comma separated and lowercase, e.g.
    /// <c>puls/voice-af,puls-cook</c>; a session or sub-session asking for another is refused as for an unknown one.
    /// Empty: every built-in role. Puls's compose lists only the roles that have passed their check. PERSONALITIES_ENABLED,
    /// its name before CL14, is read when it's unset, with a warning, until the TODO's line removes it.
    /// </summary>
    public IReadOnlyList<string> RolesEnabled { get; set; } = [];

    /// <summary>
    /// LLM_SESSION_PROFILES: the LLM profiles a session may name when it opens (F30), comma separated, e.g.
    /// <c>ollama-puls</c>; each is read from <c>config/.env.{name}</c>. Empty: every session runs on the instance's model.
    /// </summary>
    public IReadOnlyList<string> SessionLlmProfiles { get; set; } = [];

    /// <summary>
    /// MINION_LLMS (F30): the model a minion role runs on, as <c>role=profile</c> entries separated by commas, with
    /// <c>*</c> for every role not named and <c>instance</c> for this instance's own model; a <c>&gt;</c> lists a cascade,
    /// cheapest first, e.g. <c>*=ollama-puls&gt;instance,puls-minion-web-summary=instance</c>. Every profile must be in
    /// <see cref="SessionLlmProfiles"/> (checked at start-up). Empty: no <c>minions</c> toolset.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> MinionLlms { get; set; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Parses <c>MINION_LLMS</c>; an entry without a role or without a model is dropped, names are lowercased.</summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseMinionLlms(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2 && pair[0].Length > 0)
            .Select(pair => (Role: pair[0].ToLowerInvariant(), Models: (IReadOnlyList<string>)pair[1]
                .Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(m => m.ToLowerInvariant()).Distinct().ToList()))
            .Where(x => x.Models.Count > 0)
            .GroupBy(x => x.Role, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last().Models, StringComparer.Ordinal);

    /// <summary>
    /// The default of <see cref="SubSessionPromptBudget"/>: below Claude Haiku 5.5's long-prompt threshold (100k in
    /// config/prices.json), not at it. The request the budget forces to answer still carries the last round's tool
    /// results, so it needs room under the price step.
    /// </summary>
    public const int DefaultSubSessionPromptBudget = 80_000;

    /// <summary>
    /// SUB_SESSION_PROMPT_BUDGET: the most tokens a sub-session's prompt should reach (FX14). When the next request is
    /// expected to pass it, the specialist answers from what it has instead of calling more tools. 0 turns it off.
    /// </summary>
    public int SubSessionPromptBudget { get; set; } = DefaultSubSessionPromptBudget;

    /// <summary>AGENT_API_KEY: this deployment's API key, sent to peers as x-api-key (never taken from the model's arguments).</summary>
    public string? AgentApiKey { get; set; }

    /// <summary>
    /// HOST_TIME_ZONE (CL15): the zone <c>GetToday</c> and the API's place line use when the user shared none, an IANA name
    /// (<c>Africa/Johannesburg</c> in the Puls stack); null is the machine's own. Core names no country.
    /// </summary>
    public string? HostTimeZone { get; set; }

    /// <summary>Parses AGENT_PEERS; an entry without a name or an absolute http(s) URL is skipped.</summary>
    internal static IReadOnlyList<string> ParseNames(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => name.ToLowerInvariant()).Distinct().ToList();

    internal static IReadOnlyDictionary<string, Uri> ParsePeers(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2 && pair[0].Length > 0
                && Uri.TryCreate(pair[1], UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .GroupBy(pair => pair[0].ToLowerInvariant())
            .ToDictionary(g => g.Key, g => new Uri(g.Last()[1].TrimEnd('/') + "/"), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// ROLES_ENABLED, or PERSONALITIES_ENABLED (its name before CL14) when it's unset. Read raw: set but empty is "every
    /// role on", never "unset" (review of PR #60). The old name, when set, is always said in a warning: renamed when it's
    /// read, ignored when the new one is set too (Puls's compose sets ROLES_ENABLED, and env_file passes config/.env in).
    /// </summary>
    private static string? RolesSetting(Func<string, string?> raw)
    {
        string? roles = raw("ROLES_ENABLED");
        string? old = raw("PERSONALITIES_ENABLED");
        if (old is not null)
            Log.Log(LogLevel.Warning, roles is null ? LogMessage.RolesSettingRenamed : LogMessage.RolesSettingIgnored,
                new { Old = "PERSONALITIES_ENABLED", New = "ROLES_ENABLED" });
        return roles ?? old;
    }

    public static AgentOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        var options = new AgentOptions
        {
            SkillsDirectory = Get("SKILLS_DIRECTORY"),
            PaymoApiKey = Get("PAYMO_API_KEY"),
            DevopsApiKey = Get("DEVOPS_API_KEY"),
            DevopsOrganization = Get("DEVOPS_ORGANIZATION"),
            DevopsProject = Get("DEVOPS_PROJECT"),
            DevopsTeam = Get("DEVOPS_TEAM"),
            WebSearchProvider = Get("WEB_SEARCH_PROVIDER"),
            SearxngUrl = Get("SEARXNG_URL"),
            BrowserUrl = Get("BROWSER_URL"),
            PageRenderer = Get("PAGE_RENDERER"),
            TavilyApiKey = Get("TAVILY_API_KEY"),
            GoogleSearchApiKey = Get("GOOGLE_SEARCH_API_KEY"),
            GoogleSearchEngineId = Get("GOOGLE_SEARCH_ENGINE_ID"),
            RiderMcpEndpoint = Get("RIDER_MCP_ENDPOINT") ?? "http://127.0.0.1:64482",
            RiderMcpAutoDiscover = bool.TryParse(Get("RIDER_MCP_AUTO_DISCOVER"), out var result) ? result : true,
            RiderMcpProjectPath = Get("RIDER_MCP_PROJECT_PATH"),
            LogDirectory = Get("LOG_DIRECTORY"),
            UsageLedger = !string.Equals(Get("USAGE_LEDGER"), "off", StringComparison.OrdinalIgnoreCase),
            UsageInChat = bool.TryParse(Get("USAGE_IN_CHAT"), out bool usageInChat) && usageInChat,
            PricesFile = Get("PRICES_FILE"),
            PricesUrl = Get("PRICES_URL"),
            RatesUrl = Get("RATES_URL") switch
            {
                null => DefaultRatesUrl,
                { } off when off.Equals("off", StringComparison.OrdinalIgnoreCase) => null,
                { } url => url,
            },
            RatesFormat = Get("RATES_FORMAT") ?? "frankfurter",
            RatesFile = Get("RATES_FILE"),
            HostCulture = Get("HOST_CULTURE"),
            DisplayCurrency = Get("DISPLAY_CURRENCY"),
            AgentPeers = ParsePeers(Get("AGENT_PEERS")),
            RolesEnabled = ParseNames(RolesSetting(getVariable)),
            SessionLlmProfiles = ParseNames(Get("LLM_SESSION_PROFILES")),
            MinionLlms = ParseMinionLlms(Get("MINION_LLMS")),
            AgentApiKey = Get("AGENT_API_KEY"),
            HostTimeZone = TimeZoneChecked(Get("HOST_TIME_ZONE")),
            SubSessionPromptBudget = int.TryParse(Get("SUB_SESSION_PROMPT_BUDGET"), NumberStyles.None, CultureInfo.InvariantCulture, out int budget)
                ? budget : DefaultSubSessionPromptBudget,
        };
        if (Get("WORKSPACE_ROOT") is { } root) options.WorkspaceRoot = root;
        else if (FindRepositoryRoot(Environment.CurrentDirectory) is { } repoRoot)
        {
            options.WorkspaceRoot = repoRoot;
            Log.Log(LogLevel.Information, LogMessage.WorkspaceRootFromRepository, new { Path = repoRoot });
        }
        else Log.Log(LogLevel.Warning, LogMessage.WorkspaceRootFromCurrentDirectory, new { Path = options.WorkspaceRoot });
        options.WorkspaceRoot = Path.GetFullPath(options.WorkspaceRoot);
        return options;
    }

    /// <summary>
    /// HOST_TIME_ZONE must be a zone this machine knows (review of CL15 step 3): a misspelt one fell back in silence and
    /// reached the model's place line as written, two hours off; every other setting stops the host when it's wrong.
    /// </summary>
    private static string? TimeZoneChecked(string? name) =>
        name is null || TimeZoneInfo.TryFindSystemTimeZoneById(name, out _)
            ? name
            : throw new ArgumentException(MessageText.Format(ErrorMessage.UnknownHostTimeZone, new { Value = name }));

    /// <summary>Nearest directory at or above <paramref name="startDirectory"/> containing .git or a .sln file.</summary>
    internal static string? FindRepositoryRoot(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                File.Exists(Path.Combine(dir.FullName, ".git")) ||
                dir.EnumerateFiles("*.sln").Any())
                return dir.FullName;
        }

        return null;
    }
}
