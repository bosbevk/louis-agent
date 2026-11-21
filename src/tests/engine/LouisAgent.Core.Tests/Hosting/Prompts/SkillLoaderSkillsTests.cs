using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Headers.Identities;

namespace LouisAgent.Core.Tests.Hosting.Prompts;

/// <summary>
/// F129 (ADR 0114): an identity's skills sit beside its voice. The always ones are in the prompt right after the
/// voice; the on-demand ones are only listed, by name and by the line that says when to read them.
/// </summary>
public class SkillLoaderSkillsTests
{
    private static string PromptOf(string? mode = "testprod/voice-en", RoleLevel level = RoleLevel.Agent) =>
        SkillLoader.Load(new AgentOptions
        {
            SkillsDirectory = TestPaths.Skills,
            DefaultVoice = "testprod/voice-en"
        },
            TestTools.Catalog.Toolsets, mode, level).Documentation;

    /// <summary>An always skill is in the prompt after the voice and its routing, before the toolset guides.</summary>
    [Test]
    public void AnAlwaysSkill_FollowsTheVoice_BeforeTheGuides()
    {
        string prompt = PromptOf();

        int routing = prompt.IndexOf("# Testprod - Routing", StringComparison.Ordinal);
        int rules = prompt.IndexOf("# Testprod - House rules", StringComparison.Ordinal);
        int guide = prompt.IndexOf("# Files Skills", StringComparison.Ordinal);
        Assert.That((routing >= 0, rules > routing, guide > rules), Is.EqualTo((true, true, true)), prompt);
    }

    /// <summary>
    /// An on-demand skill's body stays out, and the prompt names it with the line that says when to read it.
    /// </summary>
    [Test]
    public void AnOnDemandSkill_IsListed_NotLoaded()
    {
        string prompt = PromptOf();

        Assert.That(prompt, Does.Not.Contain("A small thing costs R100."));
        Assert.That(prompt, Does.Contain(
            "`testprod/price-list`: The test product's prices. Read it when someone asks what something costs."));
    }

    /// <summary>A session that names no mode gets its identity's default, skills and all.</summary>
    [Test]
    public void TheDefaultMode_HasItsSkillsToo() =>
        Assert.That(PromptOf(mode: null), Does.Contain("# Testprod - House rules"));

    /// <summary>Below level 0 a role is no identity's voice, so no identity's skill comes with it.</summary>
    [Test]
    public void ASpecialist_GetsNoIdentitysSkill() =>
        Assert.That(PromptOf("testprod-specialist", RoleLevel.Specialist),
            Does.Not.Contain("# Testprod - House rules").And.Not.Contain("testprod/price-list"));

    /// <summary>A file in an identity's skills that doesn't say how it loads refuses, naming the file.</summary>
    [Test]
    public void ASkillWithoutItsLoad_Refuses()
    {
        var file = new PromptFile("agent/acme/skills/menu.md",
            new PromptHeader("menu", "The menu.", RoleLevel.Agent, []), "# Menu\n");

        var refused = Assert.Throws<PromptHeaderException>(() => IdentitySkill.Of("acme", file));

        Assert.That(refused!.Message, Does.Contain("agent/acme/skills/menu.md").And.Contain("load"));
    }
}
