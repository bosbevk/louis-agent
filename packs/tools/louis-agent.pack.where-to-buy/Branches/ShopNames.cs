namespace LouisAgent.Packs.WhereToBuy.Branches;

/// <summary>
/// The names a shop goes by on the map: "PnP" is searched as "Pick n Pay", and a branch called "PnP Express" is still
/// a Pick n Pay. A shop not listed here goes by the name the model gave.
/// </summary>
/// <remarks>Compared squashed (lower case, letters and digits only), so "Pick n Pay" and "Pick 'n Pay" meet.</remarks>
public static class ShopNames
{
    /// <summary>The shops people shorten, each with the name the map uses first.</summary>
    private static readonly IReadOnlyList<string[]> Known =
    [
        ["Pick n Pay", "PnP"],
        ["Woolworths", "Woolies"],
        ["Shoprite", "Shoprite"],
    ];

    /// <summary>The shop's names, the one to search the map with first.</summary>
    public static IReadOnlyList<string> Of(string shop) =>
        Known.FirstOrDefault(names => names.Any(name => Squash(name) == Squash(shop))) ?? [shop.Trim()];

    /// <summary>Whether a place on the map is one of the shop's branches, by its name.</summary>
    public static bool IsBranch(string? placeName, IReadOnlyList<string> names) =>
        placeName is not null && names.Any(name => Squash(placeName).Contains(Squash(name), StringComparison.Ordinal));

    /// <summary>A name in lower case, letters and digits only.</summary>
    private static string Squash(string name) =>
        new([.. name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
}
