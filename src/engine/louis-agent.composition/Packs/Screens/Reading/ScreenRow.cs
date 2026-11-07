namespace LouisAgent.Composition.Packs.Screens.Reading;

/// <summary>One row: what identifies it, and a value per column, as text. Null is "unknown", never 0 or empty.</summary>
public sealed record ScreenRow(string Id, IReadOnlyList<string?> Cells);
