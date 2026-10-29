namespace LouisAgent.Packs.Shops.Catalogues;

/// <summary>
/// The shops whose catalogue Puls can read (F33; an enum, ADR 0024), in the order the "supported" message lists them.
/// Shoprite, Pick n Pay and Woolworths come as providers of their own.
/// </summary>
public enum ShopKind
{
    Checkers,
}

public static class ShopKinds
{
    /// <summary>The shop as a person reads it in a result (<c>Checkers</c>): the pack's own table, never derived from the wire name (<c>checkers</c>), so a text the model reads does not follow a rename of the wire.</summary>
    public static string DisplayName(ShopKind kind) => kind switch
    {
        ShopKind.Checkers => "Checkers",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
