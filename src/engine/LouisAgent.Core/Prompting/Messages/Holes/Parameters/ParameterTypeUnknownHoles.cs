namespace LouisAgent.Core.Prompting.Messages.Holes.Parameters;

/// <summary>The skill, its parameter with no type we take, and the types we do.</summary>
public sealed record ParameterTypeUnknownHoles(string Skill, string Parameter, string Known);
