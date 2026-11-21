namespace LouisAgent.Core.Tests;

/// <summary>
/// A project's own C# files, for the rule tests that read source as text. Two of them listed a project's files
/// the same way, so the listing is here once (ADR 0123).
/// </summary>
internal static class ProjectSources
{
    /// <summary>
    /// Every C# file of a project, with its path under the repo, forward slashes on every platform, and its text.
    /// What a build wrote, in <c>bin</c> and <c>obj</c>, is left out.
    /// </summary>
    public static List<(string Path, string Text)> Of(string project) =>
    [
        .. Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, project), "*.cs", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(file => !file.Contains("/bin/") && !file.Contains("/obj/"))
            .Select(file => (file, File.ReadAllText(Path.Combine(TestPaths.RepoRoot, file))))
    ];
}
