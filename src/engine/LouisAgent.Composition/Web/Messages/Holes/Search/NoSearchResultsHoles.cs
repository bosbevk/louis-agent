namespace LouisAgent.Composition.Web.Messages.Holes.Search;

/// <summary>The search that found nothing, and the provider that ran it.</summary>
public sealed record NoSearchResultsHoles(string Query, string Provider);
