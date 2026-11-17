namespace LouisAgent.FilmExport.Messages.Holes;

/// <summary>The film being drawn, and how many frames it takes.</summary>
public sealed record RenderingHoles(string Film, int Frames);
