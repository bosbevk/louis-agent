namespace LouisAgent.Core.Agent.Messages.Holes.Oversized;

/// <summary>What the model is sent in place of a big result: the note about its size, then the summary.</summary>
public sealed record SummarisedResultHoles(string Note, string Summary);
