namespace LouisAgent.Core.Agent.Messages.Holes.Catalog;

/// <summary>
/// A toolset's tool that takes one of the engine's own names, and its toolset, for the start-up error.
/// </summary>
public sealed record CatalogToolNameIsTheEnginesHoles(string Tool, string Toolset);
