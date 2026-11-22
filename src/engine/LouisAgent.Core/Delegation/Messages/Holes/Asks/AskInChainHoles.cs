namespace LouisAgent.Core.Delegation.Messages.Holes.Asks;

/// <summary>The address already higher up the chain of asks, and that chain.</summary>
public sealed record AskInChainHoles(string Address, string Chain);
