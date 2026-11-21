namespace LouisAgent.Packs.WhereToBuy.Branches.Map;

/// <summary>
/// The map didn't answer (an error status such as 429 or 503, a timeout, a reply that isn't its JSON). That is no
/// answer about the town, so it is never taken as "no town" or "no branch".
/// </summary>
public sealed class MapUnavailableException(string reason) : Exception(reason);
