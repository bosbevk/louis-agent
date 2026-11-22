namespace LouisAgent.UsageProbe.Messages.Holes.Culture;

/// <summary>The culture, its region, and that region's currency with its symbol.</summary>
public sealed record CultureFoundHoles(string Name, string Region, string Code, string Symbol);
