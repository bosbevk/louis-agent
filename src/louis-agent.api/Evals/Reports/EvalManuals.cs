using LouisAgent.Api.Evals.List;

namespace LouisAgent.Api.Evals.Reports;

/// <summary>
/// Reads an eval's manual, the markdown file that sits beside the eval, so the portal can show it. The path comes from
/// the catalogue, never from a caller, and it has to stay inside one of the workspace's eval folders: <c>evals/</c>,
/// or <c>identities/</c> and <c>packs/</c>, where an eval that is a folder of files sits beside what it proves (F49.2).
/// A path that points elsewhere just means no manual, never a way out.
/// </summary>
public static class EvalManuals
{
    /// <summary>The folders, from the workspace root, a manual may be read from.</summary>
    private static readonly string[] Roots = ["evals", "identities", "packs"];

    /// <summary>The manual's text, if the eval has one.</summary>
    public static string? Read(string workspaceRoot, EvalEntry check) =>
        PathOf(workspaceRoot, check) is { } path ? File.ReadAllText(path) : null;

    /// <summary>Whether the eval has a manual we can show.</summary>
    public static bool Has(string workspaceRoot, EvalEntry check) => PathOf(workspaceRoot, check) is not null;

    /// <summary>Finds the manual on disk, but only if it exists and lies inside one of the eval folders.</summary>
    /// <remarks>Guard clauses: no path, then outside the folders or missing.</remarks>
    private static string? PathOf(string workspaceRoot, EvalEntry check)
    {
        if (check.ManualPath is not { Length: > 0 } relative) return null;

        // Resolve both to full paths, so a "../" in the catalogue can't climb out of a folder unnoticed.
        var path = Path.GetFullPath(Path.Combine(workspaceRoot, relative));

        // Windows paths ignore case, Linux paths don't.
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var inside = Roots
            .Select(root => Path.GetFullPath(Path.Combine(workspaceRoot, root)) + Path.DirectorySeparatorChar)
            .Any(folder => path.StartsWith(folder, comparison));
        return inside && File.Exists(path) ? path : null;
    }
}
