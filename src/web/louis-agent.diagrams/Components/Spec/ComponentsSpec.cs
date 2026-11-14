namespace LouisAgent.Diagrams.Components.Spec;

/// <summary>
/// A component diagram as its JSON says it (F134.7): one component, everything it holds, and what stands around it.
/// </summary>
/// <param name="Root">The component the picture is of. What it holds is inside it.</param>
/// <param name="Around">What stands beside the root: who hands it things, what it reads, what it calls.</param>
public sealed record ComponentsSpec(ComponentBox Root, IReadOnlyList<ComponentNeighbour>? Around = null);
