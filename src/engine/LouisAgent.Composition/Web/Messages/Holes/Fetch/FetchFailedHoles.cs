namespace LouisAgent.Composition.Web.Messages.Holes.Fetch;

/// <summary>The page that couldn't be fetched, and why.</summary>
public sealed record FetchFailedHoles(string Url, string Reason);
