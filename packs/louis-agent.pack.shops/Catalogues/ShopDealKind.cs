namespace LouisAgent.Packs.Shops.Catalogues;

/// <summary>
/// The kind of deal a shop has on a product (F34 step 2; ADR 0024: a choice is an enum). Its price fields don't show
/// either: the shop says which kind in its answer, and what it is on the product's card.
/// </summary>
public enum ShopDealKind
{
    /// <summary>A multi-buy ("bonus buy"): <c>BUY 2 FOR R199</c>, <c>BUY 3 FOR 2</c>.</summary>
    Special,

    /// <summary>A promotion the shop flags on the product, its badge's wording as it gives it (<c>SAVE</c>).</summary>
    Promotion,
}
