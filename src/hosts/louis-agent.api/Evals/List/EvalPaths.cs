namespace LouisAgent.Api.Evals.List;

/// <summary>
/// Turns a path the catalogue gave (an eval's manual, an eval's folder) into one on disk, but only when it lies inside
/// one of the workspace's eval folders: <c>evals/</c>, or <c>identities/</c> and <c>packs/</c>, where an eval that is a
/// folder of files sits beside what it proves. A path that points elsewhere gives nothing, never a way out.
/// </summary>
/// <remarks>
/// Extract Class (Fowler): the manual's guard moved here so the scenarios a sample is made from are read behind the
/// same one.
/// </remarks>
public static class EvalPaths
{
    /// <summary>The folders, from the workspace root, the catalogue's paths may lead into.</summary>
    private static readonly string[] Roots = ["src/evals", "demos", "packs", "packs-evals"];

    /// <summary>The full path, if it lies inside one of the eval folders.</summary>
    public static string? Inside(string workspaceRoot, string relative)
    {
        // Resolve both to full paths, so a "../" in the catalogue can't climb out of a folder unnoticed.
        var path = Path.GetFullPath(Path.Combine(workspaceRoot, relative));

        // Windows paths ignore case, Linux paths don't.
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var inside = Roots
            .Select(root => Path.GetFullPath(Path.Combine(workspaceRoot, root)) + Path.DirectorySeparatorChar)
            .Any(folder => path.StartsWith(folder, comparison));
        return inside ? path : null;
    }
}
