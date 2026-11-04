using LouisAgent.Packs.WhereToBuy.Branches;
using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Shops;

/// <summary>
/// Finds the supermarkets near a user's town in one map search ("supermarket", in a box around the town): the town
/// first, then the shops of the box, nearest first. A shop is a row of its own, so two Pick n Pays are two rows.
/// </summary>
/// <remarks>
/// The map answers a box with at most twenty places in no order of distance, so a box that is too wide can cut the
/// nearest off. The search starts with a box about 5.5 km from the town's centre, which holds a town's own shops, and
/// widens to about 17 km only while it holds fewer than <see cref="Kept"/> shops, adding what the wider one finds to
/// what the smaller one did (so the near ones are never lost to the cut). With no shop even there, one last box of
/// about 55 km. Only places the map files as a shop of the type supermarket, with a name, count; the same place twice
/// is one. It sits beside <see cref="BranchFinder"/>, which stays as it is, and shares its map and its
/// <see cref="Branch"/>.
/// </remarks>
public sealed class ShopFinder(MapSearch map)
{
    /// <summary>
    /// How many supermarkets are kept: eight, so Malmesbury's six (two of them Pick n Pay) all fit with room for a
    /// seventh the map may add, and a big city's suburb still gets a handful.
    /// </summary>
    public const int Kept = 8;

    /// <summary>
    /// The half-widths of the boxes tried in turn, in degrees (a degree is about 111 km): 0.05 holds a town and its
    /// edge, 0.15 its neighbours (Riebeek-Wes is 18 km from Malmesbury).
    /// </summary>
    private static readonly double[] Boxes = [0.05, 0.15];

    /// <summary>The last box, tried only when the smaller ones found no shop at all: a lone town's nearest.</summary>
    private const double WideBox = 0.5;

    /// <summary>What the map is asked for: the kind of place, not a shop's name.</summary>
    private const string Query = "supermarket";

    /// <summary>The map's category and type of a supermarket.</summary>
    private const string ShopCategory = "shop";

    /// <summary>The supermarkets near the town, or nothing when the map doesn't know the town.</summary>
    public async Task<ShopsNearby?> FindAsync(string town, CancellationToken cancellationToken)
    {
        if (await map.TownAsync(town, cancellationToken) is not { } place) return null;

        // Widen only while the box holds fewer than we keep, adding to what the smaller box found.
        var shops = new List<Branch>();
        double searched = 0;
        foreach (double degrees in Boxes)
        {
            shops = [.. shops.Concat(await SupermarketsAsync(place, degrees, cancellationToken)).Distinct()];
            searched = degrees;
            if (shops.Count >= Kept) break;
        }

        // A lone town with no shop nearby: one last, wide look.
        if (shops.Count == 0)
        {
            shops = await SupermarketsAsync(place, WideBox, cancellationToken);
            searched = WideBox;
        }

        var nearest = shops.OrderBy(s => s.Km).ThenBy(s => s.Name, StringComparer.Ordinal).Take(Kept).ToList();
        return new ShopsNearby(place, nearest, Km(searched));
    }

    /// <summary>The supermarkets in a box around the town, each as a shop measured from the town.</summary>
    private async Task<List<Branch>> SupermarketsAsync(MapPlace town, double degrees,
        CancellationToken cancellationToken)
    {
        var found = await map.NearAsync(Query, town, degrees, cancellationToken);
        return
        [
            .. found.Where(p => p.Category == ShopCategory && p.Type == Query && !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => Branch.Of(p, town))
        ];
    }

    /// <summary>A box's half-width in kilometres, roughly.</summary>
    private static int Km(double degrees) => (int)(degrees * 111);
}
