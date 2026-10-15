namespace LouisAgent.Api.Services;

/// <summary>A saved run under <c>samples/</c> (F27): the folder, the check it came from, when, how many repeats, and its cost in USD.</summary>
/// <param name="Check">The check's id, or null for a run folder the catalog doesn't recognise.</param>
/// <param name="Cost">The modes' costs added (a repeated run: their means); null when the files hold none.</param>
public sealed record SavedRun(string Folder, string? Check, DateTimeOffset SavedAt, int Repeats, decimal? Cost);
