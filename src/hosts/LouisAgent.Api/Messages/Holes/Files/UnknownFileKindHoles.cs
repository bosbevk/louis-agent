namespace LouisAgent.Api.Messages.Holes.Files;

/// <summary>A file of a kind a model can't read: its name, its kind, and the kinds a model reads.</summary>
public sealed record UnknownFileKindHoles(string Name, string Kind, string Supported);
