namespace louis_agent.core;

using dotenv.net;
using louis_agent.core.config;
using louis_agent.core.tools;
using louis_agent.core.providers;
using louis_agent.core.mcp;
using louis_agent.core.usage;

/// <summary>Everything an entry point needs after bootstrap.</summary>
public sealed record AgentHostContext(AgentEngine Engine, LlmOptions Llm, AgentOptions Agent);

/// <summary>
/// Single bootstrap shared by the CLI, ACP and MCP entry points: dotenv load, option binding,
/// skill discovery and composite wiring.
/// </summary>
public static partial class AgentHost
{
    /// <summary>
    /// Loads config/.env.secrets and config/.env (plus a legacy ./.env), searching upward from the working
    /// directory so the CLI works from any folder in the repo, then the LLM profile config/.env.{LLM_PROFILE}
    /// (.env.anthropic, .env.ollama). Variables already set in the process (shell, Docker env_file) win; each file
    /// only fills what the ones before it left unset, so the order is secrets, .env, then the profile.
    /// </summary>
    public static void LoadEnvironment() => LoadEnvironment(Environment.CurrentDirectory);

    internal static void LoadEnvironment(string startDirectory)
    {
        var files = new List<string>();
        string? configDir = FindConfigDirectory(startDirectory);
        if (configDir is not null)
        {
            files.Add(Path.Combine(configDir, ".env.secrets"));
            files.Add(Path.Combine(configDir, ".env"));
        }
        files.Add(Path.Combine(startDirectory, ".env"));
        Load(files);

        // Read after .env is loaded, since that is where it is usually set.
        if (Environment.GetEnvironmentVariable("LLM_PROFILE") is not { Length: > 0 } profile || configDir is null) return;
        string profileFile = Path.Combine(configDir, $".env.{profile}");
        if (!LlmProfileName().IsMatch(profile) || !File.Exists(profileFile))
        {
            Console.Error.WriteLine($"[WARN] LLM_PROFILE '{profile}' has no config/.env.{profile}; using the LLM settings from .env and the defaults.");
            return;
        }
        Load([profileFile]);

        static void Load(IEnumerable<string> paths) => DotEnv.Load(options: new DotEnvOptions(
            ignoreExceptions: true,
            envFilePaths: paths.Where(File.Exists).ToArray(),
            overwriteExistingVars: false));
    }

    /// <summary>Profile names are file-name parts (anthropic, ollama), never paths.</summary>
    [System.Text.RegularExpressions.GeneratedRegex("^[a-z0-9-]+$")]
    private static partial System.Text.RegularExpressions.Regex LlmProfileName();

    internal static string? FindConfigDirectory(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "config");
            if (File.Exists(Path.Combine(candidate, ".env")) || File.Exists(Path.Combine(candidate, ".env.secrets")))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Builds an engine for hosts that only publish its tools (the MCP server): the calling client brings its own
    /// model, so no LLM provider or API key is needed. Skills, agent-built tools and approval work as usual.
    /// </summary>
    public static AgentEngine BuildToolHost()
    {
        LoadEnvironment();
        var agent = AgentOptions.FromEnvironment();
        AgentLog.Initialize(agent.LogDirectory);
        var engine = new AgentEngine(LoadSkills(agent), new ToolsOnlyChatClient(), agent)
        {
            SkillsDirectory = FindSkillsDirectory(agent),
            SkillReloader = () => LoadSkills(agent),
        };
        engine.LoadApprovedTools();
        return engine;
    }

    /// <summary>Stand-in for hosts that never prompt a model themselves.</summary>
    private sealed class ToolsOnlyChatClient : Microsoft.Extensions.AI.IChatClient
    {
        public Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("This host only exposes tools; it has no language model.");

        public IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("This host only exposes tools; it has no language model.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    public static AgentHostContext Build(ILlmClientFactory? clientFactory = null)
    {
        LoadEnvironment();

        var llm = LlmOptions.FromEnvironment();
        var agent = AgentOptions.FromEnvironment();
        AgentLog.Initialize(agent.LogDirectory);
        return Build(llm, agent, clientFactory ?? new LlmClientFactory());
    }

    public static AgentHostContext Build(LlmOptions llm, AgentOptions agent, ILlmClientFactory clientFactory)
    {
        var skills = LoadSkills(agent);
        var chatClient = clientFactory.Create(llm);
        var engine = new AgentEngine(skills, chatClient, agent, llm.ResolveSupportsTools(),
            usageSink: CreateUsageSink(agent), usageMapper: clientFactory.CreateUsageMapper(llm))
        {
            SkillsDirectory = FindSkillsDirectory(agent),
            SkillReloader = () => LoadSkills(agent),
            Thinking = llm.ResolveThinking(),
        };
        engine.LoadApprovedTools();
        Console.Error.WriteLine($"[INFO] LLM: {llm}; tools={(engine.SupportsTools ? "on" : "off")}; thinking={engine.Thinking?.ToString().ToLowerInvariant() ?? "off"}");

        // Auto-discover and register tools from Rider's MCP server (async, non-blocking)
        if (agent.RiderMcpAutoDiscover && !string.IsNullOrWhiteSpace(agent.RiderMcpEndpoint))
        {
            Console.Error.WriteLine($"[INFO] Starting MCP tool discovery from {agent.RiderMcpEndpoint}");
            _ = Task.Run(async () =>
            {
                try
                {
                    // Use explicit project path if set (for Docker), otherwise use workspace root
                    var projectPath = agent.RiderMcpProjectPath ?? agent.WorkspaceRoot;
                    var discovery = new RiderMcpToolDiscovery(engine, agent.RiderMcpEndpoint, projectPath);
                    await discovery.DiscoverAndRegisterAsync(throwOnError: false);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MCP] Startup task exception: {ex}");
                }
            });
        }
        else
        {
            Console.Error.WriteLine($"[INFO] MCP discovery disabled or not configured");
        }

        return new AgentHostContext(engine, llm, agent);
    }

    /// <summary>
    /// The usage ledger, {LOG_DIRECTORY}/usage-YYYY-MM.jsonl, with each record priced from the price table; or null when
    /// USAGE_LEDGER=off or there is no LOG_DIRECTORY (with a warning, since the ledger was wanted). A malformed price table
    /// throws, so a host doesn't start on wrong prices.
    /// </summary>
    public static IUsageSink? CreateUsageSink(AgentOptions agent)
    {
        if (!agent.UsageLedger) return null;
        if (agent.LogDirectory is null)
        {
            Console.Error.WriteLine("[WARN] Usage ledger off: LOG_DIRECTORY is not set. Set it to record token usage, or USAGE_LEDGER=off to silence this.");
            return null;
        }

        Console.Error.WriteLine($"[INFO] Usage ledger: {Path.Combine(agent.LogDirectory, "usage-YYYY-MM.jsonl")}");
        return new PricingUsageSink(new JsonlUsageSink(agent.LogDirectory), LoadPrices(agent));
    }

    /// <summary>The price table from PRICES_FILE, or config/prices.json found like config/.env; empty, with a warning, if there's none.</summary>
    internal static PriceTable LoadPrices(AgentOptions agent)
    {
        string? path = agent.PricesFile
                       ?? (FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "prices.json") : null);
        PriceTable prices = path is null ? PriceTable.Empty : PriceTable.Load(path);
        if (prices.IsEmpty)
            Console.Error.WriteLine($"[WARN] No price table at {path ?? "config/prices.json"}: usage is recorded without cost. Set PRICES_FILE to price it.");
        else
            Console.Error.WriteLine($"[INFO] Prices: {path} (as of {prices.AsOf}, {prices.Models.Count} models)");
        return prices;
    }

    /// <summary>
    /// Loads skills using typed providers.
    /// Always includes DefaultSkillsProvider, and loads function-specific providers based on AgentFunction.
    /// If AgentFunction is "louis", loads ALL available skill providers.
    /// Files are looked up in <see cref="AgentOptions.SkillsDirectory"/>, the working directory, then /app.
    /// </summary>
    public static ISkillProvider LoadSkills(AgentOptions agent)
    {
        var composite = new CompositeSkillProvider();

        // Personality first, so who the agent is frames everything after it; default.md stays purely operational
        composite.AddProvider(new PersonalitySkillsProvider(FindSkillFile(agent, "personality.md")));
        Console.Error.WriteLine($"[INFO] Loaded PersonalitySkillsProvider");

        // Always include default instructions
        var defaultProvider = new DefaultSkillsProvider(FindSkillFile(agent, "default.md"));
        composite.AddProvider(defaultProvider);
        Console.Error.WriteLine($"[INFO] Loaded DefaultSkillsProvider");

        // DotNet* tools are registered for every function, so their guidance is too
        composite.AddProvider(new DotNetSkillsProvider(FindSkillFile(agent, "dotnet-skills.md")));
        Console.Error.WriteLine($"[INFO] Loaded DotNetSkillsProvider");

        // Same for the other always-registered toolsets; plain markdown, so no dedicated provider class is needed
        foreach (string fileName in AlwaysLoadedSkillFiles)
        {
            if (FindSkillFile(agent, fileName) is not { } path) continue;
            composite.AddProvider(new MarkdownSkillProvider(path, includeDefaultInstructions: false));
            Console.Error.WriteLine($"[INFO] Loaded {fileName}");
        }

        // Load function-specific provider based on AgentFunction
        var functionProvider = agent.AgentFunction switch
        {
            "louis" => LoadAllSkillProviders(agent, composite),
            "devops" => new DevOpsSkillsProvider(FindSkillFile(agent, "devops-skills.md")) as ISkillProvider,
            "paymo" => new PaymoSkillsProvider(FindSkillFile(agent, "paymo-skills.md")) as ISkillProvider,
            "code-review" => new CodeReviewSkillsProvider(FindSkillFile(agent, "code-review-skills.md")) as ISkillProvider,
            "time-logging" => new TimeLoggingSkillsProvider(FindSkillFile(agent, "time-logging-skills.md")) as ISkillProvider,
            _ => null
        };

        // If a specific provider is found, add it
        if (functionProvider != null && agent.AgentFunction != "louis")
        {
            composite.AddProvider(functionProvider);
            Console.Error.WriteLine($"[INFO] Loaded {agent.AgentFunction} skills provider");
        }
        else if (agent.AgentFunction != null && agent.AgentFunction != "louis" && !AlwaysLoadedFunctions.Contains(agent.AgentFunction))
        {
            // Fallback: try to load as a generic markdown file for custom agent functions
            string mainFile = $"{agent.AgentFunction}-skills.md";
            string? mainPath = FindSkillFile(agent, mainFile);
            if (mainPath is not null)
            {
                var customProvider = new MarkdownSkillProvider(mainPath, includeDefaultInstructions: false);
                composite.AddProvider(customProvider);
                Console.Error.WriteLine($"[INFO] Loaded {mainFile} via MarkdownSkillProvider");
            }
            else
            {
                Console.Error.WriteLine($"[WARN] Skills file '{mainFile}' not found; using default skills only.");
            }
        }

        return composite;
    }

    private static ISkillProvider? LoadAllSkillProviders(AgentOptions agent, CompositeSkillProvider composite)
    {
        Console.Error.WriteLine($"[INFO] Loading ALL skill providers (louis function)");

        var devopsProvider = new DevOpsSkillsProvider(FindSkillFile(agent, "devops-skills.md"));
        composite.AddProvider(devopsProvider);
        Console.Error.WriteLine($"[INFO] Loaded DevOpsSkillsProvider");

        var paymoProvider = new PaymoSkillsProvider(FindSkillFile(agent, "paymo-skills.md"));
        composite.AddProvider(paymoProvider);
        Console.Error.WriteLine($"[INFO] Loaded PaymoSkillsProvider");

        var codeReviewProvider = new CodeReviewSkillsProvider(FindSkillFile(agent, "code-review-skills.md"));
        composite.AddProvider(codeReviewProvider);
        Console.Error.WriteLine($"[INFO] Loaded CodeReviewSkillsProvider");

        var timeLoggingProvider = new TimeLoggingSkillsProvider(FindSkillFile(agent, "time-logging-skills.md"));
        composite.AddProvider(timeLoggingProvider);
        Console.Error.WriteLine($"[INFO] Loaded TimeLoggingSkillsProvider");

        // Any other *-skills.md dropped into the skills folder (including ones the agent created) loads without code changes
        foreach (string file in DiscoverSkillFiles(agent).Where(f => !BuiltInSkillFiles.Contains(Path.GetFileName(f))))
        {
            composite.AddProvider(new MarkdownSkillProvider(file, includeDefaultInstructions: false));
            Console.Error.WriteLine($"[INFO] Discovered skills file: {Path.GetFileName(file)}");
        }

        return null; // Already added to composite in this method
    }

    /// <summary>Guidance for toolsets registered in every session (dotnet-skills.md has its own provider).</summary>
    private static readonly string[] AlwaysLoadedSkillFiles = ["python-skills.md", "powershell-skills.md", "bash-skills.md", "web-skills.md", "self-extension-skills.md"];

    /// <summary>Functions whose skills file is loaded for every agent, so it must not be loaded a second time.</summary>
    private static readonly HashSet<string> AlwaysLoadedFunctions = new(StringComparer.OrdinalIgnoreCase) { "dotnet", "python", "powershell", "bash", "web" };

    /// <summary>Skill files loaded by a dedicated provider; discovery skips them to avoid loading them twice.</summary>
    internal static readonly HashSet<string> BuiltInSkillFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "devops-skills.md", "paymo-skills.md", "code-review-skills.md", "time-logging-skills.md",
        "dotnet-skills.md", "python-skills.md", "powershell-skills.md", "bash-skills.md", "web-skills.md", "self-extension-skills.md",
    };

    /// <summary>The folder holding default.md and the *-skills.md files, or null if none was found.</summary>
    public static string? FindSkillsDirectory(AgentOptions agent) =>
        FindSkillFile(agent, "default.md") is { } defaultFile ? Path.GetDirectoryName(defaultFile) : null;

    internal static IEnumerable<string> DiscoverSkillFiles(AgentOptions agent) =>
        FindSkillsDirectory(agent) is { } dir
            ? Directory.EnumerateFiles(dir, "*-skills.md").Order(StringComparer.OrdinalIgnoreCase)
            : [];

    internal static string? FindSkillFile(AgentOptions agent, string fileName)
    {
        // Working directory and its ancestors, so running from src/<project> still finds the repo's Skills/ folder.
        var ancestors = new List<string>();
        for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
            ancestors.Add(dir.FullName);

        var directories = new[] { agent.SkillsDirectory }.Concat(ancestors).Concat(["/app", AppContext.BaseDirectory]);
        var searchPaths = directories
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .SelectMany(d => new[]
            {
                Path.Combine(d!, fileName),
                Path.Combine(d!, "Skills", fileName)
            })
            .ToList();

        return searchPaths.FirstOrDefault(File.Exists);
    }
}
