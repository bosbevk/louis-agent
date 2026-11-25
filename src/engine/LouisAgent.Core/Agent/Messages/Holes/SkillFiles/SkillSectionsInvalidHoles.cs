namespace LouisAgent.Core.Agent.Messages.Holes.SkillFiles;

/// <summary>How many skill sections a file declared, and how many of them read as valid.</summary>
public sealed record SkillSectionsInvalidHoles(int Declared, int Valid);
