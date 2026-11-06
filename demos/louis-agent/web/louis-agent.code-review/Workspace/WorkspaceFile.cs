namespace LouisAgent.CodeReview.Workspace;

/// <summary>One file from the agent's workspace, opened to read.</summary>
public sealed record WorkspaceFile(string Path, string Name, long Size, string Content);
