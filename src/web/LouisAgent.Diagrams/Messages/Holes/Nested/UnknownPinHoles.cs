namespace LouisAgent.Diagrams.Messages.Holes.Nested;

/// <summary>
/// The box that holds a state, the pin its state names that it cannot reach, and the list that pin should be in.
/// </summary>
public sealed record UnknownPinHoles(string Holder, string Pin, string List);
