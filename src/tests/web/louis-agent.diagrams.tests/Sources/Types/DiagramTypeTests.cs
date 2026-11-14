using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Sources.Types;

namespace LouisAgent.Diagrams.Tests.Sources.Types;

/// <summary>
/// CL65.3: a picture has a type, by UML's name for it, beside its kind. A kind that is only ever one type gets it
/// filled in, a flow says which it is, and a type that is not the kind's is refused with what the kind may be.
/// </summary>
public class DiagramTypeTests
{
    /// <summary>A cards picture, with a type where the test puts one, at <c>TYPE</c>.</summary>
    private const string Cards = """
                                 {
                                   "kind": "cards",TYPE
                                   "title": "Engine and packs",
                                   "cards": {
                                     "rows": [
                                       { "cards": [
                                         { "id": "engine", "icon": "chip", "title": "Engine", "text": "The loop." }
                                       ] }
                                     ],
                                     "links": []
                                   }
                                 }
                                 """;

    /// <summary>A flow of two boxes, with a type where the test puts one.</summary>
    private const string Flow = """
                                {
                                  "kind": "flow",TYPE
                                  "title": "A turn",
                                  "flow": {
                                    "nodes": [
                                      { "id": "host", "label": "A host", "column": 0 },
                                      { "id": "engine", "label": "The engine", "column": 1 }
                                    ],
                                    "edges": [ { "from": "host", "to": "engine", "word": "runs" } ]
                                  }
                                }
                                """;

    /// <summary>A ticket of one row, with a type where the test puts one.</summary>
    private const string Ticket = """
                                  {
                                    "kind": "ticket",TYPE
                                    "title": "Booking request: the garden room",
                                    "ticket": {
                                      "kicker": "BOOKING REQUEST",
                                      "title": "The garden room",
                                      "rows": [ { "icon": "users", "label": "Guests", "value": "2 adults" } ]
                                    }
                                  }
                                  """;

    /// <summary>The file, with the type it says, or with none.</summary>
    private static string With(string file, string? type = null) =>
        file.Replace("TYPE", type is null ? "" : $""" "type": "{type}",""");

    /// <summary>A kind that is only ever one type of diagram has it as its own.</summary>
    [TestCase(DiagramKind.Sequence, DiagramType.Sequence)]
    [TestCase(DiagramKind.Classes, DiagramType.Class)]
    [TestCase(DiagramKind.Composite, DiagramType.CompositeStructure)]
    [TestCase(DiagramKind.Components, DiagramType.CompositeStructure)]
    [TestCase(DiagramKind.Cards, DiagramType.Overview)]
    public void AKindThatIsOneType_HasItAsItsOwn(DiagramKind kind, DiagramType type)
    {
        Assert.Multiple(() =>
        {
            Assert.That(DiagramTypes.Own(kind), Is.EqualTo(type));
            Assert.That(DiagramTypes.MayBe(kind), Is.EqualTo(new[] { type }));
        });
    }

    /// <summary>A flow draws several types, so it has none of its own, and a ticket is no diagram at all.</summary>
    [Test]
    public void AFlowSaysWhichOfEightItIs_AndATicketHasNone()
    {
        DiagramType[] ofAFlow =
        [
            DiagramType.Activity, DiagramType.Component, DiagramType.Deployment, DiagramType.Object,
            DiagramType.Package, DiagramType.StateMachine, DiagramType.UseCase, DiagramType.Communication
        ];

        Assert.Multiple(() =>
        {
            Assert.That(DiagramTypes.Own(DiagramKind.Flow), Is.Null);
            Assert.That(DiagramTypes.MayBe(DiagramKind.Flow), Is.EqualTo(ofAFlow));
            Assert.That(DiagramTypes.Own(DiagramKind.Ticket), Is.Null);
            Assert.That(DiagramTypes.MayBe(DiagramKind.Ticket), Is.Empty);
        });
    }

    /// <summary>A cards picture need not say it is an overview: the reader fills it in.</summary>
    [Test]
    public void AKindsOwnType_IsFilledIn()
    {
        var source = DiagramReader.Read(With(Cards));

        Assert.That(source.Type, Is.EqualTo(DiagramType.Overview));
    }

    /// <summary>A kind may say its own type, and a flow keeps the one it wrote, hyphen and all.</summary>
    [Test]
    public void ATypeThatIsTheKinds_IsKept()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DiagramReader.Read(With(Cards, "overview")).Type, Is.EqualTo(DiagramType.Overview));
            Assert.That(DiagramReader.Read(With(Flow, "activity")).Type, Is.EqualTo(DiagramType.Activity));
            Assert.That(DiagramReader.Read(With(Flow, "state-machine")).Type, Is.EqualTo(DiagramType.StateMachine));
        });
    }

    /// <summary>
    /// A flow draws eight types, so one that says none is refused with the eight, where another kind's is filled
    /// in. A ticket says none and reads, since it has none.
    /// </summary>
    [Test]
    public void AFlowWithoutAType_IsRefused_AndATicketWithoutOneReads()
    {
        var refused = Assert.Throws<InvalidDataException>(() => DiagramReader.Read(With(Flow)));

        Assert.Multiple(() =>
        {
            Assert.That(refused!.Message, Is.EqualTo(
                """A flow picture must say what type of diagram it is. Its "type" may be: activity, """ +
                "component, deployment, object, package, state-machine, use-case, communication."));
            Assert.That(DiagramReader.Read(With(Ticket)).Type, Is.Null);
        });
    }

    /// <summary>A type that is not the kind's is refused, with the kind, the type and what the kind may be.</summary>
    [Test]
    public void ATypeThatIsNotTheKinds_IsRefused()
    {
        var cards = Assert.Throws<InvalidDataException>(() => DiagramReader.Read(With(Cards, "class")));
        var flow = Assert.Throws<InvalidDataException>(() => DiagramReader.Read(With(Flow, "sequence")));

        Assert.Multiple(() =>
        {
            Assert.That(cards!.Message, Is.EqualTo(
                """A cards picture can't be a 'class' diagram. Its "type" may be: overview."""));
            Assert.That(flow!.Message, Is.EqualTo(
                """A flow picture can't be a 'sequence' diagram. Its "type" may be: activity, component, """ +
                "deployment, object, package, state-machine, use-case, communication."));
        });
    }

    /// <summary>A ticket is a card in a chat, so one that says a type is told to take it out.</summary>
    [Test]
    public void ATicketThatSaysAType_IsRefused()
    {
        var refused = Assert.Throws<InvalidDataException>(() => DiagramReader.Read(With(Ticket, "object")));

        Assert.That(refused!.Message, Is.EqualTo(
            """A ticket is not a diagram of any type. Take its "type" out."""));
    }

    /// <summary>A name that is no type of diagram is refused with the types there are.</summary>
    [Test]
    public void ANameThatIsNoType_IsRefused()
    {
        var refused = Assert.Throws<InvalidDataException>(() => DiagramReader.Read(With(Flow, "blueprint")));

        Assert.That(refused!.Message, Does.StartWith(
            "'blueprint' is not a type of diagram. The types are class, composite-structure, object,"));
    }

    /// <summary>
    /// A type is written by its name, straight after the kind, where a person would write it. A kind's own type
    /// is not written, as nothing at its default is.
    /// </summary>
    [Test]
    public void AType_IsWrittenByItsNameAfterTheKind_AndAKindsOwnIsLeftOut()
    {
        string flow = DiagramWriter.Write(DiagramReader.Read(With(Flow, "use-case")));
        string cards = DiagramWriter.Write(DiagramReader.Read(With(Cards)));

        Assert.Multiple(() =>
        {
            Assert.That(flow, Does.StartWith("{\n  \"kind\": \"flow\",\n  \"type\": \"use-case\",\n  \"title\""));
            Assert.That(cards, Does.Not.Contain("\"type\""));
        });
    }
}
