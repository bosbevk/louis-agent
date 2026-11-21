using System.Xml.Linq;

namespace LouisAgent.Core.Tests;

/// <summary>
/// A pack is its tools and runs on the server; its web views run in the browser. Neither references the other, so
/// someone can take a pack without any web, and a browser never carries a pack's database driver.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public class PackWebBoundaryTests
{
    /// <summary>The project files under this top-level folder.</summary>
    private static IEnumerable<string> ProjectsIn(string folder) =>
        Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, folder), "*.csproj", SearchOption.AllDirectories);

    /// <summary>What this project references, as paths from the top of the repository.</summary>
    private static IEnumerable<string> ReferencesOf(string project)
    {
        var folder = Path.GetDirectoryName(project)!;
        return XDocument.Load(project).Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")!.Value.Replace('\\', '/'))
            .Select(path => Path.GetFullPath(Path.Combine(folder, path)))
            .Select(path => Path.GetRelativePath(TestPaths.RepoRoot, path).Replace('\\', '/'));
    }

    /// <summary>Each offending reference, as "project -> what it references".</summary>
    private static IEnumerable<string> Offending(string folder, Func<string, bool> isForbidden) =>
        ProjectsIn(folder).SelectMany(project => ReferencesOf(project).Where(isForbidden)
            .Select(reference => $"{Path.GetFileName(project)} -> {reference}"));

    /// <summary>A pack knows nothing of the web: not the framework's, nor its own views.</summary>
    [Test]
    public void NoPack_ReferencesTheWeb() =>
        Assert.That(
            Offending("packs/tools",
                reference => reference.StartsWith("src/web/") || reference.StartsWith("packs/web/")),
            Is.Empty);

    /// <summary>A pack's web views know nothing of any pack: they read what the API hands them.</summary>
    [Test]
    public void NoPacksWebViews_ReferenceAPack() =>
        Assert.That(Offending("packs/web", reference => reference.StartsWith("packs/tools/")), Is.Empty);

    /// <summary>A site in a browser takes a pack's web views, and never the pack.</summary>
    [TestCase("demos/puls/web")]
    [TestCase("demos/louis-agent/web")]
    [TestCase("demos/starter/web")]
    public void NoSite_ReferencesAPack(string folder) =>
        Assert.That(Offending(folder, reference => reference.StartsWith("packs/tools/")), Is.Empty);
}
