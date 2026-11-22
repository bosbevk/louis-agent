namespace LouisAgent.FilmExport.Messages.Holes.Cards;

/// <summary>Where a page's card was written, where its words end, and where they may end.</summary>
public sealed record PageCardWrittenHoles(string Path, string EndsAt, string Limit);
