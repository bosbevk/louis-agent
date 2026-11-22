namespace LouisAgent.FilmExport.Messages.Holes.Cards;

/// <summary>The page whose card's words run past the middle, where they end, and where they may end.</summary>
public sealed record CardRunsPastHoles(string Page, string EndsAt, string Limit);
