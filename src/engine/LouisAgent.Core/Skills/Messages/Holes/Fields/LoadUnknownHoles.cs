namespace LouisAgent.Core.Skills.Messages.Holes.Fields;

/// <summary>The skill, how it says it loads that isn't a way, and the ways there are.</summary>
public sealed record LoadUnknownHoles(string Path, string Load, string Supported);
