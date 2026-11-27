namespace LouisAgent.Core.Prompting.Messages.Holes.Drafting;

/// <summary>The skill whose example didn't run cleanly, and what the run said.</summary>
public sealed record ExampleFailedHoles(string Skill, string Answer);
