using LouisAgent.Packs.WhereToBuy.Branches.Map;
namespace LouisAgent.Packs.WhereToBuy.Branches;

/// <summary>
/// A shop's branch near the user: its name as the map has it, its street and place, how far it is from the town's
/// centre in a straight line, whether it falls in the town's own municipality, and where it is, for directions.
/// </summary>
public sealed record Branch(
    string Name,
    string? Street,
    string? Place,
    double Km,
    bool InTownsArea,
    double Latitude,
    double Longitude)
{
    /// <summary>A map result as a branch, measured from the town.</summary>
    public static Branch Of(MapPlace place, MapPlace town) => new(
        place.Name ?? "",
        place.Address?.Street,
        place.Address?.Place,
        Distance.Km(town.Latitude, town.Longitude, place.Latitude, place.Longitude),
        place.Address?.Area is { } area &&
        string.Equals(area, town.Address?.Area, StringComparison.OrdinalIgnoreCase),
        place.Latitude,
        place.Longitude);
}
