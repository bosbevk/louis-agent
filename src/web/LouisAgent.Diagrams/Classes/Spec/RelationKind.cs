using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Classes.Spec;

/// <summary>What a link between two types means: one is a kind of the other, implements it, holds it, uses or makes it.</summary>
public enum RelationKind
{
    [WireName("inherits")] Inherits,
    [WireName("implements")] Implements,
    [WireName("holds")] Holds,
    [WireName("uses")] Uses,
    [WireName("creates")] Creates
}
