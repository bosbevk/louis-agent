using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence.Listed;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.StepLists;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.StepLists;

/// <summary>
/// One step of a sequence drawn for a phone (F169.7): a call's two names with the wide drawing's beam between them,
/// an answer's with its dashes, a call to oneself with a turn, a number in its ring, and a note on glass. The
/// drawing decides no place itself, so these tests are about what is drawn and how it is marked.
/// </summary>
public class StepListStepsTests
{
    /// <summary>A person, the API, and a part with a long name.</summary>
    private static readonly Participant[] People =
    [
        new("you", "You", Actor: true, Tone: DiagramTone.Blue),
        new("api", "LouisAgent.Api", Tone: DiagramTone.Violet),
        new("long", "A participant whose name runs on and on")
    ];

    /// <summary>One step laid out on its own at a phone's width, numbered when asked.</summary>
    private static PlacedStep Placed(SequenceStep step, bool numbered = false) =>
        StepListLayout.Place(new SequenceSpec(People, [step], numbered), 338, StepListMeasures.Marquee).Steps[0];

    /// <summary>The shapes a step draws.</summary>
    private static List<XElement> Drawn(PlacedStep step)
    {
        var writer = new SvgWriter();
        StepListSteps.Draw(writer, step, Geometries.InUse);
        return [.. writer.Shapes.SelectMany(shape => shape.DescendantsAndSelf())];
    }

    /// <summary>The shapes in a class.</summary>
    private static List<XElement> In(List<XElement> shapes, string cls) =>
        [.. shapes.Where(shape => ((string?)shape.Attribute("class") ?? "").Split(' ').Contains(cls))];

    /// <summary>The words of the shapes in a class.</summary>
    private static List<string> Words(List<XElement> shapes, string cls) => [.. In(shapes, cls).Select(t => t.Value)];

    /// <summary>A call writes both names, each after a square in its tone, a beam between, and its words.</summary>
    [Test]
    public void ACall_WritesBothNames_ABeamBetween_AndItsWords()
    {
        var shapes = Drawn(Placed(new MessageStep("you", "api", "POST /v1/sessions/{id}/messages")));

        Assert.Multiple(() =>
        {
            Assert.That(Words(shapes, "stl-name"), Is.EqualTo(new[] { "You", "LouisAgent.Api" }));
            Assert.That(In(shapes, "stl-dot").Select(dot => (string)dot.Attribute("class")!),
                Is.EqualTo(new[] { "stl-dot tone-blue", "stl-dot tone-violet" }));
            Assert.That(In(shapes, "seq-beam"), Is.Not.Empty);
            Assert.That(In(shapes, "seq-answer"), Is.Empty);
            Assert.That(In(shapes, "pic-march"), Is.Empty, "the beam stands still, as the key's sample does");
            Assert.That(Words(shapes, "stl-words"), Is.EqualTo(new[] { "POST /v1/sessions/{id}/messages" }));
            Assert.That(In(shapes, "stl-bar").Single().Name.LocalName, Is.EqualTo("rect"));
        });
    }

    /// <summary>An answer writes the wide drawing's dashes and no beam, its bar dashed and its words quiet.</summary>
    [Test]
    public void AnAnswer_WritesDashes_AndNoBeam()
    {
        var shapes = Drawn(Placed(new ReplyStep("api", "you", "server-sent events")));

        Assert.Multiple(() =>
        {
            Assert.That(Words(shapes, "stl-name"), Is.EqualTo(new[] { "LouisAgent.Api", "You" }));
            Assert.That(In(shapes, "seq-answer"), Has.Count.EqualTo(1));
            Assert.That(In(shapes, "seq-beam"), Is.Empty);
            Assert.That((string)In(shapes, "stl-bar").Single().Attribute("class")!, Is.EqualTo("stl-bar quiet"));
            Assert.That((string)In(shapes, "stl-words").Single().Attribute("class")!, Is.EqualTo("stl-words quiet"));
        });
    }

    /// <summary>A wrapped step writes its receiver on a second line, under its sender, after the line.</summary>
    [Test]
    public void AWrappedStep_WritesTheReceiverOnTheSecondLine()
    {
        var step = Placed(new MessageStep("api", "long", "hand it on"));
        var names = In(Drawn(step), "stl-name");
        double Y(XElement text) => (double)text.Attribute("y")!;

        Assert.Multiple(() =>
        {
            Assert.That(step.Ends!.Wrapped);
            Assert.That(Y(names[1]), Is.EqualTo(Y(names[0]) + StepListMeasures.Marquee.EndLine).Within(0.01));
            Assert.That(names[1].Value, Is.EqualTo("A participant whose name runs on and on"));
        });
    }

    /// <summary>A call to oneself writes one name and the wide drawing's turn after it.</summary>
    [Test]
    public void ACallToOneself_WritesOneName_AndATurn()
    {
        var shapes = Drawn(Placed(new MessageStep("api", "api", "checks every name")));

        Assert.Multiple(() =>
        {
            Assert.That(Words(shapes, "stl-name"), Is.EqualTo(new[] { "LouisAgent.Api" }));
            Assert.That(In(shapes, "seq-turn"), Has.Count.EqualTo(1));
            Assert.That(In(shapes, "seq-beam"), Is.Empty);
        });
    }

    /// <summary>A numbered step writes its number in its ring, before the sender's square.</summary>
    [Test]
    public void ANumberedStep_WritesItsNumber()
    {
        var shapes = Drawn(Placed(new MessageStep("you", "api", "a call"), numbered: true));
        var ring = In(shapes, "seq-ring").Single();
        var square = In(shapes, "stl-dot")[0];

        Assert.Multiple(() =>
        {
            Assert.That(Words(shapes, "seq-count"), Is.EqualTo(new[] { "1" }));
            Assert.That((double)ring.Attribute("cx")!, Is.LessThan((double)square.Attribute("x")!));
        });
    }

    /// <summary>A note writes its words on glass, and no name and no bar.</summary>
    [Test]
    public void ANote_WritesItsWordsOnGlass_AndNoName()
    {
        var shapes = Drawn(Placed(new NoteStep(["you", "api"], "every request priced in the ledger")));

        Assert.Multiple(() =>
        {
            Assert.That(In(shapes, "mrq-glass"), Has.Count.EqualTo(1));
            Assert.That(Words(shapes, "pic-text"), Is.EqualTo(new[] { "every request priced in the ledger" }));
            Assert.That(In(shapes, "stl-name"), Is.Empty);
            Assert.That(In(shapes, "stl-bar"), Is.Empty);
        });
    }
}
