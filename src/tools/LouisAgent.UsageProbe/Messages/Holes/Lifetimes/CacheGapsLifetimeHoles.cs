namespace LouisAgent.UsageProbe.Messages.Holes.Lifetimes;

/// <summary>One cache lifetime: the writes and reads it would have made, and what they cost.</summary>
public sealed record CacheGapsLifetimeHoles(string Lifetime, int Writes, int Reads, string Cost);
