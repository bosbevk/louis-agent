namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Text;

public sealed class WorkspaceTools
{
    internal static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".idea", ".vs", "bin", "obj", "node_modules", "packages"
    };

    private static readonly HashSet<string> SensitiveFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "credentials.json", "secrets.json", "appsettings.development.json",
        "id_rsa", "id_ed25519"
    };

    private static readonly HashSet<string> SearchableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".sln", ".props", ".targets", ".json", ".md", ".yml", ".yaml",
        ".ts", ".tsx", ".js", ".jsx", ".py", ".go", ".rs", ".java", ".xml", ".config", ".txt"
    };

    private readonly string _root;

    public WorkspaceTools(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Workspace root cannot be empty.", nameof(root));
        }

        _root = Path.GetFullPath(root);
    }

    [Description("List files and directories in the user's project workspace. Paths must be relative to the workspace root.")]
    public string ListWorkspaceFiles(
        [Description("Relative directory path to list; use '.' for the project root.")] string relativePath = ".")
    {
        string directory = ResolvePath(relativePath);
        if (!Directory.Exists(directory))
        {
            return $"Directory not found: {relativePath}";
        }

        var entries = new List<string>();
        foreach (string path in Directory.EnumerateFileSystemEntries(directory)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            if (IsExcluded(path))
            {
                continue;
            }

            entries.Add($"{(Directory.Exists(path) ? "[dir] " : "[file] ")}{Path.GetRelativePath(_root, path)}");
            if (entries.Count == 200)
            {
                entries.Add("[truncated at 200 entries]");
                break;
            }
        }

        return entries.Count == 0 ? "Directory is empty." : string.Join(Environment.NewLine, entries);
    }

    [Description("Read a UTF-8 text file from the project workspace. Sensitive files such as .env and private keys are blocked.")]
    public string ReadWorkspaceFile(
        [Description("Workspace-relative file path.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (IsSensitive(path))
        {
            return "Reading this sensitive file is not allowed.";
        }

        if (!File.Exists(path))
        {
            return $"File not found: {relativePath}";
        }

        if (new FileInfo(path).Length > 1_000_000)
        {
            return "File is larger than the 1 MB reading limit.";
        }

        return File.ReadAllText(path, Encoding.UTF8);
    }

    [Description("Search source and documentation files in the project workspace for a case-insensitive text match.")]
    public string SearchWorkspace(
        [Description("Text to find.")] string query,
        [Description("Maximum number of matching lines to return (1-100).")] int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Search query cannot be empty.";
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
                if (IsExcluded(path))
                {
                    continue;
                }

                if (Directory.Exists(path))
                {
                    pending.Push(path);
                    continue;
                }

                if (!SearchableExtensions.Contains(Path.GetExtension(path)) || IsSensitive(path) ||
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

        return matches.Count == 0 ? "No matches found." : string.Join(Environment.NewLine, matches);
    }

    [Description("Create or replace a UTF-8 text file in the project workspace. Only use this to make changes requested by the user; never write outside the workspace. For a large file (over ~300 lines) pass only the first chunk here, then add the rest with AppendToFile.")]
    public string WriteWorkspaceFile(
        [Description("Workspace-relative file path to create or update.")] string relativePath,
        [Description("UTF-8 file contents: the whole file, or the first chunk of a large one.")] string content)
    {
        string path = ResolvePath(relativePath);
        if (IsSensitive(path))
        {
            return "Writing this sensitive file is not allowed.";
        }

        string? directory = Path.GetDirectoryName(path);
        if (directory is null)
        {
            return "Invalid file path.";
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return $"Wrote {Path.GetRelativePath(_root, path)} ({content.Length} characters).";
    }

    internal string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Workspace paths must be relative.", nameof(relativePath));
        }

        string fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        string relativeToRoot = Path.GetRelativePath(_root, fullPath);
        if (relativeToRoot == ".." ||
            relativeToRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativeToRoot))
        {
            throw new ArgumentException("Path must stay inside the workspace.", nameof(relativePath));
        }

        string? current = fullPath;
        while (current is not null && !current.Equals(_root, StringComparison.OrdinalIgnoreCase))
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

    private bool IsExcluded(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
        (Directory.Exists(path) && ExcludedDirectories.Contains(Path.GetFileName(path)));

    internal static bool IsSensitive(string path)
    {
        string name = Path.GetFileName(path);
        return name.StartsWith(".env", StringComparison.OrdinalIgnoreCase) ||
               SensitiveFileNames.Contains(name) ||
               name.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".key", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase);
    }

    [Description("Create a new directory in the workspace.")]
    public string CreateDirectory(
        [Description("Workspace-relative directory path to create.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (Directory.Exists(path))
        {
            return $"Directory already exists: {relativePath}";
        }

        Directory.CreateDirectory(path);
        return $"Created directory: {Path.GetRelativePath(_root, path)}";
    }

    [Description("Delete a file from the workspace.")]
    public string DeleteFile(
        [Description("Workspace-relative file path to delete.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (IsSensitive(path))
        {
            return "Deleting this sensitive file is not allowed.";
        }

        if (!File.Exists(path))
        {
            return $"File not found: {relativePath}";
        }

        File.Delete(path);
        return $"Deleted file: {relativePath}";
    }

    [Description("Delete a directory and all its contents from the workspace.")]
    public string DeleteDirectory(
        [Description("Workspace-relative directory path to delete.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (!Directory.Exists(path))
        {
            return $"Directory not found: {relativePath}";
        }

        Directory.Delete(path, recursive: true);
        return $"Deleted directory: {relativePath}";
    }

    [Description("Rename a file or directory in the workspace.")]
    public string RenameFileOrDirectory(
        [Description("Workspace-relative path of the file/directory to rename.")] string currentPath,
        [Description("New name (can be relative path with directories, e.g., 'newdir/newname.txt').")] string newPath)
    {
        string oldPath = ResolvePath(currentPath);
        string renamedPath = ResolvePath(newPath);

        if (IsSensitive(oldPath))
        {
            return "Renaming this sensitive file is not allowed.";
        }

        if (!File.Exists(oldPath) && !Directory.Exists(oldPath))
        {
            return $"Path not found: {currentPath}";
        }

        if (File.Exists(renamedPath) || Directory.Exists(renamedPath))
        {
            return $"Destination already exists: {newPath}";
        }

        string? parentDir = Path.GetDirectoryName(renamedPath);
        if (parentDir is not null)
        {
            Directory.CreateDirectory(parentDir);
        }

        if (Directory.Exists(oldPath))
        {
            Directory.Move(oldPath, renamedPath);
        }
        else
        {
            File.Move(oldPath, renamedPath);
        }

        return $"Renamed {(Directory.Exists(renamedPath) ? "directory" : "file")} from {currentPath} to {newPath}";
    }

    [Description("Copy a file to a new location in the workspace.")]
    public string CopyFile(
        [Description("Workspace-relative path of the file to copy.")] string sourcePath,
        [Description("Workspace-relative path for the copy (can include subdirectories).")] string destinationPath)
    {
        string source = ResolvePath(sourcePath);
        string destination = ResolvePath(destinationPath);

        if (!File.Exists(source))
        {
            return $"Source file not found: {sourcePath}";
        }

        if (File.Exists(destination))
        {
            return $"Destination already exists: {destinationPath}";
        }

        string? parentDir = Path.GetDirectoryName(destination);
        if (parentDir is not null)
        {
            Directory.CreateDirectory(parentDir);
        }

        File.Copy(source, destination);
        return $"Copied {sourcePath} to {destinationPath}";
    }

    [Description("Append text to an existing file (or create it if it doesn't exist). Use it to write a large file in chunks of at most ~300 lines after WriteWorkspaceFile wrote the first one.")]
    public string AppendToFile(
        [Description("Workspace-relative file path.")] string relativePath,
        [Description("Text content to append.")] string content)
    {
        string path = ResolvePath(relativePath);
        if (IsSensitive(path))
        {
            return "Writing to this sensitive file is not allowed.";
        }

        string? directory = Path.GetDirectoryName(path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return $"Appended to {Path.GetRelativePath(_root, path)} ({content.Length} characters).";
    }

    [Description("Get detailed information about a file (size, created/modified dates, attributes).")]
    public string GetFileInfo(
        [Description("Workspace-relative file path.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (!File.Exists(path))
        {
            return $"File not found: {relativePath}";
        }

        var info = new FileInfo(path);
        return $"Path: {Path.GetRelativePath(_root, path)}\n" +
               $"Size: {info.Length} bytes ({FormatFileSize(info.Length)})\n" +
               $"Created: {info.CreationTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n" +
               $"Attributes: {info.Attributes}";
    }

    [Description("Find files matching a pattern (wildcards: * for any chars, ? for single char).")]
    public string FindFiles(
        [Description("File name pattern (e.g., '*.cs', 'test*.txt'). Use '*' for all files.")] string pattern,
        [Description("Workspace-relative directory to search in; use '.' for entire workspace.")] string searchPath = ".")
    {
        string directory = ResolvePath(searchPath);
        if (!Directory.Exists(directory))
        {
            return $"Directory not found: {searchPath}";
        }

        try
        {
            var files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories)
                .Where(p => !IsExcluded(p))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Take(500)
                .ToList();

            if (files.Count == 0)
            {
                return $"No files matching pattern '{pattern}' found.";
            }

            var result = new StringBuilder();
            result.AppendLine($"Found {files.Count} file(s) matching '{pattern}':\n");
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                result.AppendLine($"{Path.GetRelativePath(_root, file)} ({FormatFileSize(info.Length)})");
            }

            if (files.Count == 500)
            {
                result.AppendLine("\n[results truncated at 500 files]");
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error searching for files: {ex.Message}";
        }
    }

    [Description("Get a tree view of directory structure up to a specified depth.")]
    public string ListDirectoryTree(
        [Description("Workspace-relative path; use '.' for the project root.")] string relativePath = ".",
        [Description("Maximum depth to display (1-5; default 3).")] int maxDepth = 3)
    {
        maxDepth = Math.Clamp(maxDepth, 1, 5);
        string path = ResolvePath(relativePath);
        if (!Directory.Exists(path))
        {
            return $"Directory not found: {relativePath}";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Directory tree for {Path.GetRelativePath(_root, path)} (depth: {maxDepth}):\n");
        BuildTreeRecursive(path, "", 0, maxDepth, sb);
        return sb.ToString();
    }

    [Description("Check if a file or directory exists in the workspace.")]
    public string PathExists(
        [Description("Workspace-relative path to check.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (File.Exists(path))
        {
            return $"File exists: {relativePath}";
        }

        if (Directory.Exists(path))
        {
            return $"Directory exists: {relativePath}";
        }

        return $"Path does not exist: {relativePath}";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private void BuildTreeRecursive(string directory, string prefix, int depth, int maxDepth, StringBuilder sb)
    {
        if (depth >= maxDepth)
        {
            return;
        }

        try
        {
            var entries = Directory.EnumerateFileSystemEntries(directory)
                .Where(p => !IsExcluded(p))
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
                    BuildTreeRecursive(entry, newPrefix, depth + 1, maxDepth, sb);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            sb.AppendLine($"{prefix}[Access Denied]");
        }
    }
}
