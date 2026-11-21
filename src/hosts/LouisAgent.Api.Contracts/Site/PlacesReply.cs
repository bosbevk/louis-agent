namespace LouisAgent.Api.Contracts.Site;

/// <summary>
/// What a site's search found among its own places (F164.2), the best first: <c>GET /v1/site/places?q=</c>.
/// </summary>
/// <param name="Places">The places found, at most eight; none for words too short to search.</param>
public sealed record PlacesReply(IReadOnlyList<PlaceFound> Places);
