namespace LouisAgent.FilmExport.Messages.Holes.Cards;

/// <summary>A page's title that doesn't hold the site's name, and that name.</summary>
public sealed record TitleWithoutSiteHoles(string Title, string Site);
