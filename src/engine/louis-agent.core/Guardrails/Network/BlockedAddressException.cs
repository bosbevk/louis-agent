namespace LouisAgent.Core.Guardrails.Network;

/// <summary>A request to a private, local or otherwise non-public address, refused by <see cref="SsrfGuard"/>.</summary>
internal sealed class BlockedAddressException(string message) : Exception(message);