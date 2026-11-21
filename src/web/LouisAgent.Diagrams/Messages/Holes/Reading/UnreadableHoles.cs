namespace LouisAgent.Diagrams.Messages.Holes.Reading;

/// <summary>Why a diagram's file couldn't be read, in the reader's own words.</summary>
public sealed record UnreadableHoles(string Reason);
