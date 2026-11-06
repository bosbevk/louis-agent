namespace LouisAgent.Packs.Files.Web.Reading;

/// <summary>One file from the agent's workspace, opened to read.</summary>
public sealed record WorkspaceFile(string Path, string Name, long Size, string Content);
