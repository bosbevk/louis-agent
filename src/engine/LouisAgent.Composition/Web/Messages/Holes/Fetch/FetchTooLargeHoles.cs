namespace LouisAgent.Composition.Web.Messages.Holes.Fetch;

/// <summary>The most a fetched page may be, in megabytes.</summary>
public sealed record FetchTooLargeHoles(long Megabytes);
