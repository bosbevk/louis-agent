using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Web;
using LouisAgent.Testing;

namespace LouisAgent.Core.Tests.Hosting;

/// <summary>
/// What a host's program composes (CL33) reaches the engine: the web access it hands <see cref="AgentHost.Build(AgentCatalog, HostServices, ILlmClientFactory)"/>
/// is the one every toolset context answers from, and none means the Null Object. The bug this pins: the
/// bootstrap started the telemetry but built its engine factory without the services, so every host read the web
/// through <see cref="NoWebAccess"/>.
/// </summary>
[NonParallelizable]
public class HostServicesTests
{
    [TearDown]
    public void TearDown() => AgentLogging.Configure(null);

    [Test]
    public void TheWebAccessTheProgramGave_IsTheEngines()
    {
        var web = new WebParts(new UnavailableSearchProvider("test", "a test's search"));

        AgentHostContext host = AgentHost.Build(new LlmOptions(), TestPaths.Agent(), new NoModelFactory(),
            services: new HostServices(Web: (_, _) => web));

        Assert.That(host.Engines.Context.Web, Is.SameAs(web));
    }

    [Test]
    public void NoServices_MeansTheEngineHasNoWebAccess()
    {
        AgentHostContext host = AgentHost.Build(new LlmOptions(), TestPaths.Agent(), new NoModelFactory());

        Assert.That(host.Engines.Context.Web, Is.SameAs(NoWebAccess.Instance));
    }

    private sealed class NoModelFactory : ILlmClientFactory
    {
        public Microsoft.Extensions.AI.IChatClient Create(LlmOptions options) => new FakeChatClient();
        public IUsageMapper CreateUsageMapper(LlmOptions options) => new StandardUsageMapper();
    }
}
