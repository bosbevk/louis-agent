namespace LouisAgent.Core.Agent.Messages.Holes.Skills;

/// <summary>The skill asked for that isn't loaded, and the ones that are.</summary>
public sealed record SkillNotFoundHoles(string Name, string Available);
