using System.Text.Json.Serialization;

namespace LouisAgent.Packs.WhereToBuy.Branches.Map;

/// <summary>
/// A place's address as OpenStreetMap's Nominatim breaks it up: the street, the suburb and town it is in, its local
/// municipality and its province. Any part may be missing; a mapper fills in what they know.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record MapAddress(
    [property: JsonPropertyName("house_number")] string? HouseNumber = null,
    [property: JsonPropertyName("road")] string? Road = null,
    [property: JsonPropertyName("suburb")] string? Suburb = null,
    [property: JsonPropertyName("village")] string? Village = null,
    [property: JsonPropertyName("town")] string? Town = null,
    [property: JsonPropertyName("city")] string? City = null,
    [property: JsonPropertyName("municipality")] string? Municipality = null,
    [property: JsonPropertyName("state")] string? State = null)
{
    /// <summary>The town or city it is in, the smallest a mapper named.</summary>
    public string? Place => Suburb ?? Village ?? Town ?? City;

    /// <summary>
    /// The local municipality it falls under, which is what "the town's branch" means (a town's shop can stand just
    /// outside its edge); a metro gives its city instead.
    /// </summary>
    public string? Area => Municipality ?? City ?? Town ?? Village;

    /// <summary>The street, with its number when the mapper gave one.</summary>
    public string? Street => HouseNumber is null ? Road : $"{HouseNumber} {Road}";
}
