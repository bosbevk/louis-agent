using LouisAgent.Testing;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Headers;

/// <summary>
/// CL27: every built-in prompt's header is read once, by <see cref="PromptCatalog"/>, and everything that needs it asks
/// there: the loader for the body the model reads, a session for the toolsets its role always loads (F33, now under
/// <c>metadata</c>), the portal for the list. The model never sees a header.
/// </summary>
public class PromptCatalogTests
{
    /// <summary>The catalogue holds every built-in prompt, each with its header and body.</summary>
    [Test]
    public void TheCatalogue_HoldsEveryBuiltInPrompt_WithItsHeader()
    {
        var specialist = PromptCatalog.Get("specialists/testprod-specialist.md");

        Assert.That(PromptCatalog.All.Select(p => p.Path), Does.Contain("shared/principles.md"));
        Assert.That(PromptCatalog.All.Select(p => p.Path), Has.None.EndsWith("README.md"), "a folder's README is no prompt");
        Assert.That((specialist?.Header?.Name, specialist?.Header?.Level),
            Is.EqualTo(("testprod-specialist", (RoleLevel?)RoleLevel.Specialist)));
        Assert.That(specialist!.Body, Does.StartWith("# Testprod specialist"));
    }

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
                    Assert.That(ToolsetCatalog.Find(ToolsetCatalog.NameOf(toolset)), Is.Not.Null, $"{role} names '{toolset}'");
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
