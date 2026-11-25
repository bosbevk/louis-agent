using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Site.Content.Privacy;

/// <summary>
/// One tile of the privacy page: its icon, a short title, and what the site does, in a sentence or a few.
/// </summary>
/// <param name="Icon">The line icon in the tile's chip.</param>
/// <param name="Title">What the tile is about, in a few words.</param>
/// <param name="Text">What the site keeps or sends of it, and where.</param>
public sealed record PrivacyTile(DiagramIcon Icon, string Title, string Text);
