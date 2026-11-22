namespace LouisAgent.FilmExport.Cards.Pages;

/// <summary>A page of a site: its route, and the address its card shows, its host and path with no scheme.</summary>
public sealed record SitePage(string Route, string Address);
