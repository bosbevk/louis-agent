namespace LouisAgent.Packs.Files.Messages.Holes.Files;

/// <summary>The file written, and its length in characters.</summary>
public sealed record FileWrittenHoles(string Path, int Length);
