using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Pages.Claims;

/// <summary>
/// What the framework stands on is real (F109.4): every package the Built on it page names is one a project of the
/// framework references. The repository is public since 2026-11-13, so the rule that no page links to it is gone.
/// </summary>
public class StandsOnTests
{
    /// <summary>Every package reference in the solution's projects, by name.</summary>
    private static HashSet<string> PackagesReferenced()
    {
        var projects = new[] { "src", "packs", "demos" }
            .SelectMany(folder => Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, folder), "*.csproj",
                SearchOption.AllDirectories));

        // A reference reads <PackageReference Include="Name" …/>; the name is between the first pair of quotes after
        // Include=.
        return projects.SelectMany(File.ReadLines)
            .Where(line => line.Contains("<PackageReference Include=\""))
            .Select(line => line.Split("Include=\"")[1].Split('"')[0])
            .ToHashSet();
    }

    /// <summary>Each package the page names is one the framework really references.</summary>
    [Test]
    public void EveryPackageNamed_IsOneTheFrameworkReferences()
    {
        using var ctx = new BunitContext();
        var referenced = PackagesReferenced();

        var cut = ctx.Render<BuiltOnItPage>();
        var named = cut.FindAll(".stands-on code").Select(code => code.TextContent).ToList();

        Assert.That(referenced, Has.Count.GreaterThan(20), "the project files were read");
        Assert.That(named, Has.Count.GreaterThanOrEqualTo(6), "the page names what it stands on");
        Assert.That(named.Where(package => !referenced.Contains(package)), Is.Empty);
    }
}
