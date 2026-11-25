namespace LouisAgent.Core.Agent.Messages.Holes.Skills;

/// <summary>The skill that failed to run, and why.</summary>
public sealed record SkillFailedHoles(string Name, string Reason);
