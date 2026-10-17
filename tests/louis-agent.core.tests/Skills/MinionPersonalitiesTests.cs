using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

// F30 step 4: a minion (level 2) is a function on the minion base; the specialist hands it one of its own tools (Louis:
// no list of tools per role, the specialist's toolbox is the only one).
public class MinionPersonalitiesTests
{
    [Test]
    public void TheMinionRoles_AreListed_TheBaseIsNot()
    {
        // The base (puls-minion.md) frames every minion; it is no role a specialist can name.
        Assert.That(Personalities.Available(SkillLevel.Minion), Is.EqualTo(new[] { "puls-minion-web-price-lookup" }));
        Assert.That(Personalities.MinionBaseOf("puls-minion-web-price-lookup"), Is.EqualTo("puls-minion"));
        Assert.That(Personalities.MinionBaseOf("puls-cook"), Is.Null, "a specialist's role has no base");
        Assert.That(Personalities.IsMinionBase("puls-minion"), Is.True);
        Assert.That(Personalities.IsMinionBase("puls-minion-web-price-lookup"), Is.False);
    }

    [Test]
    public void AMinionsPrompt_IsThePrinciples_TheBase_ThenItsRole_AndNoRouter()
    {
        string docs = AgentHost.LoadSkills(new AgentOptions(), [], "puls-minion-web-price-lookup", SkillLevel.Minion).Documentation;

        int principles = docs.IndexOf("# Principles", StringComparison.Ordinal);
        int @base = docs.IndexOf("You are a **minion**", StringComparison.Ordinal);
        int role = docs.IndexOf("# Puls minion: Web price lookup", StringComparison.Ordinal);
        Assert.That(principles, Is.GreaterThanOrEqualTo(0), "the principles open every prompt (ADR 0020)");
        Assert.That(@base, Is.GreaterThan(principles), "the base after the principles");
        Assert.That(role, Is.GreaterThan(@base), "the role after the base");
        Assert.That(docs, Does.Not.Contain("# Louis Agent - Default Instructions"), "no router: a minion never delegates");
    }

    [Test]
    public void AMinion_AlwaysHasARole_AndTheBaseOrAnUnlistedFile_IsNoRole()
    {
        // A minion with no role would get the default voice: a coding agent's, with one tool. Refused instead.
        Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(new AgentOptions(), [], null, SkillLevel.Minion));
        var ex = Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(new AgentOptions(), [], "puls-minion", SkillLevel.Minion))!;
        Assert.That(ex.Message, Does.Contain("puls-minion'").And.EndWith("Available: puls-minion-web-price-lookup."));
    }
}
