namespace LouisAgent.Core.Prompting.Messages.Holes.Library;

/// <summary>The skill's folder that won't read, and why.</summary>
public sealed record SkillLeftOutHoles(string Folder, string Reason);
