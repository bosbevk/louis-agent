namespace LouisAgent.Core.Delegation.Messages.Holes.Addresses;

/// <summary>A lookup agent an address named that its identity doesn't have, and the ones it does.</summary>
public sealed record AddressUnknownLookupHoles(string Identity, string Role, string Lookups);
