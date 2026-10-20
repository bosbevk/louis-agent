namespace LouisAgent.Core.Tools.Workspace.Listing;

/// <summary>What the listing and search tools skip (S, CL15, from WorkspaceTools): build output and tool folders, by name; what the text search reads, by extension.</summary>
internal static class WorkspaceFilter
{
    internal static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".idea", ".vs", "bin", "obj", "node_modules", "packages"
    };

    private static readonly HashSet<string> SearchableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".props", ".targets", ".json", ".md", ".yml", ".yaml",
        ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs", ".java", ".xml", ".config", ".txt"
    };

    /// <summary>A symbolic link, or a directory named like build output or a tool's folder.</summary>
    public static bool IsExcluded(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
        (Directory.Exists(path) && ExcludedDirectories.Contains(Path.GetFileName(path)));

    /// <summary>A source or documentation file, by its extension.</summary>
    public static bool IsSearchable(string path) => SearchableExtensions.Contains(Path.GetExtension(path));
}
