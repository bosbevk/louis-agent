namespace LouisAgent.Composition.Sites;

/// <summary>
/// One place on a page of the site (F127.7): where it is, what it's called, what kind it is and its words.
/// </summary>
/// <param name="Address">Its own address (ADR 0107), such as <c>/framework/measured/72-72</c>.</param>
/// <param name="Heading">
/// What it's called on the page: a section's kicker, a card's title, a piece's title or number.
/// </param>
/// <param name="Kind">A section, a card or a piece.</param>
/// <param name="Text">Its first words, as the page shows them.</param>
public sealed record SitePlace(string Address, string Heading, SitePlaceKind Kind, string Text);
