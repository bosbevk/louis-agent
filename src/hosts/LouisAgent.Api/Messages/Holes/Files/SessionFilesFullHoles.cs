namespace LouisAgent.Api.Messages.Holes.Files;

/// <summary>
/// A file that would take a session's files past their limit: its name, what they would come to and the most a
/// session keeps, in megabytes.
/// </summary>
public sealed record SessionFilesFullHoles(string Name, string Megabytes, int Max);
