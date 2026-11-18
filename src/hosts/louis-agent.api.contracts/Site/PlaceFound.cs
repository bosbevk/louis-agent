namespace LouisAgent.Api.Contracts.Site;

/// <summary>One place a site's search found (F164.2): where it is, what it's called and its first words.</summary>
/// <param name="Address">Its own address (ADR 0107), such as <c>/how-it-works/the-web</c>.</param>
/// <param name="Heading">What the page calls it: a section's topic, a card's title.</param>
/// <param name="Text">Its first words, as the page shows them.</param>
public sealed record PlaceFound(string Address, string Heading, string Text);
