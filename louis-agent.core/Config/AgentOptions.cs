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

    /// <summary>Directory searched for *-skills.md and default.md; null means the working dir, then /app.</summary>
    public string? SkillsDirectory { get; set; }

    /// <summary>Selects {AgentFunction}-skills.md.</summary>
    public string AgentFunction { get; set; } = "code-review";

    public string? PaymoApiKey { get; set; }

    /// <summary>API key for DevOps operations (work item retrieval, project management, etc.).</summary>
    public string? DevopsApiKey { get; set; }

    /// <summary>DEVOPS_ORGANIZATION: the Azure DevOps organisation (dev.azure.com/{organisation}).</summary>
    public string? DevopsOrganization { get; set; }

    /// <summary>DEVOPS_PROJECT: the project whose work items the DevOps tools query.</summary>
    public string? DevopsProject { get; set; }

    /// <summary>DEVOPS_TEAM (optional): the team whose sprints are used; sprints live under {project}\{team}.</summary>
    public string? DevopsTeam { get; set; }

    /// <summary>WEB_SEARCH_PROVIDER: google or duckduckgo; null picks Google when its key and engine ID are set, else DuckDuckGo.</summary>
    public string? WebSearchProvider { get; set; }

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

    /// <summary>The default of <see cref="SubSessionPromptBudget"/>: Claude Haiku 5.5's long-prompt threshold (config/prices.json).</summary>
    public const int DefaultSubSessionPromptBudget = 100_000;

    /// <summary>
    /// SUB_SESSION_PROMPT_BUDGET: the most tokens a sub-session's prompt should reach (FX14). When the next request is
    /// expected to pass it, the specialist answers from what it has instead of calling more tools. 0 turns it off.
    /// </summary>
    public int SubSessionPromptBudget { get; set; } = DefaultSubSessionPromptBudget;

    /// <summary>AGENT_API_KEY: this deployment's API key, sent to peers as x-api-key (never taken from the model's arguments).</summary>
    public string? AgentApiKey { get; set; }

    /// <summary>Parses AGENT_PEERS; an entry without a name or an absolute http(s) URL is skipped.</summary>
    internal static IReadOnlyDictionary<string, Uri> ParsePeers(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2 && pair[0].Length > 0
                && Uri.TryCreate(pair[1], UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .GroupBy(pair => pair[0].ToLowerInvariant())
            .ToDictionary(g => g.Key, g => new Uri(g.Last()[1].TrimEnd('/') + "/"), StringComparer.OrdinalIgnoreCase);

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
            AgentApiKey = Get("AGENT_API_KEY"),
            SubSessionPromptBudget = int.TryParse(Get("SUB_SESSION_PROMPT_BUDGET"), NumberStyles.None, CultureInfo.InvariantCulture, out int budget)
                ? budget : DefaultSubSessionPromptBudget,
        };
        if (Get("AGENT_FUNCTION") is { } function) options.AgentFunction = function;
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
