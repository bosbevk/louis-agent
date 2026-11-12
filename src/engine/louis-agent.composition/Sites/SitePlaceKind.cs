using LouisAgent.Choices;

namespace LouisAgent.Composition.Sites;

/// <summary>
/// The kinds of place on a page (F127.7), by what showing one does: a section scrolls into view, a card opens, a piece
/// is pressed (ADR 0024).
/// </summary>
public enum SitePlaceKind
{
    [WireName("section")] Section,
    [WireName("card")] Card,
    [WireName("piece")] Piece,
}
