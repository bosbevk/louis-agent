namespace LouisAgent.Core.Tools.Workspace;

using LouisAgent.Core.Guardrails.Workspace;
using System.ComponentModel;
using System.Text;
using LouisAgent.Core.Messages;

/// <summary>A file's content in and out (the files toolset, S: one of its five classes): read, write, append.</summary>
public sealed class WorkspaceFileTools
{
    private readonly WorkspaceBoundary _boundary;
    private readonly string _root;

    public WorkspaceFileTools(string root)
    {
        _boundary = new WorkspaceBoundary(root);
        _root = _boundary.Root;
    }

    [Description("Read the full UTF-8 text content of one file in the workspace. Reading is blocked for secret files (.env*, *.pem, *.key, *.pfx, credentials.json, private SSH keys, etc.) and for any file over 1 MB — both return an explanatory message instead of content rather than an error. Use SearchWorkspace instead when you only need to find where a term appears without reading an entire file.")]
    public string ReadWorkspaceFile(
        [Description("Workspace-relative file path.")] string relativePath)
    {
        string path = ResolvePath(relativePath);
        if (SensitiveFiles.IsSensitive(path))
        {
            return MessageText.Format(ToolMessage.SensitiveRead);
        }

        if (!File.Exists(path))
        {
            return MessageText.Format(ToolMessage.FileNotFound, new { Path = relativePath });
        }

        if (new FileInfo(path).Length > 1_000_000)
        {
            return MessageText.Format(ToolMessage.FileTooLarge);
        }

        return File.ReadAllText(path, Encoding.UTF8);
    }

    [Description("Create a file or fully overwrite an existing one with the given UTF-8 content — this replaces the whole file, it does not merge or patch. Writing is blocked for secret files (.env*, *.pem, *.key, etc.), and parent directories are created automatically if missing. For a file longer than ~300 lines, write only the first chunk here and add the rest with AppendToFile in order; calling this again on the same path discards whatever was written before. Only use this to make changes requested by the user; never write outside the workspace.")]
    public string WriteWorkspaceFile(
        [Description("Workspace-relative file path to create or update.")] string relativePath,
        [Description("UTF-8 file contents: the whole file, or the first chunk of a large one.")] string content)
    {
        string path = ResolvePath(relativePath);
        if (SensitiveFiles.IsSensitive(path))
        {
            return MessageText.Format(ToolMessage.SensitiveWrite);
        }

        string? directory = Path.GetDirectoryName(path);
        if (directory is null)
        {
            return MessageText.Format(ToolMessage.InvalidFilePath);
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return MessageText.Format(ToolMessage.FileWritten, new { Path = Path.GetRelativePath(_root, path), content.Length });
    }

    [Description("Append text to the end of an existing file, or create it if it doesn't exist yet, without touching what's already there. This is the companion to WriteWorkspaceFile for files too long to send in one call: write the first ~300-line chunk with WriteWorkspaceFile, then add further chunks in order with this tool. Writing is blocked for secret files (.env*, *.pem, *.key, etc.), the same as the other write tools.")]
    public string AppendToFile(
        [Description("Workspace-relative file path.")] string relativePath,
        [Description("Text content to append.")] string content)
    {
        string path = ResolvePath(relativePath);
        if (SensitiveFiles.IsSensitive(path))
        {
            return MessageText.Format(ToolMessage.SensitiveAppend);
        }

        string? directory = Path.GetDirectoryName(path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return MessageText.Format(ToolMessage.FileAppended, new { Path = Path.GetRelativePath(_root, path), content.Length });
    }

    private string ResolvePath(string relativePath) => _boundary.Resolve(relativePath);
}
