namespace LouisAgent.Core.Agent.Messages.Holes.Catalog;

/// <summary>
/// The name two things of one kind share, and that kind, for the start-up error that refuses the second.
/// </summary>
public sealed record CatalogNameTwiceHoles(string Name, string Kind);
