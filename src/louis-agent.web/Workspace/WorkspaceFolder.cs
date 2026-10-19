namespace LouisAgent.Web.Workspace;

public sealed record WorkspaceFolder(string Path, List<WorkspaceEntry> Entries);
