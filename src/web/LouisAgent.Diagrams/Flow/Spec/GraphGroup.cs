namespace LouisAgent.Diagrams.Flow.Spec;

/// <summary>
/// A group of boxes (CL60.2), as a docs flowchart's subgraph: what a box names it by, and the words over its frame.
/// </summary>
public sealed record GraphGroup(string Id, string Label);
