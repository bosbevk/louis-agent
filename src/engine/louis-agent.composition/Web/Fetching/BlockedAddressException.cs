namespace LouisAgent.Composition.Web.Fetching;

/// <summary>A request to a private, local or otherwise non-public address, refused by the engine's guard.</summary>
public sealed class BlockedAddressException(string message) : Exception(message);
