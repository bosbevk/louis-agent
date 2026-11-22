namespace LouisAgent.UsageProbe.Messages.Holes.Lifetimes;

/// <summary>What the hour-long cache would save on the prefix, and its share.</summary>
public sealed record CacheGapsRecommendOneHourHoles(string Saving, string Percent);
