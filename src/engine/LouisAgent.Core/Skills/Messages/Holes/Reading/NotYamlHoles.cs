namespace LouisAgent.Core.Skills.Messages.Holes.Reading;

/// <summary>The prompt whose header the YAML reader refused, and why.</summary>
public sealed record NotYamlHoles(string Path, string Reason);
