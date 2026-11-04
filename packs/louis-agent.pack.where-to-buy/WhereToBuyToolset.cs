using LouisAgent.Choices;

namespace LouisAgent.Packs.WhereToBuy;

/// <summary>
/// The toolsets this pack offers, by the names they travel under. The pack keeps its own copy of the name (ADR 0043),
/// so it never needs another pack's or an identity's.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum WhereToBuyToolset
{
    [WireName("where-to-buy")] WhereToBuy,
}
