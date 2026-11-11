using LouisAgent.Api.Sessions;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Packs.Calculator;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A role's prompt comes from the folder its file is in, the depth apart (F127.3.1 step 6, Louis's pick 1 A): a
/// business asked by another agent opens one level down in its own voice, with its own packs and no way to call
/// further. Every other mix of depth and folder is as before (<c>AgentSessionsTests.ARole_OpensOnlyAtItsOwnLevel</c>).
/// </summary>
public class AskedVoiceTests
{
    /// <summary>
    /// Fresh sessions on a catalogue of the business and its one pack, with a model that's never called.
    /// </summary>
    private static AgentSessions Sessions() =>
        new(new AgentEngineFactory(new NoModel(), new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot },
            catalog: new AgentCatalog([TestBusiness.Identity], [new CalculatorPack()])));

    /// <summary>
    /// Asked at depth 1, the business opens in its voice from <c>agent/</c>, with its own pack, and nothing that
    /// delegates.
    /// </summary>
    [Test]
    public void AnIdentitysVoice_AskedAtDepthOne_OpensAsItsVoice_WithItsPacks()
    {
        var session = Sessions().Create(depth: 1, identity: TestBusiness.Identity.Name);

        Assert.Multiple(() =>
        {
            Assert.That((session.Depth, session.Role), Is.EqualTo((1, "acme/voice")));
            Assert.That(session.Toolsets, Is.EqualTo(new[] { CalculatorToolset.Calculator.Wire() }));
            Assert.That(session.Engine.SkillDocumentation, Does.Contain("# acme"), "its own voice");
            Assert.That(session.Engine.Tools.Select(tool => tool.Name), Has.None.EqualTo("StartSubSession"));
        });
    }
}
