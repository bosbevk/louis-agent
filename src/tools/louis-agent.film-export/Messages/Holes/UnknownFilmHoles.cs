namespace LouisAgent.FilmExport.Messages.Holes;

/// <summary>A film named that the tool doesn't know, and the ones it does.</summary>
public sealed record UnknownFilmHoles(string Name, string Supported);
