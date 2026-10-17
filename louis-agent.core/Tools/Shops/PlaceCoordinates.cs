namespace LouisAgent.Core.Tools.Shops;

using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;

/// <summary>
/// A town's centre (F34 step 1b), for a shop that serves by area: OpenStreetMap's Nominatim, as the cook already asks it
/// for the town's shops. Only the town the user shared is sent, never their address; South Africa only; one result.
/// Nominatim asks for a User-Agent that names the app, and few requests (the caller keeps each town's answer).
/// </summary>
public static class PlaceCoordinates
{
    private static readonly ILogger Log = AgentLogging.For<ShopSearch>();
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("louis-agent/1.0 (+https://github.com/bosbevk/louis-agent)");
        return http;
    }

    internal static Uri Url(string place) =>
        new("https://nominatim.openstreetmap.org/search?q=" + Uri.EscapeDataString(place.Trim()) + "&format=json&limit=1&countrycodes=za");

    /// <summary>The first result's latitude and longitude; none when the answer has no result with both.</summary>
    internal static (double Latitude, double Longitude)? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
            var first = doc.RootElement[0];
            return first.TryGetProperty("lat", out var lat) && first.TryGetProperty("lon", out var lon)
                && double.TryParse(lat.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)
                && double.TryParse(lon.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude)
                ? (latitude, longitude)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The town's centre, or none when Nominatim doesn't know it or doesn't answer (said in a log line).</summary>
    public static async Task<(double Latitude, double Longitude)?> OfAsync(string place, CancellationToken cancellationToken)
    {
        try
        {
            return Parse(await Http.GetStringAsync(Url(place), cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, LogMessage.ShopAreaUnavailable, new { Shop = "OpenStreetMap", Place = place, Reason = ex.Message });
            return null;
        }
    }
}
