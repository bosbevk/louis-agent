using LouisAgent.Diagrams.Engine.Messages;
using LouisAgent.Diagrams.Messages;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Sources.Types;

namespace LouisAgent.Diagrams.Engine.Types;

/// <summary>
/// What a picture's type of diagram is called, as the picture shows it at the right of its title's line (CL65.3,
/// Louis: "the type of diagram must always display"): "Class diagram", "Use case diagram", "Overview".
/// </summary>
/// <remarks>
/// The types are data, in the diagrams' project, and their names are words a drawing writes, so they are here
/// with the drawings' other words. One table from a type to its word, and a test that every type has one, so a
/// new type is not left without its name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public static class TypeNames
{
    /// <summary>
    /// The name of the type a picture is: the one it says, or its kind's own when it says none, as a source
    /// built in code does. Nothing for a flow that says none, and for a ticket.
    /// </summary>
    public static string? Of(DiagramSource source) =>
        (source.Type ?? DiagramTypes.Own(source.Kind)) is { } type ? DiagramText.Format(Word(type)) : null;

    /// <summary>A type's word among the drawings' words.</summary>
    private static DrawingMessage Word(DiagramType type) => type switch
    {
        DiagramType.Class => DrawingMessage.TypeClass,
        DiagramType.CompositeStructure => DrawingMessage.TypeCompositeStructure,
        DiagramType.Object => DrawingMessage.TypeObject,
        DiagramType.Component => DrawingMessage.TypeComponent,
        DiagramType.Deployment => DrawingMessage.TypeDeployment,
        DiagramType.Package => DrawingMessage.TypePackage,
        DiagramType.StateMachine => DrawingMessage.TypeStateMachine,
        DiagramType.Activity => DrawingMessage.TypeActivity,
        DiagramType.UseCase => DrawingMessage.TypeUseCase,
        DiagramType.Sequence => DrawingMessage.TypeSequence,
        DiagramType.Communication => DrawingMessage.TypeCommunication,
        DiagramType.InteractionOverview => DrawingMessage.TypeInteractionOverview,
        DiagramType.Timing => DrawingMessage.TypeTiming,
        DiagramType.Overview => DrawingMessage.TypeOverview,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
