namespace LouisAgent.Packs.Files.Web.Reading;

/// <summary>One folder of the agent's workspace, with what's in it.</summary>
public sealed record WorkspaceFolder(string Path, List<WorkspaceEntry> Entries);
