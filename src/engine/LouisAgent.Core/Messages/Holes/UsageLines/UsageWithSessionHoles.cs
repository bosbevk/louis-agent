namespace LouisAgent.Core.Messages.Holes.UsageLines;

/// <summary>A turn's usage line, and what the whole session has cost so far.</summary>
public sealed record UsageWithSessionHoles(string Line, string Cost);
