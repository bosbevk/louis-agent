namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>
/// What the open door learns of a chat it hasn't admitted (F43.1): whether this instance has it, in memory or in its
/// store, and the identity a visitor's chat was admitted as, so it can be let in again after a restart.
/// </summary>
/// <param name="Held">Whether the chat is in memory or in the store.</param>
/// <param name="AdmittedAs">The identity the door opened it as; none for a chat opened with the key.</param>
public sealed record SessionHolding(bool Held, string? AdmittedAs);
