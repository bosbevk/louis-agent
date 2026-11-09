using System.Xml.Linq;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The framework's own website sits in <c>src/site/</c> (F124), built on the framework the way a customer's site is: it
/// may use packs and an identity, which nothing else in <c>src/</c> does, and nothing else in <c>src/</c> uses it, so the
/// engine, the hosts and the web can leave for their own repository without it.
/// </summary>
/// <remarks>
/// A fence like <c>ArchitectureTests</c>: it reads every project file under <c>src/</c> and follows each project
/// reference to the folder it points at, so a reference can't slip in through a relative path a reviewer misreads.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: the folders say what ships</seealso>
public class SiteBoundaryTests
{
    private static readonly string[] SiteFolders = ["src/site/", "src/tests/site/"];

    /// <summary>No project of the framework references the site: the site leans on the framework, never the reverse.</summary>
    [Test]
    public void NothingElseInSrc_ReferencesTheSite()
    {
        var found = FrameworkProjects()
            .SelectMany(project => Targets(project)
                .Where(target => target.StartsWith("src/site/", StringComparison.Ordinal))
                .Select(target => $"{RepoPath(project)} -> {target}"));

        Assert.That(found, Is.Empty);
    }

    /// <summary>
    /// Only the site may use a pack or an identity in <c>src/</c>: the framework ships no agent (ADR 0076), and its own
    /// site is the one place in it that runs one.
    /// </summary>
    [Test]
    public void OnlyTheSite_ReferencesAPackOrAnIdentity()
    {
        var found = FrameworkProjects()
            .SelectMany(project => Targets(project)
                .Where(target => target.StartsWith("packs/", StringComparison.Ordinal) ||
                                 target.Contains(".identity.", StringComparison.Ordinal))
                .Select(target => $"{RepoPath(project)} -> {target}"));

        Assert.That(found, Is.Empty);
    }

    /// <summary>Every project under <c>src/</c> but the site's own and its tests'.</summary>
    private static IEnumerable<string> FrameworkProjects() =>
        Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(project =>
                !SiteFolders.Any(folder => RepoPath(project).StartsWith(folder, StringComparison.Ordinal)))
            .Where(project => !RepoPath(project).Contains("/bin/", StringComparison.Ordinal) &&
                              !RepoPath(project).Contains("/obj/", StringComparison.Ordinal));

    /// <summary>Where each of a project's references points, from the repository's root, with forward slashes.</summary>
    private static IEnumerable<string> Targets(string project)
    {
        string folder = Path.GetDirectoryName(project)!;
        return XDocument.Load(project).Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .Select(include => RepoPath(Path.GetFullPath(Path.Combine(folder, include.Replace('\\', '/')))));
    }

    /// <summary>A path from the repository's root, with forward slashes, so the test reads the same on Linux.</summary>
    private static string RepoPath(string path) =>
        Path.GetRelativePath(TestPaths.RepoRoot, path).Replace('\\', '/');
}
