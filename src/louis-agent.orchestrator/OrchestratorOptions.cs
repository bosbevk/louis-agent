namespace louis_agent.orchestrator;

/// <summary>Which service this orchestrator watches and where louis-agent is; resolved once at startup.</summary>
internal sealed class OrchestratorOptions
{
    /// <summary>ORCHESTRATOR_SERVICE: the service name, which selects Skills/{service}/ (default order-service).</summary>
    public string Service { get; init; } = "order-service";

    /// <summary>ORCHESTRATOR_SERVICE_ROOT: the service's git repository, the same folder louis-agent.api uses as WORKSPACE_ROOT.</summary>
    public required string ServiceRoot { get; init; }

    /// <summary>ORCHESTRATOR_SERVICE_PROJECT: the service's runnable project, relative to its root, for replaying requests.</summary>
    public string ServiceProject { get; init; } = "src/OrderService";

    /// <summary>The service's error feed (logs/errors.jsonl in the service root), standing in for Exceptionless.</summary>
    public string ErrorLogPath => Path.Combine(ServiceRoot, "logs", "errors.jsonl");

    /// <summary>LOUIS_AGENT_URL: the louis-agent.api instance that owns this service's repository.</summary>
    public Uri LouisAgentUrl { get; init; } = new("http://127.0.0.1:5081");

    /// <summary>LOUIS_AGENT_API_KEY (falls back to AGENT_API_KEY): sent as x-api-key when set.</summary>
    public string? LouisAgentApiKey { get; init; }

    /// <summary>Branch fixes must never land on directly; a human merges fix branches into it.</summary>
    public string MainBranch { get; init; } = "main";

    /// <summary>ORCHESTRATOR_STATE_DIRECTORY: processed error ids, decisions and escalations (default logs/orchestrator/{service}).</summary>
    public required string StateDirectory { get; init; }

    /// <summary>ORCHESTRATOR_COMMS_LOG: the Markdown log of everything the agents said to each other (default {state}/agent-comms.md).</summary>
    public string CommsLogPath => _commsLogPath ?? Path.Combine(StateDirectory, "agent-comms.md");
    private string? _commsLogPath;
    public string? CommsLogOverride { init => _commsLogPath = value is null ? null : Path.GetFullPath(value); }

    /// <summary>Folder holding orchestrator.md and the per-service skill folders.</summary>
    public required string SkillsDirectory { get; init; }

    public bool Once { get; init; }
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(15);

    public static OrchestratorOptions FromEnvironment(string[] args, Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        string service = Get("ORCHESTRATOR_SERVICE") ?? "order-service";
        string serviceRoot = Get("ORCHESTRATOR_SERVICE_ROOT")
                             ?? throw new InvalidOperationException("ORCHESTRATOR_SERVICE_ROOT is required: the service's git repository.");
        int interval = int.TryParse(Get("ORCHESTRATOR_POLL_SECONDS"), out int seconds) && seconds > 0 ? seconds : 15;

        return new OrchestratorOptions
        {
            Service = service,
            ServiceRoot = Path.GetFullPath(serviceRoot),
            ServiceProject = Get("ORCHESTRATOR_SERVICE_PROJECT") ?? "src/OrderService",
            LouisAgentUrl = new Uri(Get("LOUIS_AGENT_URL") ?? "http://127.0.0.1:5081"),
            LouisAgentApiKey = Get("LOUIS_AGENT_API_KEY") ?? Get("AGENT_API_KEY"),
            StateDirectory = Path.GetFullPath(Get("ORCHESTRATOR_STATE_DIRECTORY") ?? Path.Combine("logs", "orchestrator", service)),
            CommsLogOverride = Get("ORCHESTRATOR_COMMS_LOG"),
            SkillsDirectory = Path.Combine(AppContext.BaseDirectory, "Skills"),
            Once = args.Contains("--once"),
            PollInterval = TimeSpan.FromSeconds(interval),
        };
    }
}
