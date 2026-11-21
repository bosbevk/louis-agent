using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Hosting;

/// <summary>
/// F129 (ADR 0114): a session gets ReadSkill only when its identity has a skill to read on demand, so no other
/// session is told of a tool it has no use for.
/// </summary>
public class AgentEngineFactorySkillsTests
{
    /// <summary>A factory over the test product, whose voice has an on-demand skill beside it.</summary>
    private static AgentEngineFactory Factory() =>
        new(new FakeChatClient(), new AgentOptions
        {
            WorkspaceRoot = TestPaths.RepoRoot,
            SkillsDirectory = TestPaths.Skills,
            DefaultVoice = "testprod/voice-en",
            RatesUrl = null,
        }, catalog: TestTools.Catalog);

    /// <summary>A session of the identity's mode, named or its default, can read its skills.</summary>
    [TestCase("testprod/voice-af")]
    [TestCase(null)]
    public void AModeWithOnDemandSkills_HasReadSkill(string? mode) =>
        Assert.That(Factory().GetOrCreate([TestToolset.Web.Wire()], mode).Tools.Select(t => t.Name),
            Does.Contain("ReadSkill"));

    /// <summary>A specialist is no identity's voice, so it has none to read.</summary>
    [Test]
    public void ASpecialist_HasNoReadSkill() =>
        Assert.That(Factory().GetOrCreate([TestToolset.Web.Wire()], "testprod-specialist", RoleLevel.Specialist)
            .Tools.Select(t => t.Name), Does.Not.Contain("ReadSkill"));
}
