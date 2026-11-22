namespace LouisAgent.Core.Agent.Messages.Holes.Catalog;

/// <summary>A tool two toolsets both hold, and the two, for the start-up error that refuses the second.</summary>
public sealed record CatalogToolNameTwiceHoles(string Tool, string First, string Second);
