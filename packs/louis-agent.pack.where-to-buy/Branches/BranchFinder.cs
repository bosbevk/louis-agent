using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Branches;

/// <summary>
/// Finds a shop's branches near the user's town, or the supermarkets near it when they named no shop: the town first,
/// then a search in a box around it, a wider box only when the smaller one holds none. The three nearest are kept, each
/// marked when it is in the town's own municipality.
/// </summary>
/// <remarks>
/// The smallest box comes first because the map answers at most 20 hits in no distance order: a box that reaches a
/// city fills with the city's branches and loses the town's own, and sorting afterwards can't bring it back. About
/// 16 km holds a town's branches, about 55 km its neighbours, and about 165 km is enough to name "the nearest" for a
/// small town without one. Only places the map files as shops count, so a "Checkers Street" is never a branch.
/// </remarks>
public sealed class BranchFinder(MapSearch map)
{
    /// <summary>The half-widths of the boxes searched, smallest first, in degrees (a degree is about 111 km).</summary>
    private static readonly double[] Boxes = [0.15, 0.5, 1.5];

    /// <summary>
    /// The same for any chain: about 3 km first, since a city's 16 km box fills with 20 hits in no order and loses the
    /// nearest; about 55 km is as far as "near you" goes.
    /// </summary>
    private static readonly double[] ShopBoxes = [0.03, 0.15, 0.5];

    private const string Supermarket = "supermarket";

    private const int Kept = 3;

    /// <summary>The branches near the town, or nothing when the map doesn't know the town.</summary>
    public Task<BranchSearch?> FindAsync(string shop, string town, CancellationToken cancellationToken)
    {
        var names = ShopNames.Of(shop);
        return SearchAsync(town, names[0], Boxes, place => ShopNames.IsBranch(place.Name, names), cancellationToken);
    }

    /// <summary>The supermarkets nearest the town, of any chain, or nothing when the map doesn't know it.</summary>
    public Task<BranchSearch?> FindShopsAsync(string town, CancellationToken cancellationToken) =>
        SearchAsync(town, Supermarket, ShopBoxes,
            place => place.Type == Supermarket && !string.IsNullOrWhiteSpace(place.Name), cancellationToken);

    /// <summary>The town first, then what's asked in each box until one holds some.</summary>
    private async Task<BranchSearch?> SearchAsync(string town, string query, double[] boxes,
        Func<MapPlace, bool> counts,
        CancellationToken cancellationToken)
    {
        if (await map.TownAsync(town, cancellationToken) is not { } place) return null;

        foreach (double degrees in boxes)
        {
            var found = await map.NearAsync(query, place, degrees, cancellationToken);
            var branches = found
                .Where(p => p.Category == "shop" && counts(p))
                .Select(p => Branch.Of(p, place))
                .OrderBy(b => b.Km)
                .Take(Kept)
                .ToList();
            if (branches.Count > 0) return new BranchSearch(place, query, branches, Km(degrees));
        }

        return new BranchSearch(place, query, [], Km(boxes[^1]));
    }

    /// <summary>A box's half-width in kilometres, roughly.</summary>
    private static int Km(double degrees) => (int)(degrees * 111);
}
