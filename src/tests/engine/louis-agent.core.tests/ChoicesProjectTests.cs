using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The one parser for a name that is an enum member lives in its own project, so a pack, an identity, an eval set, a
/// host or the browser can all use it. That only holds while it references nothing: not a project of ours (it would
/// drag the rest of the solution after it) and not a package (it would make a stranger's pack carry it). Core
/// references it, so packs, identities and eval sets get it through core.
/// </summary>
public class ChoicesProjectTests
{
    /// <summary>A project file's text, from its path under the repo.</summary>
    private static string Csproj(params string[] parts) =>
        File.ReadAllText(Path.Combine([TestPaths.RepoRoot, .. parts]));

    /// <summary>The project references nothing.</summary>
    [Test]
    public void TheChoicesProject_ReferencesNothing()
    {
        string csproj = Csproj("src", "engine", "louis-agent.choices", "louis-agent.choices.csproj");

        var references = Regex.Matches(csproj, "<(ProjectReference|PackageReference) Include=\"([^\"]+)\"")
            .Select(m => $"{m.Groups[1].Value} {m.Groups[2].Value}").ToList();

        Assert.That(references, Is.Empty,
            "a project that everyone can use references nothing in the solution and no package");
    }

    /// <summary>Core references it, so packs and identities get it through core.</summary>
    [Test]
    public void Core_ReferencesIt_SoPacksAndIdentitiesGetItThroughCore()
    {
        Assert.That(Csproj("src", "engine", "louis-agent.core", "louis-agent.core.csproj"),
            Does.Contain(@"louis-agent.choices\louis-agent.choices.csproj"));
    }

    /// <summary>The browser side references it directly, since it can't reach core.</summary>
    [Test]
    public void TheBrowserSide_ReferencesItDirectly_SinceItCannotReachCore()
    {
        Assert.That(Csproj("src/web/louis-agent.web", "louis-agent.web.csproj"),
            Does.Contain(@"louis-agent.choices\louis-agent.choices.csproj"));
    }
}
