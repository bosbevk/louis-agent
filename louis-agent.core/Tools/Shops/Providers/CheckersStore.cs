namespace LouisAgent.Core.Tools.Shops.Providers;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using Microsoft.Extensions.Logging;

/// <summary>
/// The Checkers store that serves a place (F34 step 1b; Louis: "make sure we set the address in checkersprovider, to make
/// sure we use the correct catalogue"). Checkers answers every search for a default store unless the visitor has chosen
/// an area: its address picker sends the area's coordinates to <c>/api/store/fetch-store-contexts</c>, whose reply sets
/// the cookies (<c>storeContexts</c>, <c>storeids</c>, <c>istio-storeIds</c>) every later search is answered from. This
/// does the same, once per town, in a visit of its own (only the town's centre goes into the script, as numbers), and
/// keeps those cookies, Checkers' own, as the session a search starts with. No place, a place nobody knows, none served
/// there, or a lookup that fails: no session, so the default store answers, as before.
/// </summary>
public sealed class CheckersStore(IWebBrowser browser, Func<string, CancellationToken, Task<(double Latitude, double Longitude)?>> locate)
{
    private static readonly ILogger Log = AgentLogging.For<CheckersStore>();
    private static readonly Uri Site = new("https://www.checkers.co.za/");
    private static readonly HashSet<string> StoreCookies = new(["storeContexts", "storeids", "istio-storeIds"], StringComparer.Ordinal);

    // Each town's answer, kept for the instance's life: a town's stores don't move between searches. A failed lookup
    // isn't kept, so the next search tries again.
    private readonly ConcurrentDictionary<string, BrowserSession?> _byTown = new(StringComparer.OrdinalIgnoreCase);

    public async Task<BrowserSession?> ForAsync(string? place, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(place)) return null;
        string town = place.Trim();
        if (_byTown.TryGetValue(town, out var known)) return known;

        if (await locate(town, cancellationToken) is not { } centre) return _byTown.GetOrAdd(town, (BrowserSession?)null);

        BrowserVisit visit;
        try
        {
            visit = await browser.VisitAsync(Site, session: new BrowserSession([]), script: Lookup(centre.Latitude, centre.Longitude),
                cancellationToken: cancellationToken);
        }
        catch (WebBrowserException ex)
        {
            Log.Log(LogLevel.Warning, LogMessage.ShopAreaUnavailable, new { Shop = "Checkers", Place = town, Reason = ex.Message });
            return null;
        }

        BrowserSession? session = Serves(visit.ScriptResult) && visit.Cookies?.Where(c => StoreCookies.Contains(c.Name)).ToList() is { Count: > 0 } cookies
            ? new BrowserSession(cookies)
            : null;
        // Louis: "see the logs, it did not set my address": the lookup says what it found, once per town.
        Log.Log(LogLevel.Information, session is null ? LogMessage.ShopNoStoreForPlace : LogMessage.ShopStoreForPlace, new
        {
            Shop = "Checkers", Place = town, Latitude = centre.Latitude.ToString("0.####", CultureInfo.InvariantCulture),
            Longitude = centre.Longitude.ToString("0.####", CultureInfo.InvariantCulture),
            Stores = session?.Cookies.FirstOrDefault(c => c.Name == "storeids")?.Value ?? "",
        });
        return _byTown.GetOrAdd(town, session);
    }

    /// <summary>The site's own call for an area's stores, as its address picker makes it: only the centre, as numbers.</summary>
    internal static string Lookup(double latitude, double longitude) => string.Create(CultureInfo.InvariantCulture,
        $$"""fetch('/api/store/fetch-store-contexts?update=true', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ address: { coordinates: { latitude: {{latitude:R}}, longitude: {{longitude:R}} } }, update: true, acceptedLimitedExperience: false }) }).then(r => r.text())""");

    /// <summary>Whether the answer names at least one store serving the area.</summary>
    private static bool Serves(string? answer)
    {
        if (answer is null) return false;
        try
        {
            using var doc = JsonDocument.Parse(answer);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("storeContexts", out var stores)
                && stores.ValueKind == JsonValueKind.Array && stores.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
