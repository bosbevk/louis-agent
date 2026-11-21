using LouisAgent.Api.Contracts.Site;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Composition.Sites;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Site.Places;

/// <summary>
/// A site's search over its own places (F164.2): <c>GET /v1/site/places?q=</c> answers the places that hold the
/// visitor's words, the best first, at most eight, from the same index the guide's FindPlace reads.
/// </summary>
/// <remarks>
/// Retrieval with no model: the index is read from the real pages, so a search costs nothing, and only asking the
/// agent spends a model call. The route is open, as the pages it searches are: it answers only what any visitor
/// reads on them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
internal sealed class SitePlaceSearch(ISitePlaces places)
{
    /// <summary>The most places one search answers, as many as a search box shows.</summary>
    private const int Most = 8;

    /// <summary>Maps the search on the site's <c>/v1</c> group, open to every visitor.</summary>
    public static void Map(IEndpointRouteBuilder v1, ISitePlaces places)
    {
        var search = new SitePlaceSearch(places);
        v1.MapGet("/site/places", search.FindAsync).WithAccess(RouteAccess.Open);
    }

    /// <summary>The places that hold the words; none when there are no words, or none long enough.</summary>
    private async Task<JsonHttpResult<PlacesReply>> FindAsync(string? q, CancellationToken cancellationToken)
    {
        var found = string.IsNullOrWhiteSpace(q) ? [] : await places.FindAsync(q, Most, cancellationToken);
        var reply = new PlacesReply([.. found.Select(Found)]);
        return TypedResults.Json(reply);
    }

    /// <summary>A place as the search answers it: its address, heading and first words.</summary>
    private static PlaceFound Found(SitePlace place) => new(place.Address, place.Heading, place.Text);
}
