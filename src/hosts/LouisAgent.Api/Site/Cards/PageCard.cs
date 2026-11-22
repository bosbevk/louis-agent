namespace LouisAgent.Api.Site.Cards;

/// <summary>
/// A page's own share card: the picture a link to the page shows, as a path in the site's web root, and the words
/// that say what it shows, for someone who can't see it.
/// </summary>
/// <remarks>
/// Louis picked, on 2026-11-22, that the host learns a page's picture by where its file is (F169.6), over a page or a
/// menu entry that names it. So this is plain data read from two files, and nothing in a page's code says it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record PageCard(string Picture, string Description);
