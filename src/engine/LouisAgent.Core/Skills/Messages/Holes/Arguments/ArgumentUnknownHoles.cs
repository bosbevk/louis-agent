namespace LouisAgent.Core.Skills.Messages.Holes.Arguments;

/// <summary>The argument a call gave that the skill doesn't take, and the ones it does.</summary>
public sealed record ArgumentUnknownHoles(string Parameter, string Known);
