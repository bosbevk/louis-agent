namespace LouisAgent.Core.Agent.Messages.Holes.SkillFiles;

/// <summary>A skills file saved to wait for approval: where, its skills, its name, and what reloading said.</summary>
public sealed record SkillFileSavedHoles(string Path, string Skills, string Name, string Reload);
