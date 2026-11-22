namespace LouisAgent.UsageProbe.Messages.Holes.Gaps;

/// <summary>One host and model: how many requests, and the prefix's size in tokens.</summary>
public sealed record CacheGapsGroupHoles(string Host, string Model, int Requests, string Prefix);
