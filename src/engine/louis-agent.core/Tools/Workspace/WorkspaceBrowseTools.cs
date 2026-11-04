
using LouisAgent.Core.Guardrails.Workspace;
using System.ComponentModel;
using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Workspace.Listing;
using LouisAgent.Core.Tools.Workspace.Messages;

namespace LouisAgent.Core.Tools.Workspace;
/// <summary>Looking around one place (the files toolset, S: one of its five classes): a folder's entries, its tree, whether a path exists, a file's details.</summary>
public sealed class WorkspaceBrowseTools
{
    private readonly WorkspaceBoundary _boundary;
    private readonly string _root;

    public WorkspaceBrowseTools(string root)
    {
        _boundary = new WorkspaceBoundary(root);
        _root = _boundary.Root;
    }

    [Description(
        "List the immediate files and directories inside one workspace folder, each entry marked [file] or [dir]. This is not recursive — it only shows one level deep, and results are capped at 200 entries with a truncation notice beyond that. Paths must be relative to the workspace root. Use FindFiles for a recursive filename-pattern search or ListDirectoryTree for a recursive visual tree; use this when you just need the contents of a single known folder.")]
    public string ListWorkspaceFiles(
        [Description("Relative directory path to list; use '.' for the project root.")]
        string relativePath = ".")
    {
        string directory = ResolvePath(relativePath);
        if (!Directory.Exists(directory))
        {
            return MessageText.Format(WorkspaceToolMessage.DirectoryNotFound, new { Path = relativePath });
        }

        var entries = new List<string>();
        foreach (string path in Directory.EnumerateFileSystemEntries(directory)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            if (WorkspaceFilter.IsExcluded(path))
            {
                continue;
            }

            entries.Add($"{(Directory.Exists(path) ? "[dir] " : "[file] ")}{Path.GetRelativePath(_root, path)}");
            if (entries.Count == 200)
            {
                entries.Add(MessageText.Format(WorkspaceToolMessage.ListTruncated));
                break;
            }
        }

        return entries.Count == 0
            ? MessageText.Format(WorkspaceToolMessage.DirectoryEmpty)
            : string.Join(Environment.NewLine, entries);
    }

    [Description(
        "Return a file's metadata — size in bytes and a human-readable unit, creation time, last-modified time, and OS attributes — without reading its content. Use ReadWorkspaceFile instead when you need what's actually inside the file. Returns an error message if the path doesn't exist.")]
    public string GetFileInfo(
        [Description("Workspace-relative file path.")]
        string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (!File.Exists(path))
        {
            return MessageText.Format(ToolMessage.FileNotFound, new { Path = relativePath });
        }

        var info = new FileInfo(path);
        return $"Path: {Path.GetRelativePath(_root, path)}\n" +
               $"Size: {info.Length} bytes ({FileSizeText.Format(info.Length)})\n" +
               $"Created: {info.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"Attributes: {info.Attributes}";
    }

    [Description(
        "Render a visual tree of a directory's structure, recursively, down to a maximum depth (1-5, default 3) — useful for getting an overview of a folder's layout at a glance. Excluded directories (.git, node_modules, bin, obj, etc.) are skipped, the same as the other listing tools. Use ListWorkspaceFiles instead for a flat, single-level listing, or FindFiles when you need the actual matching file paths rather than a visual layout.")]
    public string ListDirectoryTree(
        [Description("Workspace-relative path; use '.' for the project root.")]
        string relativePath = ".",
        [Description("Maximum depth to display (1-5; default 3).")]
        int maxDepth = 3)
    {
        maxDepth = Math.Clamp(maxDepth, 1, 5);
        string path = ResolvePath(relativePath);
        if (!Directory.Exists(path))
        {
            return MessageText.Format(WorkspaceToolMessage.DirectoryNotFound, new { Path = relativePath });
        }

        var sb = new StringBuilder();
        sb.AppendLine(MessageText.Format(WorkspaceToolMessage.DirectoryTree,
            new { Path = Path.GetRelativePath(_root, path), Depth = maxDepth }));
        sb.Append(DirectoryTree.Draw(path, maxDepth));
        return sb.ToString();
    }

    [Description(
        "Check whether a workspace-relative path exists and report back whether it's a file, a directory, or doesn't exist at all. This is a cheap existence check only — it returns no metadata. Use GetFileInfo instead once you know a path exists and need its size or timestamps.")]
    public string PathExists(
        [Description("Workspace-relative path to check.")]
        string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (File.Exists(path))
        {
            return MessageText.Format(WorkspaceToolMessage.FileExists, new { Path = relativePath });
        }

        if (Directory.Exists(path))
        {
            return MessageText.Format(WorkspaceToolMessage.DirectoryExists, new { Path = relativePath });
        }

        return MessageText.Format(WorkspaceToolMessage.PathDoesNotExist, new { Path = relativePath });
    }

    private string ResolvePath(string relativePath) => _boundary.Resolve(relativePath);
}
