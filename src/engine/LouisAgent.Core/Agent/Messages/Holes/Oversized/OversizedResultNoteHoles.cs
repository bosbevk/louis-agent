namespace LouisAgent.Core.Agent.Messages.Holes.Oversized;

/// <summary>The tool whose result was too big to send, and its length in characters, written for reading.</summary>
public sealed record OversizedResultNoteHoles(string Tool, string Chars);
