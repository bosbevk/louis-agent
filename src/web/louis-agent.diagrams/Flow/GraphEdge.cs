namespace LouisAgent.Diagrams.Flow;

/// <summary>One arrow of a diagram, from one node's id to another's, with a word on it when one helps.</summary>
public sealed record GraphEdge(string From, string To, string? Word = null);
