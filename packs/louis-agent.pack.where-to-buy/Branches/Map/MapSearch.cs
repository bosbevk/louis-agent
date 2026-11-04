using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;
using LouisAgent.Packs.WhereToBuy.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.WhereToBuy.Branches.Map;

/// <summary>
/// Asks OpenStreetMap's Nominatim where a town is, and which shops of a name lie within a box around it. Only the
/// town and the shop's name are sent, never the user's address; South Africa only. Nominatim's usage policy asks for
/// a User-Agent that names the app and at most one request a second, so requests wait their turn here.
/// </summary>
/// <remarks>
/// A class, since it owns the HTTP client and the pace. It calls the map's API directly rather than through the
/// browser: it is a data service with a usage policy, not a page to read.
/// </remarks>
/// <seealso href="https://operations.osmfoundation.org/policies/nominatim/">Nominatim usage policy</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0038-the-browser-says-what-it-is.md">ADR 0038: say what we are</seealso>
public sealed class MapSearch(HttpClient http, TimeSpan pace)
{
    private static readonly ILogger Log = AgentLogging.For<MapSearch>();
    private const string Base = "https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&countrycodes=za";

    private readonly SemaphoreSlim _turn = new(1, 1);
    private DateTimeOffset _last = DateTimeOffset.MinValue;

    /// <summary>The one the process shares: a named User-Agent, ten seconds a request, one request a second.</summary>
    public static MapSearch Default { get; } = new(CreateClient(), TimeSpan.FromSeconds(1));

    /// <summary>The URL that finds a town, the best match only.</summary>
    internal static Uri TownUrl(string town) => new($"{Base}&limit=1&q={Uri.EscapeDataString(town.Trim())}");

    /// <summary>The URL that finds places of a name inside a box this many degrees around a point.</summary>
    internal static Uri NearUrl(string name, double latitude, double longitude, double degrees) =>
        new(string.Create(CultureInfo.InvariantCulture,
            $"{Base}&limit=20&bounded=1&viewbox={longitude - degrees},{latitude + degrees},{longitude + degrees},{latitude - degrees}&q={Uri.EscapeDataString(name.Trim())}"));

    /// <summary>The town's best match, or none when the map doesn't know it.</summary>
    public async Task<MapPlace?> TownAsync(string town, CancellationToken cancellationToken) =>
        (await SearchAsync(TownUrl(town), town, cancellationToken)).FirstOrDefault();

    /// <summary>The places of this name inside a box around the town.</summary>
    public Task<IReadOnlyList<MapPlace>> NearAsync(string name, MapPlace town, double degrees,
        CancellationToken cancellationToken) =>
        SearchAsync(NearUrl(name, town.Latitude, town.Longitude, degrees), town.Name ?? name, cancellationToken);

    /// <summary>One request, in its turn; a map that doesn't answer is <see cref="MapUnavailableException"/>.</summary>
    private async Task<IReadOnlyList<MapPlace>> SearchAsync(Uri url, string place, CancellationToken cancellationToken)
    {
        await _turn.WaitAsync(cancellationToken);
        try
        {
            // Keep to the policy's pace: wait out what is left of the second since the last request.
            var wait = _last + pace - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait, cancellationToken);
            _last = DateTimeOffset.UtcNow;
            string answer = await http.GetStringAsync(url, cancellationToken);
            return JsonSerializer.Deserialize<List<MapPlace>>(answer) ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException &&
                                   !cancellationToken.IsCancellationRequested)
        {
            // The town the user named is logged as a hash, never as text.
            Log.Log(LogLevel.Warning, WhereToBuyLogMessage.MapUnavailable,
                new { Place = LogContent.Of(place), Reason = LogContent.Of(ex.Message) });
            throw new MapUnavailableException(ex.Message);
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <summary>The client every request goes out on, naming the app as the policy asks.</summary>
    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("louis-agent/1.0 (+https://github.com/bosbevk/louis-agent)");
        return http;
    }
}
