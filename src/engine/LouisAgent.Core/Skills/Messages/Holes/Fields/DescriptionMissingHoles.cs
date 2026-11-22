namespace LouisAgent.Core.Skills.Messages.Holes.Fields;

/// <summary>The prompt with no description, or one too long, and the longest one may be.</summary>
public sealed record DescriptionMissingHoles(string Path, int Limit);
