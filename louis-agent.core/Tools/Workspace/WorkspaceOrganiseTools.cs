
using LouisAgent.Core.Guardrails.Workspace;
using System.ComponentModel;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Workspace.Messages;

namespace LouisAgent.Core.Tools.Workspace;
/// <summary>The tree's shape (the files toolset, S: one of its five classes): create a directory, delete, rename, copy.</summary>
public sealed class WorkspaceOrganiseTools
{
    private readonly WorkspaceBoundary _boundary;
    private readonly string _root;

    public WorkspaceOrganiseTools(string root)
    {
        _boundary = new WorkspaceBoundary(root);
        _root = _boundary.Root;
    }

    [Description(
        "Create a new directory at the given workspace-relative path, including any missing parent directories. If the directory already exists this is a no-op that reports it already exists rather than an error. Use this before writing files into a path that may not exist yet, though WriteWorkspaceFile and AppendToFile already create missing parent directories on their own.")]
    public string CreateDirectory(
        [Description("Workspace-relative directory path to create.")]
        string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (Directory.Exists(path))
        {
            return MessageText.Format(WorkspaceToolMessage.DirectoryExistsAlready, new { Path = relativePath });
        }

        Directory.CreateDirectory(path);
        return MessageText.Format(WorkspaceToolMessage.DirectoryCreated,
            new { Path = Path.GetRelativePath(_root, path) });
    }

    [Description(
        "Permanently delete a single file from the workspace — there is no undo and the file is not moved to a trash or recycle bin. Deletion is blocked for secret files (.env*, *.pem, *.key, etc.). Use DeleteDirectory instead to remove a whole directory and everything inside it.")]
    public string DeleteFile(
        [Description("Workspace-relative file path to delete.")]
        string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (SensitiveFiles.IsSensitive(path))
        {
            return MessageText.Format(WorkspaceToolMessage.SensitiveDelete);
        }

        if (!File.Exists(path))
        {
            return MessageText.Format(ToolMessage.FileNotFound, new { Path = relativePath });
        }

        File.Delete(path);
        return MessageText.Format(WorkspaceToolMessage.FileDeleted, new { Path = relativePath });
    }

    [Description(
        "Permanently delete a directory and everything inside it, recursively — there is no undo and no confirmation prompt. Deletion is blocked if the directory contains any secret file anywhere inside it (.env*, *.pem, *.key, etc.), the same protection DeleteFile applies to a single file. Use this only when the user has clearly asked to remove an entire folder; prefer DeleteFile for a single file so sibling files aren't lost by accident.")]
    public string DeleteDirectory(
        [Description("Workspace-relative directory path to delete.")]
        string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (!Directory.Exists(path))
        {
            return MessageText.Format(WorkspaceToolMessage.DirectoryNotFound, new { Path = relativePath });
        }

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            if (SensitiveFiles.IsSensitive(file))
            {
                return MessageText.Format(WorkspaceToolMessage.SensitiveDirectoryDelete);
            }
        }

        Directory.Delete(path, recursive: true);
        return MessageText.Format(WorkspaceToolMessage.DirectoryDeleted, new { Path = relativePath });
    }

    [Description(
        "Rename or move a file or directory within the workspace by resolving both the current and new path and performing the move. It fails if the destination path already exists (no overwrite) or if the source is a blocked sensitive file; missing parent directories for the destination are created automatically. Works on both files and directories — the same tool handles either.")]
    public string RenameFileOrDirectory(
        [Description("Workspace-relative path of the file/directory to rename.")]
        string currentPath,
        [Description("New name (can be relative path with directories, e.g., 'newdir/newname.txt').")]
        string newPath)
    {
        string oldPath = ResolvePath(currentPath);
        string renamedPath = ResolvePath(newPath);

        if (SensitiveFiles.IsSensitive(oldPath))
        {
            return MessageText.Format(WorkspaceToolMessage.SensitiveRename);
        }

        if (!File.Exists(oldPath) && !Directory.Exists(oldPath))
        {
            return MessageText.Format(WorkspaceToolMessage.PathNotFound, new { Path = currentPath });
        }

        if (File.Exists(renamedPath) || Directory.Exists(renamedPath))
        {
            return MessageText.Format(WorkspaceToolMessage.DestinationExists, new { Path = newPath });
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

        return MessageText.Format(
            Directory.Exists(renamedPath) ? WorkspaceToolMessage.DirectoryRenamed : WorkspaceToolMessage.FileRenamed,
            new { From = currentPath, To = newPath });
    }

    [Description(
        "Copy a single file to a new workspace-relative location, creating missing parent directories for the destination automatically. It fails if the destination already exists (no overwrite) or if the source file is missing. Copying is blocked if either the source or the destination is a secret file (.env*, *.pem, *.key, etc.), the same protection the other write tools apply. This only copies files, not directories — there is no recursive directory-copy tool.")]
    public string CopyFile(
        [Description("Workspace-relative path of the file to copy.")]
        string sourcePath,
        [Description("Workspace-relative path for the copy (can include subdirectories).")]
        string destinationPath)
    {
        string source = ResolvePath(sourcePath);
        string destination = ResolvePath(destinationPath);

        if (SensitiveFiles.IsSensitive(source) || SensitiveFiles.IsSensitive(destination))
        {
            return MessageText.Format(WorkspaceToolMessage.SensitiveCopy);
        }

        if (!File.Exists(source))
        {
            return MessageText.Format(WorkspaceToolMessage.SourceFileNotFound, new { Path = sourcePath });
        }

        if (File.Exists(destination))
        {
            return MessageText.Format(WorkspaceToolMessage.DestinationExists, new { Path = destinationPath });
        }

        string? parentDir = Path.GetDirectoryName(destination);
        if (parentDir is not null)
        {
            Directory.CreateDirectory(parentDir);
        }

        File.Copy(source, destination);
        return MessageText.Format(WorkspaceToolMessage.FileCopied,
            new { Source = sourcePath, Destination = destinationPath });
    }

    private string ResolvePath(string relativePath) => _boundary.Resolve(relativePath);
}
