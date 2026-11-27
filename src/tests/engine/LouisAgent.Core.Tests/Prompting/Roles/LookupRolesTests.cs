using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Roles;

namespace LouisAgent.Core.Tests.Prompting.Roles;

// F30 step 4: a lookup agent (level 2) is a function on the lookup agent base; the subagent hands it one of its own
// tools (Louis: no list of tools per role, the subagent's toolbox is the only one). CL68.3: on the tests' own lookup
// agent, agents/testprod/lookups/lookup/.
public class LookupRolesTests
{
    [Test]
    public void TheLookupAgentRoles_AreListed_TheBaseIsNot()
    {
        // CL68.3: a lookup agent is listed by its address; the base frames every one of its identity's and is no role.
        Assert.That(RoleCatalog.Available(RoleLevel.Lookup), Is.EqualTo(new[] { "testprod.lookup" }));
        Assert.That(RoleCatalog.LookupBaseOf("testprod.lookup"), Is.EqualTo("agents/testprod/lookup.md"));
        Assert.That(RoleCatalog.FileName("testprod.lookup", RoleLevel.Lookup),
            Is.EqualTo("agents/testprod/lookups/lookup/voice.md"), "inside its agent, apart from its subagents");
        Assert.That(RoleCatalog.Available(RoleLevel.Subagent), Does.Not.Contain("testprod.lookup"));
        Assert.That(RoleCatalog.LookupBaseOf("testprod/voice-en"), Is.Null, "a mode has no lookup base");
    }

    [Test]
    public void ALookupAgentsPrompt_IsThePrinciples_TheBase_ThenItsRole_AndNoRouter()
    {
        string docs = SkillLoader.Load(new AgentOptions(), [], "testprod.lookup", RoleLevel.Lookup)
            .Documentation;

        int principles = docs.IndexOf("# Principles", StringComparison.Ordinal);
        int @base = docs.IndexOf("You are a **lookup agent**", StringComparison.Ordinal);
        int role = docs.IndexOf("# Testprod lookup agent: Lookup", StringComparison.Ordinal);
        Assert.That(principles, Is.GreaterThanOrEqualTo(0), "the principles open every prompt (ADR 0020)");
        Assert.That(@base, Is.GreaterThan(principles), "the base after the principles");
        Assert.That(role, Is.GreaterThan(@base), "the role after the base");
        Assert.That(docs, Does.Not.Contain("# Testprod - Routing"), "no router: a lookup agent never delegates");
    }

    [Test]
    public void ALookupAgent_AlwaysHasARole_AndTheBaseOrAnUnlistedFile_IsNoRole()
    {
        // A lookup agent with no role would get the default voice: a coding agent's, with one tool. Refused instead.
        Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], null, RoleLevel.Lookup));
        var ex = Assert.Throws<RoleException>(() =>
            SkillLoader.Load(new AgentOptions(), [], "testprod-helper", RoleLevel.Lookup))!;
        Assert.That(ex.Message, Does.Contain("testprod-helper'").And.EndWith("Available: testprod.lookup."));
    }
}
