namespace LouisAgent.Core.Guardrails;

/// <summary>
/// Keeps paths inside the workspace: relative only, never above the root, never through a symbolic link. The agent's
/// file tools and the API's workspace and git endpoints resolve every path through it.
/// </summary>
internal sealed class WorkspaceBoundary
{
    public WorkspaceBoundary(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Workspace root cannot be empty.", nameof(root));
        }

        Root = Path.GetFullPath(root);
    }

    /// <summary>The workspace root, as a full path.</summary>
    public string Root { get; }

    /// <summary>The full path of a workspace-relative one; throws <see cref="ArgumentException"/> when it would leave the workspace.</summary>
    public string Resolve(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Workspace paths must be relative.", nameof(relativePath));
        }

        string fullPath = Path.GetFullPath(Path.Combine(Root, relativePath));
        string relativeToRoot = Path.GetRelativePath(Root, fullPath);
        if (relativeToRoot == ".." ||
            relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativeToRoot))
        {
            throw new ArgumentException("Path must stay inside the workspace.", nameof(relativePath));
        }

        string? current = fullPath;
        while (current is not null && !current.Equals(Root, StringComparison.OrdinalIgnoreCase))
        {
            if ((Directory.Exists(current) || File.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new ArgumentException("Workspace paths cannot traverse symbolic links.", nameof(relativePath));
            }

            current = Path.GetDirectoryName(current);
        }

        return fullPath;
    }
}