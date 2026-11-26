namespace LouisAgent.Core.Skills.Messages.Holes.Running;

/// <summary>The skill whose script ran past its limit, the limit, and the end of what it printed.</summary>
public sealed record SkillTimedOutHoles(string Skill, int Seconds, string Output);
