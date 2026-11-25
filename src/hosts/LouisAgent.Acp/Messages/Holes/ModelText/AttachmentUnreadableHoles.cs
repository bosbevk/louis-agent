namespace LouisAgent.Acp.Messages.Holes.ModelText;

/// <summary>A file Rider attached that could not be read, and why.</summary>
public sealed record AttachmentUnreadableHoles(string Name, string Reason);
