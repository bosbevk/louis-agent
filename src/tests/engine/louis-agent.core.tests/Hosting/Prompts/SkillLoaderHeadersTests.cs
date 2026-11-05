using LouisAgent.Testing;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Hosting.Prompts;

/// <summary>
/// CL27: the loader reads a built-in prompt's header through <see cref="LouisAgent.Core.Skills.Headers.PromptCatalog"/>
/// and hands the model only the body; a session asks it for the toolsets its role always loads (F33, now under the
/// header's <c>metadata</c>).
/// </summary>
public class SkillLoaderHeadersTests
{
    /// <summary>A role always loads the toolsets its header's metadata names, where this instance has them.</summary>
    [Test]
    public void ARole_AlwaysLoadsItsHeadersToolsets() =>
        Assert.That(SkillLoader.ToolsetsOf("testprod-specialist", RoleLevel.Specialist), Is.EqualTo(new[] { "web?" }));

    /// <summary>A role whose header names none, or a name that isn't a role, loads none of its own.</summary>
    [TestCase("louis-agent/voice", RoleLevel.Agent)]
    [TestCase("no-such-role", RoleLevel.Specialist)]
    [TestCase("../shared/principles", RoleLevel.Specialist)]
    public void ARoleThatNamesNone_LoadsNoneOfItsOwn(string role, RoleLevel level) =>
        Assert.That(SkillLoader.ToolsetsOf(role, level), Is.Empty);

    /// <summary>Every toolset a built-in role's header names is one the catalogue has.</summary>
    [Test]
    public void EveryBuiltInRolesHeader_NamesToolsetsThatExist()
    {
        foreach (var level in Enum.GetValues<RoleLevel>())
            foreach (string role in RoleCatalog.Available(level))
                foreach (string toolset in SkillLoader.ToolsetsOf(role, level))
                    Assert.That(TestTools.Catalog.Toolsets.Any(t => t.Name == ToolsetCatalog.NameOf(toolset)), Is.True,
                        $"{role} names '{toolset}'");
    }

    /// <summary>No prompt the model gets carries a header: not a mode's, a role's, a guide's or the principles'.</summary>
    [Test]
    public void TheModel_NeverSeesAHeader()
    {
        var options = new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null };
        string specialist = SkillLoader.Load(options, ToolsetCatalog.All, "testprod-specialist", RoleLevel.Specialist)
            .Documentation;
        string mode = SkillLoader.Load(options, ToolsetCatalog.All, "testprod/voice-af").Documentation;

        foreach (string prompt in new[] { specialist, mode })
            Assert.That(prompt, Does.Not.Contain("description:").And.Not.Contain("metadata:"));
        Assert.That(specialist, Does.Contain("# Testprod specialist"));
    }
}
