using LouisAgent.Diagrams.Engine.Types;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Sources.Types;

namespace LouisAgent.Diagrams.Tests.Types;

/// <summary>
/// CL65.3: a picture shows what type of diagram it is, by a name a person reads: "Class diagram", "Use case
/// diagram". The names are the engine's words, one for every type there is.
/// </summary>
public class TypeNamesTests
{
    /// <summary>A flow that says a type, which is how any type reaches a picture.</summary>
    private static DiagramSource Typed(DiagramType type) => new(DiagramKind.Flow, "A picture", Type: type);

    /// <summary>A type's name is UML's words for it, a capital first, and "diagram" after.</summary>
    [TestCase(DiagramType.Class, "Class diagram")]
    [TestCase(DiagramType.CompositeStructure, "Composite structure diagram")]
    [TestCase(DiagramType.StateMachine, "State machine diagram")]
    [TestCase(DiagramType.UseCase, "Use case diagram")]
    [TestCase(DiagramType.InteractionOverview, "Interaction overview diagram")]
    [TestCase(DiagramType.Overview, "Overview")]
    public void AType_IsCalledByUmlsWords(DiagramType type, string name) =>
        Assert.That(TypeNames.Of(Typed(type)), Is.EqualTo(name));

    /// <summary>Every type has a name, and every one of UML's ends in "diagram".</summary>
    [Test]
    public void EveryType_HasItsName()
    {
        var names = Enum.GetValues<DiagramType>().ToDictionary(type => type, type => TypeNames.Of(Typed(type)));

        Assert.Multiple(() =>
        {
            Assert.That(names.Where(name => string.IsNullOrWhiteSpace(name.Value)).Select(name => name.Key),
                Is.Empty, "a type without a name");
            Assert.That(names.Where(name => name.Key != DiagramType.Overview)
                .Where(name => name.Value?.EndsWith(" diagram", StringComparison.Ordinal) != true)
                .Select(name => name.Key), Is.Empty, "one of UML's that does not end in \"diagram\"");
            Assert.That(names.Values.Distinct().Count(), Is.EqualTo(names.Count), "two types with one name");
        });
    }

    /// <summary>
    /// A source built in code, which no reader filled in, is still named for its kind's own type. A flow that
    /// says none and a ticket have no name.
    /// </summary>
    [Test]
    public void ASourceThatSaysNoType_IsNamedForItsKindsOwn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(TypeNames.Of(new DiagramSource(DiagramKind.Sequence, "t")), Is.EqualTo("Sequence diagram"));
            Assert.That(TypeNames.Of(new DiagramSource(DiagramKind.Cards, "t")), Is.EqualTo("Overview"));
            Assert.That(TypeNames.Of(new DiagramSource(DiagramKind.Flow, "t")), Is.Null);
            Assert.That(TypeNames.Of(new DiagramSource(DiagramKind.Ticket, "t")), Is.Null);
        });
    }
}
