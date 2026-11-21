using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Cards.Links;

/// <summary>
/// Which way a link's arrow points: from the first card to the second, back, or both ways, as the arrows between
/// the engine and its identities and packs do in Louis's figure.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum CardLinkKind
{
    [WireName("down")] Down,
    [WireName("up")] Up,
    [WireName("both")] Both
}
