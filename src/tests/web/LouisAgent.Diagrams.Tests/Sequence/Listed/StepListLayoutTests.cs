using LouisAgent.Diagrams.Engine.Sequence.Listed;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Tests.Sequence.Listed;

/// <summary>
/// A sequence laid out as steps down a phone's width (F169.7): each step under the one before, none wider than the
/// width, its words wrapped to its room, and its two ends on one line or two.
/// </summary>
public class StepListLayoutTests
{
    /// <summary>The width a phone's picture is laid out at.</summary>
    private const double Width = 338;

    private static readonly StepListMeasures Measures = StepListMeasures.Marquee;

    /// <summary>A person, the API, and a part with a long name, which calls itself and answers back.</summary>
    private static readonly SequenceSpec Spec = new(
        [
            new Participant("you", "You", Actor: true),
            new Participant("api", "LouisAgent.Api"),
            new Participant("long", "A participant whose name runs on and on")
        ],
        [
            new MessageStep("you", "api", "POST /v1/sessions/{id}/messages"),
            new MessageStep("api", "long",
                "a long call whose words run well past one line on a phone, so they have to wrap down the page"),
            new MessageStep("long", "long", "checks every name"),
            new NoteStep(["api", "long"], "every request priced in the ledger"),
            new ReplyStep("long", "you", "the answer")
        ]);

    /// <summary>Each step's box starts under the one before it, by the measures' gap.</summary>
    [Test]
    public void EachStep_StartsUnderTheOneBefore()
    {
        var steps = StepListLayout.Place(Spec, Width, Measures).Steps;

        Assert.Multiple(() =>
        {
            Assert.That(steps, Has.Count.EqualTo(Spec.Steps.Count));
            Assert.That(steps[0].Box.Top, Is.Zero);
            for (int at = 1; at < steps.Count; at++)
                Assert.That(steps[at].Box.Top, Is.EqualTo(steps[at - 1].Box.Bottom + Measures.StepGap).Within(0.01));
        });
    }

    /// <summary>No step is wider than the width asked for, and the list is as tall as its last step reaches.</summary>
    [Test]
    public void NoStep_IsWiderThanTheWidth()
    {
        var placed = StepListLayout.Place(Spec, Width, Measures);

        Assert.Multiple(() =>
        {
            Assert.That(placed.Width, Is.EqualTo(Width));
            Assert.That(placed.Steps.Select(step => step.Box.Right), Has.All.LessThanOrEqualTo(Width));
            Assert.That(placed.Height, Is.EqualTo(placed.Steps[^1].Box.Bottom).Within(0.01));
        });
    }

    /// <summary>A step's words are broken so no line is wider than the room the bar leaves them.</summary>
    [Test]
    public void AStepsWords_WrapToTheirRoom()
    {
        var placed = StepListLayout.Place(Spec, Width, Measures);
        var call = placed.Steps[1];

        Assert.Multiple(() =>
        {
            Assert.That(call.Lines, Has.Count.GreaterThan(1));
            Assert.That(call.Lines.Select(Measures.WordFace.Width), Has.All.LessThanOrEqualTo(Width - Measures.Rail));
            Assert.That(string.Join(' ', call.Lines), Does.EndWith("wrap down the page"));
        });
    }

    /// <summary>Two short names fit one line with the line between, and two long ones put the receiver on a second.</summary>
    [Test]
    public void TwoLongNames_AreWrapped_AndTwoShortOnesAreNot()
    {
        var steps = StepListLayout.Place(Spec, Width, Measures).Steps;

        Assert.Multiple(() =>
        {
            Assert.That(steps[0].Ends!.Wrapped, Is.False);
            Assert.That(steps[0].Ends!.From.Id, Is.EqualTo("you"));
            Assert.That(steps[0].Ends!.To.Id, Is.EqualTo("api"));
            Assert.That(steps[1].Ends!.Wrapped, Is.True);
            Assert.That(steps[1].Box.Height, Is.GreaterThan(steps[0].Box.Height));
        });
    }

    /// <summary>A call a participant makes to itself has one name and is never wrapped.</summary>
    [Test]
    public void ACallToOneself_IsSelf()
    {
        var self = StepListLayout.Place(Spec, Width, Measures).Steps[2];

        Assert.Multiple(() =>
        {
            Assert.That(self.Ends!.Self, Is.True);
            Assert.That(self.Ends!.Wrapped, Is.False);
        });
    }

    /// <summary>A note has no ends, and its glass is as wide as its words and no wider than the width.</summary>
    [Test]
    public void ANote_HasNoEnds()
    {
        var note = StepListLayout.Place(Spec, Width, Measures).Steps[3];

        Assert.Multiple(() =>
        {
            Assert.That(note.Ends, Is.Null);
            Assert.That(note.Number, Is.Null);
            Assert.That(note.Box.Width, Is.LessThan(Width));
            Assert.That(note.Lines, Is.EqualTo(new[] { "every request priced in the ledger" }));
        });
    }

    /// <summary>The numbers run 1, 2, 3 over the calls and answers when the data asks, and a note has none.</summary>
    [Test]
    public void TheNumbers_RunOverCallsAndAnswers_OnlyWhenAsked()
    {
        var numbered = StepListLayout.Place(Spec with { Numbered = true }, Width, Measures).Steps;
        var plain = StepListLayout.Place(Spec, Width, Measures).Steps;

        Assert.Multiple(() =>
        {
            Assert.That(numbered.Select(step => step.Number), Is.EqualTo(new int?[] { 1, 2, 3, null, 4 }));
            Assert.That(plain.Select(step => step.Number), Has.All.Null);
        });
    }
}
