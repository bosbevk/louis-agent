namespace LouisAgent.Composition.Web.Messages.Holes.Search;

/// <summary>The search the results are for, and the provider that ran it.</summary>
public sealed record SearchResultsHoles(string Query, string Provider);
