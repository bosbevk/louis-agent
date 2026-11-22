namespace LouisAgent.UsageProbe.Messages.Holes.Culture;

/// <summary>The rate from the prices' currency to the display one, its date and where it came from.</summary>
public sealed record RateFoundHoles(string Base, decimal Rate, string Code, string? AsOf, string? Source);
