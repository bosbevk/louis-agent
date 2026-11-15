using LouisAgent.Testing;

namespace LouisAgent.Web.Testing.Sites;

/// <summary>Where one of our sites lives on disk, by its project's name: a demo's, or the framework's own.</summary>
public static class SiteProject
{
    /// <summary>The folders our sites live under.</summary>
    private static readonly string[] Homes = ["demos", "src/site"];

    /// <summary>The site's project folder, such as the one <c>louis-agent.starter</c> is in.</summary>
    public static string Folder(string project) =>
        Homes
            .SelectMany(home => Directory.EnumerateDirectories(Path.Combine(TestPaths.RepoRoot, home), project,
                SearchOption.AllDirectories))
            .Single();
}
