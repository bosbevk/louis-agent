namespace LouisAgent.Core.Tools.Workspace.Listing;

using System.Text;

/// <summary>The tree ListDirectoryTree draws (S, CL15, from WorkspaceTools).</summary>
internal static class DirectoryTree
{
    /// <summary>
    /// One line per entry, directories first then by name, a branch per level and the last entry closing it, down to
    /// <paramref name="maxDepth"/> levels; excluded directories skipped, a folder that can't be read marked as such.
    /// </summary>
    public static string Draw(string directory, int maxDepth)
    {
        var sb = new StringBuilder();
        Draw(directory, "", 0, maxDepth, sb);
        return sb.ToString();
    }

    private static void Draw(string directory, string prefix, int depth, int maxDepth, StringBuilder sb)
    {
        if (depth >= maxDepth)
        {
            return;
        }

        try
        {
            var entries = Directory.EnumerateFileSystemEntries(directory)
                .Where(p => !WorkspaceFilter.IsExcluded(p))
                .OrderBy(p => !Directory.Exists(p))
                .ThenBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                bool isLast = i == entries.Count - 1;
                string icon = Directory.Exists(entry) ? "📁" : "📄";
                string name = Path.GetFileName(entry);

                sb.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{icon} {name}");

                if (Directory.Exists(entry))
                {
                    string newPrefix = prefix + (isLast ? "    " : "│   ");
                    Draw(entry, newPrefix, depth + 1, maxDepth, sb);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            sb.AppendLine($"{prefix}[Access Denied]");
        }
    }
}
