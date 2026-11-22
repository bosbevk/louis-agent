namespace LouisAgent.UsageProbe.Messages.Holes.Gaps;

/// <summary>The ledger read, and the date of the prices the costs use.</summary>
public sealed record CacheGapsHeaderHoles(string Path, string AsOf);
