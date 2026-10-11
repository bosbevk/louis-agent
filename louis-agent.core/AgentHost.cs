namespace louis_agent.core;

using dotenv.net;
using louis_agent.core.config;
using louis_agent.core.tools;
using louis_agent.core.providers;
using louis_agent.core.mcp;
using louis_agent.core.usage;

/// <summary>Everything an entry point needs after bootstrap.</summary>
/// <param name="Prices">The price table the usage ledger prices with (louis-agent.api also serves it at GET /prices).</param>
/// <param name="Rates">Exchange rates from the price table's currency, for showing costs in another currency (F14).</param>
/// <param name="Display">The currency costs are shown in (F14): HOST_CULTURE's region, DISPLAY_CURRENCY, or as charged.</param>
public sealed record AgentHostContext(
    AgentEngine Engine, LlmOptions Llm, AgentOptions Agent, PriceTable Prices, ExchangeRates Rates, DisplayCurrency Display);

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
        PriceTable prices = LoadPrices(agent);
        ExchangeRates rates = LoadRates(agent, prices.Currency);
        DisplayCurrency display = DisplayCurrency.Resolve(agent, prices.Currency, rates);
        Console.Error.WriteLine($"[INFO] Display currency: {display}");
        var engine = new AgentEngine(skills, chatClient, agent, llm.ResolveSupportsTools(),
            usageSink: CreateUsageSink(agent, prices), usageMapper: clientFactory.CreateUsageMapper(llm))
        {
            SkillsDirectory = FindSkillsDirectory(agent),
            SkillReloader = () => LoadSkills(agent),
            Thinking = llm.ResolveThinking(),
            Display = display,
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

        return new AgentHostContext(engine, llm, agent, prices, rates, display);
    }

    /// <summary>
    /// The usage ledger, {LOG_DIRECTORY}/usage-YYYY-MM.jsonl, with each record priced from the price table; or null when
    /// USAGE_LEDGER=off or there is no LOG_DIRECTORY (with a warning, since the ledger was wanted). A malformed price table
    /// throws, so a host doesn't start on wrong prices.
    /// </summary>
    /// <param name="prices">Already loaded prices (<see cref="Build(LlmOptions, AgentOptions, ILlmClientFactory)"/> loads them once); null loads them here.</param>
    public static IUsageSink? CreateUsageSink(AgentOptions agent, PriceTable? prices = null)
    {
        if (!agent.UsageLedger) return null;
        if (agent.LogDirectory is null)
        {
            Console.Error.WriteLine("[WARN] Usage ledger off: LOG_DIRECTORY is not set. Set it to record token usage, or USAGE_LEDGER=off to silence this.");
            return null;
        }

        Console.Error.WriteLine($"[INFO] Usage ledger: {Path.Combine(agent.LogDirectory, "usage-YYYY-MM.jsonl")}");
        return new PricingUsageSink(new JsonlUsageSink(agent.LogDirectory), prices ?? LoadPrices(agent));
    }

    private static readonly TimeSpan PricesUrlTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The price table: from PRICES_URL when set and reachable, otherwise from PRICES_FILE or config/prices.json (found like
    /// config/.env); empty, with a warning, if there's none. A response or file that isn't a valid table throws.
    /// </summary>
    /// <param name="handler">For tests: answers the PRICES_URL request.</param>
    public static PriceTable LoadPrices(AgentOptions agent, HttpMessageHandler? handler = null)
    {
        if (agent.PricesUrl is { } pricesUrl)
        {
            if (!Uri.TryCreate(pricesUrl, UriKind.Absolute, out Uri? url) || url.Scheme is not ("http" or "https"))
                throw new InvalidDataException($"PRICES_URL must be an http or https URL, got '{pricesUrl}'.");

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = PricesUrlTimeout;
            try
            {
                // Start-up is synchronous in every host; the call is bounded by the timeout above.
                PriceTable fromUrl = PriceTable.LoadAsync(http, url).GetAwaiter().GetResult();
                Console.Error.WriteLine($"[INFO] Prices: {url} (as of {fromUrl.AsOf}, {fromUrl.Models.Count} models)");
                return fromUrl;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Pricing being unreachable never stops an agent: the local table keeps costs flowing.
                Console.Error.WriteLine($"[WARN] Prices: {url} can't be reached ({ex.Message}); using the price file instead.");
            }
        }

        string? path = agent.PricesFile
                       ?? (FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "prices.json") : null);
        PriceTable prices = path is null ? PriceTable.Empty : PriceTable.Load(path);
        if (prices.IsEmpty)
            Console.Error.WriteLine($"[WARN] No price table at {path ?? "config/prices.json"}: usage is recorded without cost. Set PRICES_FILE to price it.");
        else
            Console.Error.WriteLine($"[INFO] Prices: {path} (as of {prices.AsOf}, {prices.Models.Count} models)");
        return prices;
    }

    private static readonly TimeSpan RatesUrlTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Exchange rates for showing costs in another currency (F14): from RATES_URL (Frankfurter by default), turned into our
    /// rates.json format by RATES_FORMAT's mapper; if that can't be fetched or read, from RATES_FILE or config/rates.json;
    /// empty, with a warning, if there's neither. Failures are sorted by who can fix them: anything wrong with RATES_URL's
    /// reply (down, a new shape, or rates from another currency) is the provider's, so it warns and uses the file; a rates
    /// file that isn't valid, or whose base isn't <paramref name="baseCurrency"/>, is our own configuration, so it throws:
    /// costs converted at a wrong rate are worse than not starting.
    /// </summary>
    /// <param name="baseCurrency">The price table's currency; the rates must be from it.</param>
    /// <param name="handler">For tests: answers the RATES_URL request.</param>
    public static ExchangeRates LoadRates(AgentOptions agent, string baseCurrency = "USD", HttpMessageHandler? handler = null)
    {
        IRatesMapper mapper = RatesMappers.For(agent.RatesFormat);
        if (agent.RatesUrl is { } ratesUrl)
        {
            if (!Uri.TryCreate(ratesUrl, UriKind.Absolute, out Uri? url) || url.Scheme is not ("http" or "https"))
                throw new InvalidDataException($"RATES_URL must be an http or https URL (or off), got '{ratesUrl}'.");

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = RatesUrlTimeout;
            try
            {
                // Start-up is synchronous in every host; the call is bounded by the timeout above.
                ExchangeRates fromUrl = ExchangeRates.LoadAsync(http, url, mapper).GetAwaiter().GetResult();
                CheckBase(fromUrl, baseCurrency, url.ToString());
                Console.Error.WriteLine($"[INFO] Rates: {url} (as of {fromUrl.AsOf}, {fromUrl.Rates.Count} currencies)");
                return fromUrl;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or WrongBaseException)
            {
                // A third party's reply changing shape, its currency, or being down, never stops an agent: the rates file keeps
                // working. (Frankfurter's v2 reads ?from= as a date, so a v1 alias dropped would answer in EUR.)
                Console.Error.WriteLine($"[WARN] Rates: {url} can't be used ({ex.Message}); using the rates file instead.");
            }
        }

        string? path = agent.RatesFile
                       ?? (FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "rates.json") : null);
        ExchangeRates rates = path is null ? ExchangeRates.Empty : ExchangeRates.Load(path);
        if (rates.IsEmpty)
        {
            Console.Error.WriteLine($"[WARN] No exchange rates at {path ?? "config/rates.json"}: costs are shown in {baseCurrency}.");
            return rates;
        }

        CheckBase(rates, baseCurrency, path!);
        Console.Error.WriteLine($"[INFO] Rates: {path} (as of {rates.AsOf}, {rates.Rates.Count} currencies)");
        return rates;
    }

    /// <summary>Rates from another currency than the price table's would convert every cost wrongly.</summary>
    private static void CheckBase(ExchangeRates rates, string baseCurrency, string source)
    {
        if (!string.Equals(rates.Base, baseCurrency, StringComparison.OrdinalIgnoreCase))
            throw new WrongBaseException(
                $"Exchange rates {source} are from {rates.Base}, but the price table is in {baseCurrency}: set RATES_URL to rates from {baseCurrency}.");
    }

    /// <summary>
    /// Rates from another currency than the price table's. From RATES_FILE, our own configuration, it stops start-up; from
    /// RATES_URL it falls back to the file, since a provider changing its reply is not something the operator can fix.
    /// </summary>
    internal sealed class WrongBaseException(string message) : Exception(message);

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
