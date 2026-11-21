using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Hosting;

/// <summary>
/// F127.3.1: a session that may ask other agents is told whom, from its identity's <c>Calls</c>, so the one list
/// that is checked in code is also the one the model reads.
/// </summary>
public class AgentEngineFactoryCallsTests
{
    /// <summary>
    /// A factory over the test product as an identity that may ask acme and its cook, with a peer so the agents
    /// toolset loads.
    /// </summary>
    private static AgentEngineFactory Factory() =>
        new(new FakeChatClient(), new AgentOptions
        {
            WorkspaceRoot = TestPaths.RepoRoot,
            SkillsDirectory = TestPaths.Skills,
            DefaultVoice = "testprod/voice-en",
            AgentPeers = new Dictionary<string, Uri> { ["self"] = new("http://self.test/") },
            RatesUrl = null
        }, catalog: new AgentCatalog(
            [new FakeIdentity("testprod", voice: "testprod/voice-en", calls: ["acme", "acme.cook"])],
            TestTools.Catalog.Packs));

    /// <summary>The identity's mode, named or its default, with the agents toolset, lists the addresses.</summary>
    [TestCase("testprod/voice-en")]
    [TestCase(null)]
    public void ASessionThatMayAsk_IsToldWhom(string? mode) =>
        Assert.That(Factory().GetOrCreate([CoreToolset.Agents.Wire()], mode).SkillDocumentation,
            Does.Contain("- `acme`").And.Contain("- `acme.cook`"));

    /// <summary>A session without the agents toolset is told of nobody to ask.</summary>
    [Test]
    public void ASessionWithoutTheToolset_IsToldOfNobody() =>
        Assert.That(Factory().GetOrCreate([TestToolset.Web.Wire()], "testprod/voice-en").SkillDocumentation,
            Does.Not.Contain("- `acme.cook`"));
}
