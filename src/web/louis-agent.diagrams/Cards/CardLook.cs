using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// How a card looks: a plain card with its words from the left, a highlighted one centred in the accent's tint, as
/// the core engine stands out in Louis's architecture figure, or a heading, centred with no box, as the figure's
/// application on top.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum CardLook
{
    [WireName("plain")] Plain,
    [WireName("highlight")] Highlight,
    [WireName("heading")] Heading
}
