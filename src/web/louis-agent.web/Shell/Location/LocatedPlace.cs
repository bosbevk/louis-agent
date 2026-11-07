namespace LouisAgent.Web.Shell.Location;

/// <summary>What the page's script answers about the location: the place, or why there isn't one.</summary>
public sealed record LocatedPlace(string? Place, string? Error);
