using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

// F18 step 7.5.2 (Louis: "say some other guy want to build his own packs or eval or anything pluggable, they must use wirename, so shared for all"): the one parser for a name that is an enum member lives in its own
// project, `louis-agent.choices`, so a pack, an identity, an eval set, a host or the browser can all use it. That only holds while it references nothing: not a project of ours (it would drag the rest of the solution
// after it) and not a package (it would make a stranger's pack carry it). Core references it, so packs, identities and eval sets get it through core.
public class ChoicesProjectTests
{
    private static string Csproj(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestPaths.RepoRoot, .. parts]));

    [Test]
    public void TheChoicesProject_ReferencesNothing()
    {
        string csproj = Csproj("louis-agent.choices", "louis-agent.choices.csproj");

        var references = Regex.Matches(csproj, "<(ProjectReference|PackageReference) Include=\"([^\"]+)\"")
            .Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}").ToList();

        Assert.That(references, Is.Empty,
            "a project that everyone can use references nothing in the solution and no package");
    }

    [Test]
    public void Core_ReferencesIt_SoPacksAndIdentitiesGetItThroughCore()
    {
        Assert.That(Csproj("louis-agent.core", "louis-agent.core.csproj"),
            Does.Contain(@"louis-agent.choices\louis-agent.choices.csproj"));
    }

    [Test]
    public void TheBrowserSide_ReferencesItDirectly_SinceItCannotReachCore()
    {
        Assert.That(Csproj("src", "louis-agent.web.shared", "louis-agent.web.shared.csproj"),
            Does.Contain(@"louis-agent.choices\louis-agent.choices.csproj"));
    }
}
