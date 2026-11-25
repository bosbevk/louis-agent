namespace LouisAgent.Core.Messages.Holes.SkillFiles;

/// <summary>The skill file rejected, the file deleted, and what more was done about it, if anything.</summary>
public sealed record SkillFileRejectedHoles(string Name, string Path, string Removed);
