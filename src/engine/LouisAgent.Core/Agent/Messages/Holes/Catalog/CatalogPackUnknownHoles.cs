namespace LouisAgent.Core.Agent.Messages.Holes.Catalog;

/// <summary>Who named a pack nobody registered, the name, and the packs that are, for the start-up error.</summary>
public sealed record CatalogPackUnknownHoles(string Owner, string Name, string Registered);
