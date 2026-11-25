namespace LouisAgent.Core.Agent.Messages.Holes.Calls;

/// <summary>One argument a call left out, and what it is for.</summary>
public sealed record ArgumentMissingHoles(string Name, string Meaning);
