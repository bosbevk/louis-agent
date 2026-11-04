namespace LouisAgent.Web.Workspace;

/// <summary>A file or folder in the agent's workspace; <see cref="Path"/> is workspace-relative with forward slashes.</summary>
public sealed record WorkspaceEntry(string Name, string Path, string Type, long? Size, bool Sensitive)
{
    public bool IsDirectory => Type == "directory";
}
