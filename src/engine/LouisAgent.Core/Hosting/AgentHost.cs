using LouisAgent.Composition.Logging.Content;
using LouisAgent.Composition.Logging;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Core.Hosting.Messages;
using LouisAgent.Core.Hosting.Messages.Holes;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Roles;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Hosting;

/// <summary>
/// Single bootstrap shared by the CLI, ACP, API and MCP entry points: the flow only (S, CL15). The .env files are
/// <see cref="HostEnvironment"/>, the roles <see cref="RoleFiles"/>, the session models
/// <see cref="SessionLlmLoader"/>, the tables <see cref="PriceTableLoader"/> and <see cref="ExchangeRatesLoader"/>,
/// the ledger <see cref="UsageLedger"/>, the models a pack's tool may be given <see cref="ToolModels"/> (F171.1), the
/// prompt <c>SkillLoader</c> (through the engine factory), and whatever <see cref="HostServices"/> the host gave: its
/// web, its traces and its tool discovery (CL33, CL57; none means none).
/// </summary>
public static class AgentHost
{
    /// <summary>The bootstrap's own lines: the display currency and the model it chose.</summary>
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(AgentHost));

    /// <summary>
    /// Builds an engine for hosts that only publish its tools (the MCP server): the calling client brings its own
    /// model, so no LLM provider or API key is needed. It has every toolset this instance can load (F5); the skills the
    /// agent keeps are reached through <c>UseSkill</c>, as in any chat.
    /// </summary>
    public static AgentEngine BuildToolHost() => BuildToolHost(AgentCatalog.CoreOnly);

    /// <summary>
    /// The same, with the identities and packs the host was given (CL15): their prompts registered, the roles checked.
    /// </summary>
    public static AgentEngine BuildToolHost(AgentCatalog catalog, HostServices? services = null)
    {
        HostEnvironment.Load();
        var agent = AgentOptions.FromEnvironment();
        var telemetry = (services ?? HostServices.None).TelemetryOrNone;
        AgentLogging.Configure(agent.LogDirectory, LogContent.ModeOf(agent.LogContent), telemetry.AddSinks);
        // After the .env files load, so OTEL_* there reach the tracer under Rider too (F52).
        telemetry.Start();
        SkillFileSource.Use(catalog.Prompts);
        agent.DefaultVoice ??= catalog.DefaultVoice;
        RoleFiles.Check(agent, agent.DefaultVoice);
        var engines = new AgentEngineFactory(new ToolsOnlyChatClient(), agent, services: services, catalog: catalog);
        return engines.GetOrCreate(engines.Available);
    }

    /// <summary>
    /// Builds a host with core alone, from the environment and the .env files: the model, prices, rates, the ledger
    /// and its engines.
    /// </summary>
    public static AgentHostContext Build(ILlmClientFactory? clientFactory = null) =>
        Build(AgentCatalog.CoreOnly, clientFactory);

    /// <summary>
    /// The same, with the identities and packs the host was given (CL15): their prompts registered, the roles checked.
    /// </summary>
    public static AgentHostContext Build(AgentCatalog catalog, ILlmClientFactory? clientFactory = null) =>
        Build(catalog, HostServices.None, clientFactory);

    /// <summary>
    /// The same, with what the host's program composes besides the catalogue (CL33): its telemetry, started once the
    /// .env files have loaded.
    /// </summary>
    public static AgentHostContext Build(AgentCatalog catalog, HostServices services,
        ILlmClientFactory? clientFactory = null) =>
        Build(_ => catalog, services, clientFactory);

    /// <summary>
    /// The same, for a program whose packs need a model (F171.1): the catalogue is built by
    /// <paramref name="catalogFor"/>, called once with the models a pack's tool may be given, each already traced,
    /// replayed and written to the ledger, so the program picks one for each pack.
    /// </summary>
    public static AgentHostContext Build(Func<ToolModels, AgentCatalog> catalogFor, HostServices services,
        ILlmClientFactory? clientFactory = null)
    {
        HostEnvironment.Load();

        var llm = LlmOptions.FromEnvironment();
        var agent = AgentOptions.FromEnvironment();
        var telemetry = services.TelemetryOrNone;
        AgentLogging.Configure(agent.LogDirectory, LogContent.ModeOf(agent.LogContent), telemetry.AddSinks);
        // After the .env files load, so OTEL_* there reach the tracer under Rider too (F52).
        telemetry.Start();
        return Build(llm, agent, clientFactory ?? services.ClientFactory(), catalogFor, services);
    }

    /// <summary>
    /// Bootstraps a host with the catalogue it was given; see the overload that takes the catalogue's function.
    /// </summary>
    public static AgentHostContext Build(LlmOptions llm, AgentOptions agent, ILlmClientFactory clientFactory,
        AgentCatalog? catalog = null, HostServices? services = null) =>
        Build(llm, agent, clientFactory, _ => catalog ?? AgentCatalog.CoreOnly, services);

    /// <summary>
    /// Bootstraps a host: the model client, prices, rates, the usage ledger, the eval's recordings, the models a
    /// pack's tool may be given, the catalogue built from them, and an <see cref="AgentEngineFactory"/> that builds
    /// one engine per toolset profile (F5). <see cref="AgentHostContext.Engine"/> is the host's own engine, with every
    /// toolset this instance can load: what the CLI and the ACP server use, one session per process. The API builds
    /// each session's engine from <see cref="AgentHostContext.Engines"/>.
    /// </summary>
    public static AgentHostContext Build(LlmOptions llm, AgentOptions agent, ILlmClientFactory clientFactory,
        Func<ToolModels, AgentCatalog> catalogFor, HostServices? services = null)
    {
        var chatClient = clientFactory.Create(llm);
        PriceTable prices = PriceTableLoader.Load(agent);
        ExchangeRates rates = ExchangeRatesLoader.Load(agent, prices.Currency);
        DisplayCurrency display = DisplayCurrency.Resolve(agent, prices.Currency, rates);
        var currency = new DisplayCurrencyChosenHoles(display);
        Log.Log(LogLevel.Information, HostingLogMessage.DisplayCurrencyChosen, currency);
        // The profiles sit beside .env: the repo's config/ when run from it, the mounted workspace's in a container.
        string configDirectory = EnvFile.FindConfigDirectory(Environment.CurrentDirectory) ??
                                 Path.Combine(agent.WorkspaceRoot, "config");
        var sessionLlms =
            SessionLlmLoader.Load(agent, clientFactory, configDirectory, Environment.GetEnvironmentVariable);
        SessionLlmLoader.CheckLookupLlms(agent);

        // The ledger and the eval's recordings first, so the models a pack's tool is given write and replay where the
        // engines do (F171.1); then the catalogue, built from them.
        var ledger = UsageLedger.Open(agent, prices);
        var usageMapper = clientFactory.CreateUsageMapper(llm);
        var recordings = ReplayStore.From(agent);
        var models = ToolModels.Create(chatClient, usageMapper, sessionLlms, ledger, recordings);
        var catalog = catalogFor(models);

        // The identities' and packs' prompts, before any is read (CL15); then every ROLES_ENABLED name and the default
        // mode must resolve, so a missing identity stops the host here, not at its first chat.
        SkillFileSource.Use(catalog.Prompts);
        agent.DefaultVoice ??= catalog.DefaultVoice;
        RoleFiles.Check(agent, agent.DefaultVoice);
        var engines = new AgentEngineFactory(chatClient, agent, llm.ResolveSupportsTools(), ledger, usageMapper,
            display, llm.ResolveThinking(), rates: rates, sessionLlms: sessionLlms, catalog: catalog,
            services: services, recordings: recordings, staleThinking: clientFactory.CreateStaleThinking(llm));
        var engine = engines.GetOrCreate(engines.Available);
        var chosen = new LlmChosenHoles(llm, engine.SupportsTools ? "on" : "off", engine.Thinking?.Wire() ?? "off");
        Log.Log(LogLevel.Information, HostingLogMessage.LlmChosen, chosen);

        // What the host's program finds tools with (Rider's discovery under the ACP server); none looks nowhere.
        (services ?? HostServices.None).DiscoveryOrNone.Start(engine);

        return new AgentHostContext(engine, llm, agent, prices, rates, display, engines);
    }
}
