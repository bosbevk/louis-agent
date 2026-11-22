namespace LouisAgent.Core.Delegation.Messages.Holes;

/// <summary>A minion an address named that its identity doesn't have, and the ones it does.</summary>
public sealed record AddressUnknownMinionHoles(string Identity, string Role, string Minions);
