namespace LouisAgent.UsageProbe.Messages.Holes.Gaps;

/// <summary>How many gaps between requests fell under five minutes, up to an hour, and over.</summary>
public sealed record CacheGapsGapsHoles(int Under5m, int From5To60m, int Over60m);
