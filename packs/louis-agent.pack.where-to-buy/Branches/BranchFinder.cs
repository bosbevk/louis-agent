using LouisAgent.Packs.WhereToBuy.Branches.Map;
namespace LouisAgent.Packs.WhereToBuy.Branches;

/// <summary>
/// Finds a shop's branches near the user's town: the town first, then the shop's name in a box around it, a wider
/// box only when the first holds none. The three nearest are kept, each marked when it is in the town's own
/// municipality.
/// </summary>
/// <remarks>
/// Two searches at most for the shop: about 55 km around the town covers a town and its neighbours, and about 165 km
/// is enough to name "the nearest" for a small town without one. Only places the map files as shops count, so a
/// "Checkers Street" is never a branch.
/// </remarks>
public sealed class BranchFinder(MapSearch map)
{
    /// <summary>The half-widths of the boxes searched, in degrees (a degree is about 111 km).</summary>
    private static readonly double[] Boxes = [0.5, 1.5];

    private const int Kept = 3;

    /// <summary>The branches near the town, or nothing when the map doesn't know the town.</summary>
    public async Task<BranchSearch?> FindAsync(string shop, string town, CancellationToken cancellationToken)
    {
        if (await map.TownAsync(town, cancellationToken) is not { } place) return null;

        var names = ShopNames.Of(shop);
        foreach (double degrees in Boxes)
        {
            var found = await map.NearAsync(names[0], place, degrees, cancellationToken);
            var branches = found
                .Where(p => p.Category == "shop" && ShopNames.IsBranch(p.Name, names))
                .Select(p => Branch.Of(p, place))
                .OrderBy(b => b.Km)
                .Take(Kept)
                .ToList();
            if (branches.Count > 0) return new BranchSearch(place, names[0], branches, Km(degrees));
        }

        return new BranchSearch(place, names[0], [], Km(Boxes[^1]));
    }

    /// <summary>A box's half-width in kilometres, roughly.</summary>
    private static int Km(double degrees) => (int)(degrees * 111);
}
