
using LouisAgent.Packs.Shops.Places;
using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;
using LouisAgent.Core.Messages;
using LouisAgent.Packs.Shops.Messages;
using LouisAgent.Packs.Shops;
using LouisAgent.Core.Tools.Web.Browsing;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Shops.Providers;
/// <summary>
/// The Checkers store that serves a place (F34 step 1b; Louis: "make sure we set the address in checkersprovider, to make
/// sure we use the correct catalogue"). Checkers answers every search for a default store unless the visitor has chosen
/// an area: its address picker sends the area's coordinates to <c>/api/store/fetch-store-contexts</c>, whose reply sets
/// the cookies (<c>storeContexts</c>, <c>storeids</c>, <c>istio-storeIds</c>) every later search is answered from. This
/// does the same, once per town, in a visit of its own (only the town's centre goes into the script, as numbers), and
/// keeps those cookies, Checkers' own, as the session a search starts with. No place, a place nobody knows, none served
/// there, or a lookup that fails: no session, so the default store answers, as before. The towns are kept in
/// <paramref name="stores"/>, the process's one cache (F34): one lookup per town however many searches ask at once, kept
/// a day in its file. A lookup goes through <paramref name="throttle"/> like a search (review of PR #61), and a refused
/// one is a refusal.
/// </summary>
public sealed class CheckersStore(
    IWebBrowser browser,
    Func<string, CancellationToken, Task<(double Latitude, double Longitude)?>> locate,
    StoreSessionCache? stores = null,
    ShopThrottle? throttle = null)
{
    private static readonly ILogger Log = AgentLogging.For<CheckersStore>();
    private static readonly Uri Site = new("https://www.checkers.co.za/");

    private static readonly HashSet<string> StoreCookies =
        new(["storeContexts", "storeids", "istio-storeIds"], StringComparer.Ordinal);

    // The store Checkers answers a visitor with no address from (seen 2026-10-17 and 18). A place Checkers has no store
    // for gets its fallback set, this store first (Prince Albert, Sutherland, Calvinia, Loxton): no store for the town.
    internal const string DefaultStore = "5ece6935faafe599532665b2";

    // Each town's answer: a town's stores don't move between searches. A failed lookup isn't kept, so the next search
    // tries again.
    private readonly StoreSessionCache _stores = stores ?? new StoreSessionCache();
    private readonly ShopThrottle _throttle = throttle ?? new ShopThrottle();

    public Task<BrowserSession?> ForAsync(string? place, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(place)) return Task.FromResult<BrowserSession?>(null);
        string town = place.Trim();
        // The lookup is shared by every search waiting for it, so a caller's cancellation ends its wait, not the lookup.
        return _stores.GetOrLookUpAsync("checkers", town, () => LookUpAsync(town, CancellationToken.None),
            cancellationToken);
    }

    private async Task<(BrowserSession? Session, bool Keep)> LookUpAsync(string town,
        CancellationToken cancellationToken)
    {
        (double Latitude, double Longitude)? found;
        try
        {
            found = await locate(town, cancellationToken);
        }
        catch (PlaceUnavailableException)
        {
            return
                (null, false); // Nominatim didn't answer: no answer about the town, so it isn't kept (review of PR #61)
        }

        if (found is not { } centre) return (null, true);

        BrowserVisit visit;
        try
        {
            visit = await _throttle.RunAsync(() => browser.VisitAsync(Site, session: new BrowserSession([]),
                    script: Lookup(centre.Latitude, centre.Longitude), cancellationToken: cancellationToken),
                cancellationToken);
        }
        catch (WebBrowserException ex)
        {
            Log.Log(LogLevel.Warning, ShopLogMessage.ShopAreaUnavailable,
                new { Shop = "Checkers", Place = LogContent.Of(town), Reason = LogContent.Of(ex.Message) });
            return (null, false);
        }

        // Cook check 42d837f13659: a reply that isn't Checkers' store answer (a refusal) is a failed lookup, never "no store".
        if (Serves(visit.ScriptResult) is not { } serves)
        {
            _throttle.Refused();
            Log.Log(LogLevel.Warning, ShopLogMessage.ShopStoreAnswerUnreadable,
                new { Shop = "Checkers", Place = LogContent.Of(town) });
            return (null, false);
        }

        BrowserSession? session = serves && visit.Cookies?.Where(c => StoreCookies.Contains(c.Name)).ToList() is
        { Count: > 0 } cookies
            ? new BrowserSession(cookies)
            : null;
        // Louis: "see the logs, it did not set my address": the lookup says what it found, once per town.
        Log.Log(LogLevel.Information,
            session is null ? ShopLogMessage.ShopNoStoreForPlace : ShopLogMessage.ShopStoreForPlace, new
            {
                // The town, its centre and the store ids (an id names its town as surely as the coordinates) are hashes unless LOG_CONTENT=full (F18 step 4f).
                Shop = "Checkers",
                Place = LogContent.Of(town),
                Latitude = LogContent.Of(centre.Latitude.ToString("0.####", CultureInfo.InvariantCulture)),
                Longitude = LogContent.Of(centre.Longitude.ToString("0.####", CultureInfo.InvariantCulture)),
                Stores = LogContent.Of(session?.Cookies.FirstOrDefault(c => c.Name == "storeids")?.Value ?? ""),
            });
        return (session, true);
    }

    /// <summary>The site's own call for an area's stores, as its address picker makes it: only the centre, as numbers.</summary>
    internal static string Lookup(double latitude, double longitude) => string.Create(CultureInfo.InvariantCulture,
        $$"""fetch('/api/store/fetch-store-contexts?update=true', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ address: { coordinates: { latitude: {{latitude:R}}, longitude: {{longitude:R}} } }, update: true, acceptedLimitedExperience: false }) }).then(r => r.text())""");

    /// <summary>
    /// Whether the answer names a store serving the area: at least one, and not Checkers' fallback set. Null when it isn't
    /// Checkers' store answer at all (no <c>storeContexts</c> list: a refusal, or a site that changed).
    /// </summary>
    private static bool? Serves(string? answer)
    {
        if (answer is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(answer);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("storeContexts",
                                                                      out var stores)
                                                                  || stores.ValueKind != JsonValueKind.Array)
                return null;
            return stores.GetArrayLength() > 0
                   && !stores.EnumerateArray().Any(store =>
                       store.TryGetProperty("storeId", out var id) && id.GetString() == DefaultStore);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
