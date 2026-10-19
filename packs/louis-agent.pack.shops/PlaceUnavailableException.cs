namespace LouisAgent.Packs.Shops;

/// <summary>Nominatim didn't answer (an error status, a timeout, or a reply that isn't its JSON): no answer about the town.</summary>
public sealed class PlaceUnavailableException(string message) : Exception(message);
