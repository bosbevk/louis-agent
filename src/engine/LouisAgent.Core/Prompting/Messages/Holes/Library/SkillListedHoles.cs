namespace LouisAgent.Core.Prompting.Messages.Holes.Library;

/// <summary>An approved skill's name, which the model calls, and its line saying what it does and when.</summary>
public sealed record SkillListedHoles(string Name, string Description);
