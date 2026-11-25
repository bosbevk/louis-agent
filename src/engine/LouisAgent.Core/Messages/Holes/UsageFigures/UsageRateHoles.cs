namespace LouisAgent.Core.Messages.Holes.UsageFigures;

/// <summary>The exchange rate costs are shown at, the day it's from, and what was charged before converting.</summary>
public sealed record UsageRateHoles(string Rate, string? AsOf, string Charged);
