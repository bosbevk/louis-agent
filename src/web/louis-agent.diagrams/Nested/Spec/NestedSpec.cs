namespace LouisAgent.Diagrams.Nested.Spec;

/// <summary>
/// A nested picture as its JSON says it (F134.7): one component, everything it holds, and what stands around it.
/// </summary>
/// <remarks>
/// The kind is named for how it is laid out, boxes inside a box (Louis's pick, 2026-11-15). It was
/// <c>components</c>, which read as UML's component diagram, and what it shows is a composite structure. The
/// other two ways were to keep that name with a line saying so, or to fold the kind into <c>composite</c> with
/// the layout as a field.
/// </remarks>
/// <param name="Root">The component the picture is of. What it holds is inside it.</param>
/// <param name="Around">What stands beside the root: who hands it things, what it reads, what it calls.</param>
/// <param name="Subtitle">What stands after the picture's title, such as where the component is.</param>
public sealed record NestedSpec(
    NestedBox Root,
    IReadOnlyList<NestedNeighbour>? Around = null,
    string? Subtitle = null);
