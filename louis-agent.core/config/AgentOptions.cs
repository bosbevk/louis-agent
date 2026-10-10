namespace louis_agent.core.config;

/// <summary>Agent runtime settings, resolved once at startup instead of read from the environment inside core.</summary>
public sealed class AgentOptions
{
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

    /// <summary>PRICES_FILE: the price table for the ledger's costs; null means config/prices.json, found like config/.env.</summary>
    public string? PricesFile { get; set; }

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
            PricesFile = Get("PRICES_FILE"),
        };
        if (Get("AGENT_FUNCTION") is { } function) options.AgentFunction = function;
        if (Get("WORKSPACE_ROOT") is { } root) options.WorkspaceRoot = root;
        else if (FindRepositoryRoot(Environment.CurrentDirectory) is { } repoRoot)
        {
            options.WorkspaceRoot = repoRoot;
            Console.Error.WriteLine($"[INFO] WORKSPACE_ROOT not set, using repository root: {repoRoot}");
        }
        else Console.Error.WriteLine($"[WARN] WORKSPACE_ROOT not set, using current directory: {options.WorkspaceRoot}");
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
