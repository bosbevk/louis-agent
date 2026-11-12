using LouisAgent.Diagrams.Classes;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Classes;

/// <summary>
/// A class chart is drawn as a flow (CL60.4, Claude's pick Q1: an Adapter): each type a box with its kind and its
/// members, each relation an arrow whose style and word say what kind of link it is (Q3).
/// </summary>
public class ClassFlowTests
{
    /// <summary>A speech provider: an interface, its implementation, the factory holding it, and what it makes.</summary>
    private static readonly ClassesSpec Speech = new(
        [
            new ClassType("provider", "ISpeechProvider", "interface", ["SpeechProviderName Name", "Create(SpeechOptions)"],
                DiagramIcon.Code, DiagramTone.Violet),
            new ClassType("whisper", "WhisperSpeechProvider", "class", Tone: DiagramTone.Green),
            new ClassType("factory", "SpeechClientFactory", Tone: DiagramTone.Violet),
            new ClassType("client", "ISpeechToTextClient", "interface", Tone: DiagramTone.Teal),
            new ClassType("base", "SpeechProviderBase", "abstract class", Tone: DiagramTone.Green),
            new ClassType("endpoints", "TranscriptionEndpoints", Tone: DiagramTone.Green)
        ],
        [
            new ClassRelation("whisper", "provider", RelationKind.Implements),
            new ClassRelation("whisper", "base", RelationKind.Inherits),
            new ClassRelation("factory", "provider", RelationKind.Holds),
            new ClassRelation("provider", "client", RelationKind.Creates),
            new ClassRelation("endpoints", "factory", RelationKind.Uses, "asks")
        ]);

    /// <summary>Each type is a box: its name the label, its kind the note under it, its members and tone kept.</summary>
    [Test]
    public void EachType_IsABoxWithItsKindAndMembers()
    {
        var provider = ClassFlow.Of(Speech).Nodes[0];

        Assert.That(provider, Is.EqualTo(provider with
        {
            Id = "provider",
            Label = "ISpeechProvider",
            Note = "interface",
            Icon = DiagramIcon.Code,
            Tone = DiagramTone.Violet
        }));
        Assert.That(provider.Members, Is.EqualTo(new[] { "SpeechProviderName Name", "Create(SpeechOptions)" }));
    }

    /// <summary>
    /// Each relation's kind is its arrow's style and word: a kind of, solid; implements, uses and creates, dashed; holds,
    /// solid; a word written on a relation takes the place of its kind's.
    /// </summary>
    [Test]
    public void EachRelationKind_HasItsStyleAndWord()
    {
        var edges = ClassFlow.Of(Speech).Edges;

        Assert.That(edges.Select(edge => (edge.Word, edge.Style)), Is.EqualTo(new[]
        {
            ("implements", EdgeStyle.Dashed), ("is a", EdgeStyle.Solid), ("holds", EdgeStyle.Solid),
            ("creates", EdgeStyle.Dashed), ((string?)"asks", EdgeStyle.Dashed)
        }));
    }

    /// <summary>
    /// A type that names no tone takes its kind's: an interface, a record or an enum is a contract or state (teal),
    /// anything else this project's own (green); a tone it names wins.
    /// </summary>
    [Test]
    public void AType_WithNoTone_TakesItsKinds()
    {
        var spec = new ClassesSpec(
            [
                new ClassType("a", "IFeed", "interface"), new ClassType("b", "Grade", "record, step 1"),
                new ClassType("c", "Runner", "core, step 2"), new ClassType("d", "Plain"),
                new ClassType("e", "ICheck", "interface", Tone: DiagramTone.Violet)
            ],
            []);

        Assert.That(ClassFlow.Of(spec).Nodes.Select(node => node.Tone), Is.EqualTo(new[]
        {
            DiagramTone.Teal, DiagramTone.Teal, DiagramTone.Green, DiagramTone.Green, DiagramTone.Violet
        }));
    }

    /// <summary>
    /// Arrows into one type with the same word say it once: the first keeps it, the rest go without, their style still
    /// saying what they are, so a fan of chips doesn't pile up by the type.
    /// </summary>
    [Test]
    public void ArrowsIntoOneType_SayASharedWordOnce()
    {
        var spec = new ClassesSpec(
            [new ClassType("i", "ICheck", "interface"), new ClassType("a", "A"), new ClassType("b", "B"), new ClassType("c", "C")],
            [
                new ClassRelation("a", "i", RelationKind.Implements), new ClassRelation("b", "i", RelationKind.Implements),
                new ClassRelation("c", "i", RelationKind.Uses, "reads")
            ]);

        Assert.That(ClassFlow.Of(spec).Edges.Select(edge => edge.Word),
            Is.EqualTo(new[] { "implements", null, "reads" }));
    }

    /// <summary>Arrows out of one type with the same word say it once too: a workflow holding five parts.</summary>
    [Test]
    public void ArrowsOutOfOneType_SayASharedWordOnce()
    {
        var spec = new ClassesSpec(
            [new ClassType("w", "TriageWorkflow"), new ClassType("a", "A"), new ClassType("b", "B")],
            [new ClassRelation("w", "a", RelationKind.Holds), new ClassRelation("w", "b", RelationKind.Holds)]);

        Assert.That(ClassFlow.Of(spec).Edges.Select(edge => edge.Word), Is.EqualTo(new[] { "holds", null }));
    }

    /// <summary>The drawing matches its golden file, looked at by eye when it was made.</summary>
    [Test]
    public void AClassChart_MatchesTheGoldenFile() =>
        Golden.Match("classes.svg", DiagramRenderers.Render(new DiagramSource(DiagramKind.Classes, "Speech",
            Classes: Speech)));

    /// <summary>Framed for the docs, a class chart's legend names the tones its types wear.</summary>
    [Test]
    public void TheDocsFrame_HasALegendOfTheTypesTones() =>
        Assert.That(DiagramRenderers.RenderTitled(new DiagramSource(DiagramKind.Classes, "Speech", Classes: Speech)),
            Does.Contain("the framework").And.Contain("this project"));
}
