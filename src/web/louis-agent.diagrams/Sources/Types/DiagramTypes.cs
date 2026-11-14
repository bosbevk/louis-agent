namespace LouisAgent.Diagrams.Sources.Types;

/// <summary>
/// Which types of diagram a kind of picture may be (CL65.3). A kind is how a picture is laid out, so most kinds
/// are one type and need not say it. A flow is boxes and arrows, which draw eight of UML's types, so it says
/// which. A ticket is a card in a chat and no diagram at all.
/// </summary>
public static class DiagramTypes
{
    /// <summary>The types boxes and arrows can draw, in the order a refusal lists them.</summary>
    private static readonly IReadOnlyList<DiagramType> OfAFlow =
    [
        DiagramType.Activity, DiagramType.Component, DiagramType.Deployment, DiagramType.Object,
        DiagramType.Package, DiagramType.StateMachine, DiagramType.UseCase, DiagramType.Communication
    ];

    /// <summary>The one type a kind is, when it is only ever one. None for a flow, which says, and a ticket.</summary>
    public static DiagramType? Own(DiagramKind kind) => kind switch
    {
        DiagramKind.Sequence => DiagramType.Sequence,
        DiagramKind.Classes => DiagramType.Class,
        DiagramKind.Composite or DiagramKind.Nested => DiagramType.CompositeStructure,
        DiagramKind.Cards => DiagramType.Overview,
        _ => null
    };

    /// <summary>The types a picture of a kind may say it is: a flow's eight, a kind's own one, or none.</summary>
    public static IReadOnlyList<DiagramType> MayBe(DiagramKind kind) => kind switch
    {
        DiagramKind.Flow => OfAFlow,
        _ when Own(kind) is { } own => [own],
        _ => []
    };
}
