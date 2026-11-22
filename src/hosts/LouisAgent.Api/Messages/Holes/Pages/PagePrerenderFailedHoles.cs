namespace LouisAgent.Api.Messages.Holes.Pages;

/// <summary>The page that couldn't be rendered for search engines, and the route that answers the shell.</summary>
public sealed record PagePrerenderFailedHoles(string Page, string Route);
