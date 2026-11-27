namespace LouisAgent.Core.Prompting.Messages.Holes.Door;

/// <summary>The skill a call didn't fit, each problem, and the parameters it takes.</summary>
public sealed record ArgumentsDoNotFitHoles(string Skill, string Problems, string Parameters);
