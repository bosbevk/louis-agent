using LouisAgent.Core.Agent.EngineTools.Skills;
using LouisAgent.Core.Skills.Headers.Identities;

namespace LouisAgent.Core.Tests.Agent.EngineTools.Skills;

/// <summary>
/// F129 (ADR 0114): ReadSkill hands the model one of its identity's on-demand skills, by the name the prompt's list
/// gives, and for a name that isn't one says which it has.
/// </summary>
public class SkillReadToolsTests
{
    /// <summary>The test product's skills, as a session of its mode holds them.</summary>
    private static SkillReadTools Tools() => new(IdentitySkills.Of("testprod"));

    /// <summary>A listed skill is read whole, without its header.</summary>
    [Test]
    public void AListedSkill_IsReadWhole()
    {
        string text = Tools().ReadSkill("testprod/price-list");

        Assert.That(text, Does.Contain("# Testprod - Price list").And.Contain("A small thing costs R100."));
        Assert.That(text, Does.Not.Contain("load: on-demand"));
    }

    /// <summary>
    /// A name that isn't one of its on-demand skills, an always one included (it's in the prompt already), names the
    /// ones it can read.
    /// </summary>
    [TestCase("testprod/menu")]
    [TestCase("testprod/house-rules")]
    [TestCase("price-list")]
    public void ANameThatIsNoOnDemandSkill_NamesTheOnesThereAre(string name)
    {
        string text = Tools().ReadSkill(name);

        Assert.That(text, Does.Contain($"'{name}'").And.Contain("testprod/price-list"));
        Assert.That(text, Does.Not.Contain("A small thing costs R100."));
    }
}
