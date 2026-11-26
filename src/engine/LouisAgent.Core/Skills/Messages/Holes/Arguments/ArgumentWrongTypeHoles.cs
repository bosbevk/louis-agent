namespace LouisAgent.Core.Skills.Messages.Holes.Arguments;

/// <summary>The argument whose value isn't its parameter's type, the type it must be, and the kind it was.</summary>
public sealed record ArgumentWrongTypeHoles(string Parameter, string Expected, string Given);
