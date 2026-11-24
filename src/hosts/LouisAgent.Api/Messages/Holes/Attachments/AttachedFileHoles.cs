namespace LouisAgent.Api.Messages.Holes.Attachments;

/// <summary>A photo or a PDF as the model reads of it: its name, its kind and its size, never its bytes.</summary>
public sealed record AttachedFileHoles(string Name, string Kind, string Size);
