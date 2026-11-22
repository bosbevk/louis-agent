namespace LouisAgent.Core.Delegation.Messages.Holes.Lookups;

/// <summary>A tool the asker doesn't hold, and the tools it does.</summary>
public sealed record LookupToolNotMineHoles(string Tool, string Tools);
