namespace LouisAgent.FilmExport.Messages.Holes.Cards;

/// <summary>A page card's words: the site's name, the page's name and its first heading.</summary>
public sealed record CardDescriptionHoles(string Site, string Page, string Heading);
