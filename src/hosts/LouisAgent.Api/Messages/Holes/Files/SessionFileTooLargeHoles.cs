namespace LouisAgent.Api.Messages.Holes.Files;

/// <summary>A file over the limit: its name, its size and the most a file may weigh, in megabytes.</summary>
public sealed record SessionFileTooLargeHoles(string Name, string Megabytes, int Max);
