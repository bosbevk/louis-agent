namespace LouisAgent.FilmExport.Messages.Holes;

/// <summary>The film written, where, and how big.</summary>
public sealed record ExportedHoles(string Film, string Path, string Megabytes);
