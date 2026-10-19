namespace LouisAgent.Packs.Shops;

/// <summary>A product's deal (F34 step 2): its kind, and its wording from the product's card, null when the card shows none.</summary>
public sealed record ShopDeal(ShopDealKind Kind, string? Text);
