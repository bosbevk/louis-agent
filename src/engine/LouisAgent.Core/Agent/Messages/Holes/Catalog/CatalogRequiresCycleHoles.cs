namespace LouisAgent.Core.Agent.Messages.Holes.Catalog;

/// <summary>The ring of packs that require each other, written out, for the start-up error.</summary>
public sealed record CatalogRequiresCycleHoles(string Cycle);
