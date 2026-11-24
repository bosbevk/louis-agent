namespace LouisAgent.Packs.Files.Messages.Holes.Search;

/// <summary>The part of a long line a search hit shows, and the whole line's length, written out.</summary>
public sealed record HitLineCutHoles(string Shown, string Length);
