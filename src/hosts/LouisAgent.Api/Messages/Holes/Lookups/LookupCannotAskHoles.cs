namespace LouisAgent.Api.Messages.Holes.Lookups;

/// <summary>The toolset a lookup agent may never load: the one that asks.</summary>
public sealed record LookupCannotAskHoles(string Toolset);
