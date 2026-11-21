using LouisAgent.Testing;

namespace LouisAgent.Web.Testing.Sites;

/// <summary>
/// Where a site lives on disk, by its project's name, wherever its repository keeps it: a demo's, the framework's own,
/// or a site in a repository of its own, such as Puls under its src/ (Louis's pick A of F166.3's finding 8).
/// </summary>
public static class SiteProject
{
    /// <summary>The folders a walk never enters: build output and what a tool keeps, never a project of ours.</summary>
    private static readonly HashSet<string> Skipped = ["bin", "obj", ".git", "node_modules"];

    /// <summary>The site's project folder in this repository, such as the one <c>LouisAgent.Starter</c> is in.</summary>
    public static string Folder(string project) => Folder(project, TestPaths.RepoRoot);

    /// <summary>The site's project folder in the repository at this root; there must be exactly one.</summary>
    public static string Folder(string project, string root) =>
        FoldersBelow(root).Where(folder => Path.GetFileName(folder) == project).Single();

    /// <summary>Every folder below this one, depth first, leaving out what <see cref="Skipped"/> names.</summary>
    private static IEnumerable<string> FoldersBelow(string folder)
    {
        foreach (string child in Directory.EnumerateDirectories(folder))
        {
            if (Skipped.Contains(Path.GetFileName(child))) continue;
            yield return child;
            foreach (string below in FoldersBelow(child)) yield return below;
        }
    }
}
