namespace LouisAgent.Core.Skills.Messages.Holes.Folders;

/// <summary>The skill whose header won't read, and what the header reader said.</summary>
public sealed record SkillHeaderUnreadHoles(string Skill, string Reason);
