using LouisAgent.Choices;

namespace LouisAgent.Packs.ShopPrices.Prices.Files;

/// <summary>
/// What a pack's amount counts: grams, millilitres or items. A size we can't scale by (a pallet of bricks, a 55 inch
/// television) is <see cref="Other"/>, priced only as it is.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum PackUnit
{
    [WireName("g")] Gram,
    [WireName("ml")] Millilitre,
    [WireName("each")] Each,
    [WireName("other")] Other,
}
