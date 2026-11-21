namespace LouisAgent.Api.Messages.Holes.Pages;

/// <summary>The page the visitor reads: where, its title, its words, and its areas, links and section in view.</summary>
public sealed record PageHoles(string Url, string Title, string Text, string Areas, string Links, string InView);
