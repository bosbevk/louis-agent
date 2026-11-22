namespace LouisAgent.Composition.Web.Messages.Holes.Search;

/// <summary>The search provider that failed, and why.</summary>
public sealed record SearchFailedHoles(string Provider, string Reason);
