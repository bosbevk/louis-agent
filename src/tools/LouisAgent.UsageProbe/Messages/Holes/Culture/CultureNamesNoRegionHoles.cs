namespace LouisAgent.UsageProbe.Messages.Holes.Culture;

/// <summary>The culture that names no region, and why it was refused.</summary>
public sealed record CultureNamesNoRegionHoles(string Name, string Reason);
