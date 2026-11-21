namespace LouisAgent.Diagrams.Nested.Spec;

/// <summary>
/// The one thing a component holds of its own (F134.7), such as which demo is forward, and the pins that carry it.
/// </summary>
/// <param name="Says">What it is, in a few words.</param>
/// <param name="Down">The pins it is handed down through, each named <c>box.Pin</c>.</param>
/// <param name="Up">The pins it is changed through, each named <c>box.Pin</c>.</param>
public sealed record NestedState(
    string Says,
    IReadOnlyList<string>? Down = null,
    IReadOnlyList<string>? Up = null);
