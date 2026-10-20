namespace LouisAgent.Core.Tools.Workspace;

using LouisAgent.Core.Guardrails.Workspace;
using System.ComponentModel;
using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Workspace.Listing;

/// <summary>Finding across the tree (the files toolset, S: one of its five classes): text in files, files by name.</summary>
public sealed class WorkspaceSearchTools
{
    private readonly WorkspaceBoundary _boundary;
    private readonly string _root;

    public WorkspaceSearchTools(string root)
    {
        _boundary = new WorkspaceBoundary(root);
        _root = _boundary.Root;
    }

    [Description("Recursively search source and documentation files under the workspace root for a case-insensitive literal text match, returning each hit as path:line: text. Only a fixed set of extensions is searched (code, config, markdown, etc.), and sensitive files, files over 1 MB, and excluded directories (.git, node_modules, bin, obj, etc.) are skipped. Results stop at maxResults (1-100, default 30) even if more matches exist, so a \"no matches\" result does not prove the term is absent from excluded files. Use FindFiles instead when you're matching file names rather than file content.")]
    public string SearchWorkspace(
        [Description("Text to find.")] string query,
        [Description("Maximum number of matching lines to return (1-100).")] int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return MessageText.Format(ToolMessage.WorkspaceSearchQueryEmpty);
        }

        maxResults = Math.Clamp(maxResults, 1, 100);
        var matches = new List<string>();
        var pending = new Stack<string>();
        pending.Push(_root);

        while (pending.Count > 0 && matches.Count < maxResults)
        {
            string directory = pending.Pop();
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                if (WorkspaceFilter.IsExcluded(path))
                {
                    continue;
                }

                if (Directory.Exists(path))
                {
                    pending.Push(path);
                    continue;
                }

                if (!WorkspaceFilter.IsSearchable(path) || SensitiveFiles.IsSensitive(path) ||
                    new FileInfo(path).Length > 1_000_000)
                {
                    continue;
                }

                int lineNumber = 0;
                foreach (string line in File.ReadLines(path))
                {
                    lineNumber++;
                    if (line.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add($"{Path.GetRelativePath(_root, path)}:{lineNumber}: {line.Trim()}");
                        if (matches.Count == maxResults)
                        {
                            break;
                        }
                    }
                }
            }
        }

        return matches.Count == 0 ? MessageText.Format(ToolMessage.NoMatches) : string.Join(Environment.NewLine, matches);
    }

    [Description("Recursively search the workspace for files whose name matches a wildcard pattern (* for any characters, ? for a single character), such as '*.cs' or 'test*.txt' — this matches file names only, not file content. Excluded directories (.git, node_modules, bin, obj, etc.) are skipped and results are capped at 500 files. Use SearchWorkspace instead when you're looking for a piece of text inside files rather than files by name.")]
    public string FindFiles(
        [Description("File name pattern (e.g., '*.cs', 'test*.txt'). Use '*' for all files.")] string pattern,
        [Description("Workspace-relative directory to search in; use '.' for entire workspace.")] string searchPath = ".")
    {
        string directory = ResolvePath(searchPath);
        if (!Directory.Exists(directory))
        {
            return MessageText.Format(ToolMessage.DirectoryNotFound, new { Path = searchPath });
        }

        try
        {
            var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
                .Where(p => !WorkspaceFilter.IsExcluded(p))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Take(500)
                .ToList();

            if (files.Count == 0)
            {
                return MessageText.Format(ToolMessage.NoFilesMatching, new { Pattern = pattern });
            }

            var result = new StringBuilder();
            result.AppendLine($"Found {files.Count} file(s) matching '{pattern}':\n");
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                result.AppendLine($"{Path.GetRelativePath(_root, file)} ({FileSizeText.Format(info.Length)})");
            }

            if (files.Count == 500)
            {
                result.AppendLine(MessageText.Format(ToolMessage.FindTruncated));
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return MessageText.Format(ToolMessage.FileSearchFailed, new { Reason = ex.Message });
        }
    }

    private string ResolvePath(string relativePath) => _boundary.Resolve(relativePath);
}
