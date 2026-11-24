using System.ComponentModel;
using System.Text;
using LouisAgent.Composition.Guardrails.Workspace;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools.Listing;
using LouisAgent.Packs.Files.Messages;
using LouisAgent.Packs.Files.Messages.Holes.Directories;
using LouisAgent.Packs.Files.Messages.Holes.Search;

namespace LouisAgent.Packs.Files;

/// <summary>
/// Finding across the tree, one of the files toolset's classes: text in files, and files by name. A text hit says where
/// the text is, and a giant line shows only the part around the match (<see cref="SearchHit"/>).
/// </summary>
public sealed class WorkspaceSearchTools
{
    /// <summary>The most matching lines a text search returns, whatever the model asks for.</summary>
    private const int MostMatches = 100;

    /// <summary>The most files a name search lists.</summary>
    private const int MostFiles = 500;

    /// <summary>The largest file a text search reads, in bytes.</summary>
    private const long LargestSearched = 1_000_000;

    /// <summary>Keeps every path inside the workspace.</summary>
    private readonly WorkspaceBoundary _boundary;

    /// <summary>The workspace's root, which hits are named from.</summary>
    private readonly string _root;

    /// <summary>The tools over the workspace at <paramref name="root"/>.</summary>
    public WorkspaceSearchTools(string root)
    {
        _boundary = new WorkspaceBoundary(root);
        _root = _boundary.Root;
    }

    /// <summary>
    /// Every line under a folder of the workspace holding the text, case ignored, up to the most asked for (FX243: a
    /// folder, so an agent searches the repository it was given and not the whole tree).
    /// </summary>
    [Description(
        """
        Recursively search source and documentation files under the workspace root for a case-insensitive literal
        text match, returning each hit as path:line: text; a line longer than 200 characters shows the part around
        the match, marked as cut, and ReadWorkspaceFile reads it whole. Only a fixed set of extensions is searched
        (code, config, markdown, etc.), and sensitive files, files over 1 MB, and excluded directories (.git,
        node_modules, bin, obj, etc.) are skipped. Results stop at maxResults (1-100, default 30) even if more matches
        exist, so a "no matches" result does not prove the term is absent from excluded files. Use FindFiles instead
        when you're matching file names rather than file content.
        """)]
    public string SearchWorkspace(
        [Description("Text to find.")] string query,
        [Description("Maximum number of matching lines to return (1-100).")]
        int maxResults = 30,
        [Description("Workspace-relative folder to search in, such as the repository you were given; '.' for all.")]
        string searchPath = ".")
    {
        if (string.IsNullOrWhiteSpace(query)) return MessageText.Format(WorkspaceToolMessage.WorkspaceSearchQueryEmpty);

        maxResults = Math.Clamp(maxResults, 1, MostMatches);
        var matches = new List<string>();
        // From the folder asked for, held inside the workspace; hits keep their paths from the workspace's root.
        var pending = new Stack<string>();
        pending.Push(_boundary.Resolve(searchPath));

        // Depth first through the folders, skipping what's excluded, until enough lines are found.
        while (pending.Count > 0 && matches.Count < maxResults)
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(pending.Pop()))
            {
                if (WorkspaceFilter.IsExcluded(path)) continue;
                if (Directory.Exists(path))
                {
                    pending.Push(path);
                    continue;
                }

                if (!IsSearched(path)) continue;
                AddMatches(path, query, maxResults, matches);
            }
        }

        return matches.Count == 0
            ? MessageText.Format(WorkspaceToolMessage.NoMatches)
            : string.Join(Environment.NewLine, matches);
    }

    /// <summary>Every file under a folder whose name matches the pattern, up to the most listed.</summary>
    [Description(
        """
        Recursively search the workspace for files whose name matches a wildcard pattern (* for any characters, ? for
        a single character), such as '*.cs' or 'test*.txt'; this matches file names only, not file content. Excluded
        directories (.git, node_modules, bin, obj, etc.) are skipped and results are capped at 500 files. Use
        SearchWorkspace instead when you're looking for a piece of text inside files rather than files by name.
        """)]
    public string FindFiles(
        [Description("File name pattern (e.g., '*.cs', 'test*.txt'). Use '*' for all files.")]
        string pattern,
        [Description("Workspace-relative directory to search in; use '.' for entire workspace.")]
        string searchPath = ".")
    {
        string directory = _boundary.Resolve(searchPath);
        if (!Directory.Exists(directory))
        {
            var missing = new DirectoryNotFoundHoles(searchPath);
            return MessageText.Format(WorkspaceToolMessage.DirectoryNotFound, missing);
        }

        try
        {
            var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
                .Where(file => !WorkspaceFilter.IsExcluded(file))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .Take(MostFiles)
                .ToList();
            if (files.Count == 0)
            {
                var none = new NoFilesMatchingHoles(pattern);
                return MessageText.Format(WorkspaceToolMessage.NoFilesMatching, none);
            }

            return Listing(files, pattern);
        }
        // The folder can change or refuse while it is read: the model hears why, rather than the turn failing.
        catch (Exception ex)
        {
            var failed = new FileSearchFailedHoles(ex.Message);
            return MessageText.Format(WorkspaceToolMessage.FileSearchFailed, failed);
        }
    }

    /// <summary>Whether a text search reads this file: a searchable kind, not sensitive, and not too large.</summary>
    private static bool IsSearched(string path) =>
        WorkspaceFilter.IsSearchable(path) && !SensitiveFiles.IsSensitive(path) &&
        new FileInfo(path).Length <= LargestSearched;

    /// <summary>Adds the file's matching lines as hits, until there are enough.</summary>
    private void AddMatches(string path, string query, int maxResults, List<string> matches)
    {
        int lineNumber = 0;
        foreach (string line in File.ReadLines(path))
        {
            lineNumber++;
            if (!line.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
            matches.Add(SearchHit.Of(Path.GetRelativePath(_root, path), lineNumber, line, query));
            if (matches.Count == maxResults) return;
        }
    }

    /// <summary>The files found, each with its size, and a note when the list stopped at the most listed.</summary>
    private string Listing(IReadOnlyList<string> files, string pattern)
    {
        var found = new FilesFoundHoles(files.Count, pattern);
        var result = new StringBuilder();
        result.AppendLine(MessageText.Format(WorkspaceToolMessage.FilesFound, found));
        foreach (string file in files)
        {
            string size = FileSizeText.Format(new FileInfo(file).Length);
            result.AppendLine($"{Path.GetRelativePath(_root, file)} ({size})");
        }
        if (files.Count == MostFiles) result.AppendLine(MessageText.Format(WorkspaceToolMessage.FindTruncated));
        return result.ToString();
    }
}
