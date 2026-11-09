namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// The sites built on the framework's web, found on disk, so a new site comes under the rule without anyone listing it.
/// </summary>
internal static class FrameworkWebSites
{
    /// <summary>
    /// Every project under a demo's <c>web</c> folder, and the framework's own site under <c>src/site</c> (F124), that
    /// references <c>louis-agent.web</c>.
    /// </summary>
    public static IReadOnlyList<string> Folders() =>
    [
        .. Directory.EnumerateDirectories(Path.Combine(TestPaths.RepoRoot, "demos"))
            .Select(demo => $"{demo}/web")
            .Append(Path.Combine(TestPaths.RepoRoot, "src", "site"))
            .Where(Directory.Exists)
            .SelectMany(Directory.EnumerateDirectories)
            .Where(ReferencesTheFramework)
            .Order()
    ];

    /// <summary>A site's own name, from its folder: <c>louis-agent.puls</c> is <c>puls</c>.</summary>
    public static string NameOf(string folder) => Path.GetFileName(folder).Replace("louis-agent.", "");

    /// <summary>A project is on the framework if its project file points at the framework's.</summary>
    private static bool ReferencesTheFramework(string folder) =>
        Directory.EnumerateFiles(folder, "*.csproj")
            .Any(project => File.ReadAllText(project).Contains("louis-agent.web.csproj"));
}
