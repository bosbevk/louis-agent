using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

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

        Assert.That(toolsets, Is.EqualTo(new[] { "web", "shops?" }));
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
    public void TheCook_AlwaysLoadsTheShops_WhereThisInstanceHasThem() =>
        Assert.That(Personalities.ToolsetsOf("puls-cook", SkillLevel.Specialist), Is.EqualTo(new[] { "shops?" }));

    [Test]
    public void ARoleWithoutAHeader_DeclaresNone() =>
        Assert.That(Personalities.ToolsetsOf("louis-agent/voice", SkillLevel.Agent), Is.Empty);

    [Test]
    public void EveryBuiltInRolesHeader_NamesToolsetsThatExist()
    {
        foreach (var level in Enum.GetValues<SkillLevel>())
            foreach (string role in Personalities.Available(level))
                foreach (string toolset in Personalities.ToolsetsOf(role, level))
                    Assert.That(ToolsetCatalog.Find(ToolsetCatalog.NameOf(toolset)), Is.Not.Null, $"{role} names '{toolset}'");
    }

    [Test]
    public void TheModel_NeverSeesTheHeader()
    {
        var options = new AgentOptions { WorkspaceRoot = TestPaths.RepoRoot, RatesUrl = null };
        string prompt = AgentHost.LoadSkills(options, [], "puls-cook", SkillLevel.Specialist).Documentation;

        Assert.That(prompt, Does.Contain("Local cook").And.Not.Contain("toolsets: shops?"));
    }

    private sealed class HeaderComparer : IEqualityComparer<(IReadOnlyList<string>, string)>
    {
        public bool Equals((IReadOnlyList<string>, string) x, (IReadOnlyList<string>, string) y) => x.Item1.SequenceEqual(y.Item1) && x.Item2 == y.Item2;
        public int GetHashCode((IReadOnlyList<string>, string) obj) => obj.Item2.GetHashCode();
    }
}
