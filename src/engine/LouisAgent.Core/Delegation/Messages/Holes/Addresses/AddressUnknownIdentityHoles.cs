namespace LouisAgent.Core.Delegation.Messages.Holes.Addresses;

/// <summary>The identity an address named that this instance doesn't run, and the ones it does.</summary>
public sealed record AddressUnknownIdentityHoles(string Identity, string Identities);
