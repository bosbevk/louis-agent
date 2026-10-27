using LouisAgent.Choices;
namespace LouisAgent.Core.Hosting;

using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging.Content;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Core.Hosting.Messages;
using LouisAgent.Core.Hosting.Rider;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.Logging;

/// <summary>
/// Single bootstrap shared by the CLI, ACP, API and MCP entry points: the flow only (S, CL15). The .env files are
/// <see cref="HostEnvironment"/>, the roles <see cref="RoleFiles"/>, the session models <see cref="SessionLlmLoader"/>,
/// the tables <see cref="PriceTableLoader"/> and <see cref="ExchangeRatesLoader"/>, the ledger <see cref="UsageLedger"/>,
/// the prompt <c>SkillLoader</c> (through the engine factory) and Rider's tools <see cref="RiderMcpToolDiscovery"/>.
/// </summary>
public static class AgentHost
{
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(AgentHost));

    /// <summary>
    /// Builds an engine for hosts that only publish its tools (the MCP server): the calling client brings its own
    /// model, so no LLM provider or API key is needed. It has every toolset this instance can load (F5); skills,
    /// agent-built tools and approval work as usual.
    /// </summary>
    public static AgentEngine BuildToolHost() => BuildToolHost(AgentCatalog.CoreOnly);

    /// <summary>The same, with the identities and packs the host was given (CL15): their prompts registered, the roles checked.</summary>
    public static AgentEngine BuildToolHost(AgentCatalog catalog)
    {
        HostEnvironment.Load();
        var agent = AgentOptions.FromEnvironment();
        AgentLogging.Configure(agent.LogDirectory, LogContent.ModeOf(agent.LogContent));
        SkillFileSource.Use(catalog.Prompts);
        RoleFiles.Check(agent, RoleCatalog.DefaultVoice);
        var engines = new AgentEngineFactory(new ToolsOnlyChatClient(), agent, catalog: catalog);
        return engines.GetOrCreate(engines.Available);
    }

    public static AgentHostContext Build(ILlmClientFactory? clientFactory = null) => Build(AgentCatalog.CoreOnly, clientFactory);

    /// <summary>The same, with the identities and packs the host was given (CL15): their prompts registered, the roles checked.</summary>
    public static AgentHostContext Build(AgentCatalog catalog, ILlmClientFactory? clientFactory = null)
    {
        HostEnvironment.Load();

        var llm = LlmOptions.FromEnvironment();
        var agent = AgentOptions.FromEnvironment();
        AgentLogging.Configure(agent.LogDirectory, LogContent.ModeOf(agent.LogContent));
        return Build(llm, agent, clientFactory ?? new LlmClientFactory(), catalog);
    }

    /// <summary>
    /// Bootstraps a host: the model client, prices, rates, the usage ledger and an <see cref="AgentEngineFactory"/> that
    /// builds one engine per toolset profile (F5). <see cref="AgentHostContext.Engine"/> is the host's own engine, with
    /// every toolset this instance can load: what the CLI and the ACP server use, one session per process. The API
    /// builds each session's engine from <see cref="AgentHostContext.Engines"/>.
    /// </summary>
    public static AgentHostContext Build(LlmOptions llm, AgentOptions agent, ILlmClientFactory clientFactory, AgentCatalog? catalog = null)
    {
        catalog ??= AgentCatalog.CoreOnly;
        // The identities' and packs' prompts, before any is read (CL15); then every ROLES_ENABLED name and the default
        // mode must resolve, so a missing identity stops the host here, not at its first chat.
        SkillFileSource.Use(catalog.Prompts);
        RoleFiles.Check(agent, RoleCatalog.DefaultVoice);
        var chatClient = clientFactory.Create(llm);
        PriceTable prices = PriceTableLoader.Load(agent);
        ExchangeRates rates = ExchangeRatesLoader.Load(agent, prices.Currency);
        DisplayCurrency display = DisplayCurrency.Resolve(agent, prices.Currency, rates);
        Log.Log(LogLevel.Information, HostingLogMessage.DisplayCurrencyChosen, new { Display = display });
        // The profiles sit beside .env: the repo's config/ when run from it, the mounted workspace's in a container.
        string configDirectory = EnvFile.FindConfigDirectory(Environment.CurrentDirectory) ?? Path.Combine(agent.WorkspaceRoot, "config");
        var sessionLlms = SessionLlmLoader.Load(agent, clientFactory, configDirectory, Environment.GetEnvironmentVariable);
        SessionLlmLoader.CheckMinionLlms(agent);
        var engines = new AgentEngineFactory(chatClient, agent, llm.ResolveSupportsTools(), UsageLedger.Open(agent, prices),
            clientFactory.CreateUsageMapper(llm), display, llm.ResolveThinking(), rates: rates, sessionLlms: sessionLlms, catalog: catalog);
        var engine = engines.GetOrCreate(engines.Available);
        Log.Log(LogLevel.Information, HostingLogMessage.LlmChosen,
            new { Llm = llm, Tools = engine.SupportsTools ? "on" : "off", Thinking = engine.Thinking?.Wire() ?? "off" });

        RiderMcpToolDiscovery.StartInBackground(engine, agent);

        return new AgentHostContext(engine, llm, agent, prices, rates, display, engines);
    }
}
