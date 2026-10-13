namespace LouisAgent.Core.Hosting;

using dotenv.net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Usage;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// Single bootstrap shared by the CLI, ACP and MCP entry points: dotenv load, option binding,
/// skill discovery and composite wiring.
/// </summary>
public static partial class AgentHost
{
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(AgentHost));

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
        string? configDir = EnvFile.FindConfigDirectory(startDirectory);
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
            Log.Log(LogLevel.Warning, LogMessage.LlmProfileMissing, new { Profile = profile, File = $"config/.env.{profile}" });
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

    /// <summary>
    /// Builds an engine for hosts that only publish its tools (the MCP server): the calling client brings its own
    /// model, so no LLM provider or API key is needed. It has every toolset this instance can load (F5); skills,
    /// agent-built tools and approval work as usual.
    /// </summary>
    public static AgentEngine BuildToolHost()
    {
        LoadEnvironment();
        var agent = AgentOptions.FromEnvironment();
        AgentLogging.Configure(agent.LogDirectory);
        var engines = new AgentEngineFactory(new ToolsOnlyChatClient(), agent);
        return engines.GetOrCreate(engines.Available);
    }

    /// <summary>Stand-in for hosts that never prompt a model themselves.</summary>
    private sealed class ToolsOnlyChatClient : Microsoft.Extensions.AI.IChatClient
    {
        public Task<Microsoft.Extensions.AI.ChatResponse> GetResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(MessageText.Format(ErrorMessage.ToolHostHasNoModel));

        public IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, Microsoft.Extensions.AI.ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(MessageText.Format(ErrorMessage.ToolHostHasNoModel));

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    public static AgentHostContext Build(ILlmClientFactory? clientFactory = null)
    {
        LoadEnvironment();

        var llm = LlmOptions.FromEnvironment();
        var agent = AgentOptions.FromEnvironment();
        AgentLogging.Configure(agent.LogDirectory);
        return Build(llm, agent, clientFactory ?? new LlmClientFactory());
    }

    /// <summary>
    /// Bootstraps a host: the model client, prices, rates, the usage ledger and an <see cref="AgentEngineFactory"/> that
    /// builds one engine per toolset profile (F5). <see cref="AgentHostContext.Engine"/> is the host's own engine, with
    /// every toolset this instance can load: what the CLI and the ACP server use, one session per process. The API
    /// builds each session's engine from <see cref="AgentHostContext.Engines"/>.
    /// </summary>
    public static AgentHostContext Build(LlmOptions llm, AgentOptions agent, ILlmClientFactory clientFactory)
    {
        var chatClient = clientFactory.Create(llm);
        PriceTable prices = LoadPrices(agent);
        ExchangeRates rates = LoadRates(agent, prices.Currency);
        DisplayCurrency display = DisplayCurrency.Resolve(agent, prices.Currency, rates);
        Log.Log(LogLevel.Information, LogMessage.DisplayCurrencyChosen, new { Display = display });
        var engines = new AgentEngineFactory(chatClient, agent, llm.ResolveSupportsTools(), CreateUsageSink(agent, prices),
            clientFactory.CreateUsageMapper(llm), display, llm.ResolveThinking());
        var engine = engines.GetOrCreate(engines.Available);
        Log.Log(LogLevel.Information, LogMessage.LlmChosen,
            new { Llm = llm, Tools = engine.SupportsTools ? "on" : "off", Thinking = engine.Thinking?.ToString().ToLowerInvariant() ?? "off" });

        // Auto-discover and register tools from Rider's MCP server (async, non-blocking)
        if (agent.RiderMcpAutoDiscover && !string.IsNullOrWhiteSpace(agent.RiderMcpEndpoint))
        {
            Log.Log(LogLevel.Information, LogMessage.McpDiscoveryStarting, new { Endpoint = agent.RiderMcpEndpoint });
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
                    Log.Log(LogLevel.Warning, LogMessage.McpStartupFailed, new { Error = ex.ToString() }, tag: "MCP");
                }
            });
        }
        else
        {
            Log.Log(LogLevel.Information, LogMessage.McpDiscoveryDisabled);
        }

        return new AgentHostContext(engine, llm, agent, prices, rates, display, engines);
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
            Log.Log(LogLevel.Warning, LogMessage.LedgerOff);
            return null;
        }

        Log.Log(LogLevel.Information, LogMessage.LedgerAt, new { Path = Path.Combine(agent.LogDirectory, "usage-YYYY-MM.jsonl") });
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
                throw new InvalidDataException(MessageText.Format(ErrorMessage.PricesUrlInvalid, new { Url = pricesUrl }));

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = PricesUrlTimeout;
            try
            {
                // Start-up is synchronous in every host; the call is bounded by the timeout above.
                PriceTable fromUrl = PriceTable.LoadAsync(http, url).GetAwaiter().GetResult();
                Log.Log(LogLevel.Information, LogMessage.PricesLoaded, new { Source = url, fromUrl.AsOf, fromUrl.Models.Count });
                return fromUrl;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Pricing being unreachable never stops an agent: the local table keeps costs flowing.
                Log.Log(LogLevel.Warning, LogMessage.PricesUrlUnreachable, new { Url = url, Reason = ex.Message });
            }
        }

        string? path = agent.PricesFile
                       ?? (EnvFile.FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "prices.json") : null);
        PriceTable prices = path is null ? PriceTable.Empty : PriceTable.Load(path);
        if (prices.IsEmpty)
            Log.Log(LogLevel.Warning, LogMessage.NoPriceTable, new { Path = path ?? "config/prices.json" });
        else
            Log.Log(LogLevel.Information, LogMessage.PricesLoaded, new { Source = path, prices.AsOf, prices.Models.Count });
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
                throw new InvalidDataException(MessageText.Format(ErrorMessage.RatesUrlInvalid, new { Url = ratesUrl }));

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = RatesUrlTimeout;
            try
            {
                // Start-up is synchronous in every host; the call is bounded by the timeout above.
                ExchangeRates fromUrl = ExchangeRates.LoadAsync(http, url, mapper).GetAwaiter().GetResult();
                CheckBase(fromUrl, baseCurrency, url.ToString());
                Log.Log(LogLevel.Information, LogMessage.RatesLoaded, new { Source = url, fromUrl.AsOf, fromUrl.Rates.Count });
                return fromUrl;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or WrongBaseException)
            {
                // A third party's reply changing shape, its currency, or being down, never stops an agent: the rates file keeps
                // working. (Frankfurter's v2 reads ?from= as a date, so a v1 alias dropped would answer in EUR.)
                Log.Log(LogLevel.Warning, LogMessage.RatesUrlUnusable, new { Url = url, Reason = ex.Message });
            }
        }

        string? path = agent.RatesFile
                       ?? (EnvFile.FindConfigDirectory(Environment.CurrentDirectory) is { } configDir ? Path.Combine(configDir, "rates.json") : null);
        ExchangeRates rates = path is null ? ExchangeRates.Empty : ExchangeRates.Load(path);
        if (rates.IsEmpty)
        {
            Log.Log(LogLevel.Warning, LogMessage.NoRates, new { Path = path ?? "config/rates.json", Base = baseCurrency });
            return rates;
        }

        CheckBase(rates, baseCurrency, path!);
        Log.Log(LogLevel.Information, LogMessage.RatesLoaded, new { Source = path, rates.AsOf, rates.Rates.Count });
        return rates;
    }

    /// <summary>Rates from another currency than the price table's would convert every cost wrongly.</summary>
    private static void CheckBase(ExchangeRates rates, string baseCurrency, string source)
    {
        if (!string.Equals(rates.Base, baseCurrency, StringComparison.OrdinalIgnoreCase))
            throw new WrongBaseException(
                MessageText.Format(ErrorMessage.RatesWrongBase, new { Source = source, rates.Base, Currency = baseCurrency }));
    }

    /// <summary>
    /// Rates from another currency than the price table's. From RATES_FILE, our own configuration, it stops start-up; from
    /// RATES_URL it falls back to the file, since a provider changing its reply is not something the operator can fix.
    /// </summary>
    internal sealed class WrongBaseException(string message) : Exception(message);

    /// <summary>The system prompt with every toolset's guide (hosts that load every toolset, and tests).</summary>
    public static ISkillProvider LoadSkills(AgentOptions agent) => LoadSkills(agent, ToolsetCatalog.All);

    /// <summary>
    /// Loads the system prompt for a toolset profile (F5): personality, default.md (the router), the guide of each of
    /// <paramref name="toolsets"/> and no other (docs/CLAUDE.md, *Adding a toolset*), then, when the profile has the skills
    /// toolset, the function's own skill file ({AgentFunction}-skills.md; "louis" loads every skill file). Our own skill files are plain markdown read by
    /// <see cref="MarkdownSkillProvider"/>; a provider class of its own is only for skills we don't control.
    /// Built-in files are read from disk when there (<see cref="AgentOptions.SkillsDirectory"/>, the working directory
    /// and its parents, /app), else from the copy embedded in the assembly (<see cref="SkillFileSource"/>).
    /// </summary>
    /// <param name="personality">The session's personality (F21): a name in <c>Skills/personalities/</c>, or null for
    /// <c>personality.md</c>. <see cref="PersonalityException"/> for a name that isn't valid or has no file.</param>
    public static ISkillProvider LoadSkills(AgentOptions agent, IEnumerable<ToolsetDefinition> toolsets, string? personality = null)
    {
        var composite = new CompositeSkillProvider();

        // Personality first, so who the agent is frames everything after it; default.md (the router) after it
        AddBuiltIn(agent, composite, PersonalityFile(agent, personality));
        AddBuiltIn(agent, composite, "default.md");

        // The guides of this profile's toolsets only, so the prompt never describes tools the session doesn't have
        var profile = toolsets.ToList();
        foreach (string fileName in ToolsetCatalog.GuideFiles(profile))
            AddBuiltIn(agent, composite, fileName);

        // Function skill files are procedures ExecuteSkill runs: they come with the skills toolset, like a guide with its
        // tools. A profile without it (Puls's chats: agents only) gets the router and its guides, not every skill file.
        if (profile.All(t => t.Name != SkillsToolset)) return composite;

        if (agent.AgentFunction == "louis")
        {
            LoadAllSkillFiles(agent, composite);
        }
        else if (agent.AgentFunction != null && ToolsetCatalog.Find(agent.AgentFunction) is null)
        {
            // The function's own skill file: code-review-skills.md, time-logging-skills.md, or a custom one (a toolset's
            // name is not a function: its guide comes with the toolset)
            string mainFile = $"{agent.AgentFunction}-skills.md";
            if (ReadSkillFile(agent, mainFile) is { } main)
            {
                composite.AddProvider(MarkdownSkillProvider.FromText(main.Text));
                Log.Log(LogLevel.Information, LogMessage.MainSkillsFileLoaded, new { File = mainFile });
            }
            else
            {
                Log.Log(LogLevel.Warning, LogMessage.MainSkillsFileMissing, new { File = mainFile });
            }
        }

        return composite;
    }

    private static void LoadAllSkillFiles(AgentOptions agent, CompositeSkillProvider composite)
    {
        Log.Log(LogLevel.Information, LogMessage.LoadingAllSkills);

        foreach (string fileName in FunctionSkillFiles)
            AddBuiltIn(agent, composite, fileName);

        // Any other *-skills.md dropped into the skills folder (including ones the agent created) loads without code changes
        foreach (string file in DiscoverSkillFiles(agent).Where(f => !SkillFiles.BuiltIn.Contains(Path.GetFileName(f))))
        {
            composite.AddProvider(new MarkdownSkillProvider(file, includeDefaultInstructions: false));
            Log.Log(LogLevel.Information, LogMessage.SkillsFileDiscovered, new { File = Path.GetFileName(file) });
        }
    }

    /// <summary>Skill files of functions that aren't toolsets, loaded by "louis" (and by their own AGENT_FUNCTION).</summary>
    private static readonly string[] FunctionSkillFiles = ["code-review-skills.md", "time-logging-skills.md"];

    /// <summary>The toolset the function skill files come with (ExecuteSkill runs their procedures).</summary>
    private const string SkillsToolset = "skills";

    /// <summary>The folder holding default.md and the *-skills.md files, or null if none was found.</summary>
    public static string? FindSkillsDirectory(AgentOptions agent) =>
        FindSkillFile(agent, "default.md") is { } defaultFile ? Path.GetDirectoryName(defaultFile) : null;

    internal static IEnumerable<string> DiscoverSkillFiles(AgentOptions agent) =>
        FindSkillsDirectory(agent) is { } dir
            ? Directory.EnumerateFiles(dir, "*-skills.md").Order(StringComparer.OrdinalIgnoreCase)
            : [];

    internal static string? FindSkillFile(AgentOptions agent, string fileName) => SkillFilePaths(agent, fileName).FirstOrDefault(File.Exists);

    /// <summary>A skill file's text from disk (which overrides) or the assembly; null when neither has it.</summary>
    internal static (string Text, bool FromDisk)? ReadSkillFile(AgentOptions agent, string fileName) =>
        SkillFileSource.Read(fileName, SkillFilePaths(agent, fileName));

    /// <summary>
    /// The skill file of <paramref name="personality"/>: <see cref="Personalities.DefaultFile"/> when none is named, else
    /// its file in <c>personalities/</c>, which must exist on disk or in the assembly.
    /// </summary>
    private static string PersonalityFile(AgentOptions agent, string? personality)
    {
        if (string.IsNullOrEmpty(personality)) return Personalities.DefaultFile;
        if (!Personalities.IsValidName(personality))
            throw new PersonalityException(MessageText.Format(ErrorMessage.PersonalityNameInvalid, new { Name = personality }));
        string file = Personalities.FileName(personality);
        if (ReadSkillFile(agent, file) is null)
            throw new PersonalityException(MessageText.Format(ErrorMessage.UnknownPersonality,
                new { Name = personality, Available = string.Join(", ", Personalities.Available(agent.SkillsDirectory)) }));
        return file;
    }

    /// <summary>Adds a built-in skill file to the prompt, from disk or the assembly; a missing one is skipped.</summary>
    private static void AddBuiltIn(AgentOptions agent, CompositeSkillProvider composite, string fileName)
    {
        if (ReadSkillFile(agent, fileName) is not { } file) return;
        composite.AddProvider(MarkdownSkillProvider.FromText(file.Text));
        Log.Log(LogLevel.Information, file.FromDisk ? LogMessage.SkillProviderLoaded : LogMessage.SkillFileEmbedded,
            new { Provider = fileName });
    }

    /// <summary>Where a skill file may be on disk, in order: the skills folder, the working directory and its parents
    /// (so running from src/&lt;project&gt; finds the repo's Skills/), /app, the program's folder.</summary>
    private static IEnumerable<string> SkillFilePaths(AgentOptions agent, string fileName)
    {
        var ancestors = new List<string>();
        for (var dir = new DirectoryInfo(Environment.CurrentDirectory); dir is not null; dir = dir.Parent)
            ancestors.Add(dir.FullName);

        return new[] { agent.SkillsDirectory }.Concat(ancestors).Concat(["/app", AppContext.BaseDirectory])
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .SelectMany(d => new[] { Path.Combine(d!, fileName), Path.Combine(d!, "Skills", fileName) })
            .ToList();
    }
}
