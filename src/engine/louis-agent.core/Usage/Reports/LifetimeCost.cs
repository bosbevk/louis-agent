namespace LouisAgent.Core.Usage.Reports;

/// <summary>What one lifetime would have cost the prefix: its writes, its reads and their price (null: no price).</summary>
public sealed record LifetimeCost(int Writes, int Reads, decimal? Cost);
