namespace LouisAgent.Core.Skills.Messages.Holes.Drafting;

/// <summary>The name a skill was given that can't be a skill's folder.</summary>
public sealed record SkillNameInvalidHoles(string Name, int Longest);
