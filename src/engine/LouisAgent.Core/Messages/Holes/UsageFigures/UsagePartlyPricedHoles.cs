namespace LouisAgent.Core.Messages.Holes.UsageFigures;

/// <summary>The cost of the priced requests, and how many requests have no price.</summary>
public sealed record UsagePartlyPricedHoles(string Amount, int Unpriced);
