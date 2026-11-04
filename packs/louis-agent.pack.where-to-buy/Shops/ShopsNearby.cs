using LouisAgent.Packs.WhereToBuy.Branches;
using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Shops;

/// <summary>
/// What a lookup of the supermarkets near a town found: the town as the map knows it, the supermarkets nearest first
/// (none when the map lists none), and how far around the town was searched, in kilometres.
/// </summary>
/// <remarks>
/// A record for a value (ADR 0040). A shop is a <see cref="Branch"/>: the same name, street, place and distance, since
/// two shops of one chain are two rows and the map's own street tells them apart.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record ShopsNearby(MapPlace Town, IReadOnlyList<Branch> Shops, int SearchedKm);
