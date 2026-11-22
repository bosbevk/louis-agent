namespace LouisAgent.Core.Delegation.Messages.Holes.Addresses;

/// <summary>A subagent an address named that its identity doesn't have, and the ones it does.</summary>
public sealed record AddressUnknownRoleHoles(string Identity, string Role, string Roles);
