namespace LouisAgent.CodeReview.Workspace;

/// <summary>A file or folder in the agent's workspace; its path is relative to it, with forward slashes.</summary>
public sealed record WorkspaceEntry(string Name, string Path, string Type, long? Size, bool Sensitive)
{
    public bool IsDirectory => Type == "directory";
}
