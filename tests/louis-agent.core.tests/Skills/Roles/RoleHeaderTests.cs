using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Roles;

// F33 step 4 (Louis: "we want the cook to have the tool always … puls should not pass it"): a role's file declares the
// toolsets the role always loads, in a header the loader reads and strips, so the model never sees it:
//     ---
//     toolsets: shops?
//     ---
// Whatever its parent passes is added to them; `?` (optional) loads only where this instance has the toolset.
public class RoleHeaderTests
{
    [Test]
    public void AHeader_GivesTheRolesToolsets_AndTheTextWithoutIt()
    {
        var (toolsets, body) = RoleHeader.Split("---\ntoolsets: web, shops?\n---\n# Puls specialist: Local cook\n");

        Assert.That(toolsets, Is.EqualTo(new[] { CoreToolset.Web.Name(), "shops?" }));
        Assert.That(body, Is.EqualTo("# Puls specialist: Local cook\n"));
    }

    [Test]
    public void AHeaderWithWindowsLineEndings_IsReadTheSame() =>
        Assert.That(RoleHeader.Split("---\r\ntoolsets: shops?\r\n---\r\n# Cook\r\n"), Is.EqualTo((new[] { "shops?" }, "# Cook\r\n")).Using(new HeaderComparer()));

    [TestCase("# Puls specialist: Local cook\n\nYou are …")]
    [TestCase("")]
    [TestCase("--- not a header\ntext")]
    public void NoHeader_IsNoToolsets_AndTheTextAsItIs(string text)
    {
        var (toolsets, body) = RoleHeader.Split(text);

        Assert.That(toolsets, Is.Empty);
        Assert.That(body, Is.EqualTo(text));
    }

    [Test]
    public void ARole_AlwaysLoadsItsHeadersToolsets_WhereThisInstanceHasThem() =>
        Assert.That(RoleCatalog.ToolsetsOf("testprod-specialist", RoleLevel.Specialist), Is.EqualTo(new[] { "web?" }));

    [Test]
    public void ARoleWithoutAHeader_DeclaresNone() =>
        Assert.That(RoleCatalog.ToolsetsOf("louis-agent/voice", RoleLevel.Agent), Is.Empty);

    [Test]
    public void EveryBuiltInRolesHeader_NamesToolsetsThatExist()
    {
        foreach (var level in Enum.GetValues<RoleLevel>())
            foreach (string role in RoleCatalog.Available(level))
                foreach (string toolset in RoleCatalog.ToolsetsOf(role, level))
                    Assert.That(ToolsetCatalog.Find(ToolsetCatalog.NameOf(toolset)), Is.Not.Null, $"{role} names '{toolset}'");
    }

    [Test]
    public void TheModel_NeverSeesTheHeader()
    {
        var options = new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null };
        string prompt = SkillLoader.Load(options, [], "testprod-specialist", RoleLevel.Specialist).Documentation;

        Assert.That(prompt, Does.Contain("Testprod specialist").And.Not.Contain("toolsets: web?"));
    }

    private sealed class HeaderComparer : IEqualityComparer<(IReadOnlyList<string>, string)>
    {
        public bool Equals((IReadOnlyList<string>, string) x, (IReadOnlyList<string>, string) y) => x.Item1.SequenceEqual(y.Item1) && x.Item2 == y.Item2;
        public int GetHashCode((IReadOnlyList<string>, string) obj) => obj.Item2.GetHashCode();
    }
}
