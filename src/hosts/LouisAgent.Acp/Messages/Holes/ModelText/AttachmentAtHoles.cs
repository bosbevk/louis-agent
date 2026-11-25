namespace LouisAgent.Acp.Messages.Holes.ModelText;

/// <summary>A file Rider attached, by name, where the agent sees it, and what it holds.</summary>
public sealed record AttachmentAtHoles(string Name, string Path, string Content);
