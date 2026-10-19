namespace LouisAgent.Packs.Shops.Catalogues;

/// <summary>
/// The shops whose catalogue Puls can read (F33; an enum, ADR 0024), in the order the "supported" message lists them.
/// Shoprite, Pick n Pay and Woolworths come as providers of their own.
/// </summary>
public enum ShopKind
{
    Checkers,
}
