namespace LouisAgent.CodeReview.Workspace;

public sealed record WorkspaceFolder(string Path, List<WorkspaceEntry> Entries);
