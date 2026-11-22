namespace LouisAgent.FilmExport.Cards.Pages;

/// <summary>
/// The site's name, photographed alone from its running header, and the size it stands at on a card, in points.
/// </summary>
/// <remarks>
/// A photograph, never the card's own drawing of the name, so the card shows the name exactly as the site draws it,
/// whatever the theme does to it next, as today's card photographs the glass ball.
/// </remarks>
public sealed record CardName(byte[] Picture, double Width, double Height);
