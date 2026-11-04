using System.Text.Json.Serialization;

namespace LouisAgent.Packs.WhereToBuy.Branches.Map;

/// <summary>
/// One result from Nominatim's search: the place's name, where it is, what kind of thing it is (a shop, a town) and
/// its address. Nominatim writes the coordinates as text, so they are read from strings.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record MapPlace(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("lat")] [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    double Latitude,
    [property: JsonPropertyName("lon")] [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    double Longitude,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("address")] MapAddress? Address);
