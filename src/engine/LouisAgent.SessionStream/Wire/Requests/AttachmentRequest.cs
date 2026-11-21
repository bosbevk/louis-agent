namespace LouisAgent.SessionStream.Wire.Requests;

/// <summary>A file that goes with a message: its name and its text.</summary>
public sealed record AttachmentRequest(string Name, string? Content);
