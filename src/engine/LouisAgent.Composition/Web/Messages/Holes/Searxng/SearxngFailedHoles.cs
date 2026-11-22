namespace LouisAgent.Composition.Web.Messages.Holes.Searxng;

/// <summary>SearXNG's status and what it said.</summary>
public sealed record SearxngFailedHoles(int Status, string Detail);
