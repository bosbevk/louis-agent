using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Site.Reading;

/// <summary>
/// <c>GET /v1/site/page?address=</c>: a page of the site as the guide's widget would send it with a message. Open,
/// as the site's search is, since it answers only the site's public words. An eval's run asks it, so its agent gets
/// the page production's agent gets.
/// </summary>
/// <remarks>
/// Remote Facade (Fowler): the run asks one coarse call over the wire for the whole page, picked over a seam handed
/// to the run and over the API filling the page in itself.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
internal sealed class SitePageRoute(ReadPages pages)
{
    /// <summary>The route's address under the API's version.</summary>
    public const string Address = "/site/page";

    /// <summary>Maps the route on the API's version group.</summary>
    public static void Map(IEndpointRouteBuilder v1, ReadPages pages)
    {
        var route = new SitePageRoute(pages);
        v1.MapGet(Address, route.FindAsync).WithAccess(RouteAccess.Open);
    }

    /// <summary>The page that holds the address, or not found when there is none, or no address.</summary>
    private async Task<Results<JsonHttpResult<PageRequest>, NotFound>> FindAsync(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return TypedResults.NotFound();
        if (await pages.FindAsync(address) is not { } page) return TypedResults.NotFound();
        return TypedResults.Json(page);
    }
}
