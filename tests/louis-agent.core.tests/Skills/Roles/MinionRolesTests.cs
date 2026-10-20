using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Roles;

// F30 step 4: a minion (level 2) is a function on the minion base; the specialist hands it one of its own tools (Louis:
// no list of tools per role, the specialist's toolbox is the only one). CL15: on the tests' own minion (Prompts/minions/).
public class MinionRolesTests
{
    [Test]
    public void TheMinionRoles_AreListed_TheBaseIsNot()
    {
        // The base (testprod-minion.md) frames every minion; it is no role a specialist can name.
        Assert.That(RoleCatalog.Available(RoleLevel.Minion), Is.EqualTo(new[] { "testprod-minion-lookup" }));
        Assert.That(RoleCatalog.MinionBaseOf("testprod-minion-lookup"), Is.EqualTo("testprod-minion"));
        Assert.That(RoleCatalog.MinionBaseOf("testprod-specialist"), Is.Null, "a specialist's role has no base");
        Assert.That(RoleCatalog.IsMinionBase("testprod-minion"), Is.True);
        Assert.That(RoleCatalog.IsMinionBase("testprod-minion-lookup"), Is.False);
    }

    [Test]
    public void AMinionsPrompt_IsThePrinciples_TheBase_ThenItsRole_AndNoRouter()
    {
        string docs = SkillLoader.Load(new AgentOptions(), [], "testprod-minion-lookup", RoleLevel.Minion).Documentation;

        int principles = docs.IndexOf("# Principles", StringComparison.Ordinal);
        int @base = docs.IndexOf("You are a **minion**", StringComparison.Ordinal);
        int role = docs.IndexOf("# Testprod minion: Lookup", StringComparison.Ordinal);
        Assert.That(principles, Is.GreaterThanOrEqualTo(0), "the principles open every prompt (ADR 0020)");
        Assert.That(@base, Is.GreaterThan(principles), "the base after the principles");
        Assert.That(role, Is.GreaterThan(@base), "the role after the base");
        Assert.That(docs, Does.Not.Contain("# Louis Agent - Default Instructions"), "no router: a minion never delegates");
    }

    [Test]
    public void AMinion_AlwaysHasARole_AndTheBaseOrAnUnlistedFile_IsNoRole()
    {
        // A minion with no role would get the default voice: a coding agent's, with one tool. Refused instead.
        Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], null, RoleLevel.Minion));
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "testprod-minion", RoleLevel.Minion))!;
        Assert.That(ex.Message, Does.Contain("testprod-minion'").And.EndWith("Available: testprod-minion-lookup."));
    }
}
