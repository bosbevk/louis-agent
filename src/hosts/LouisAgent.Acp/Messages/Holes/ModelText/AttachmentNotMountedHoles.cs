namespace LouisAgent.Acp.Messages.Holes.ModelText;

/// <summary>A file Rider attached that sits outside every folder mounted into the agent, by name and address.</summary>
public sealed record AttachmentNotMountedHoles(string Name, string Uri);
