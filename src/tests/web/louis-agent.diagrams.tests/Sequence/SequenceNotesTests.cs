using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Sequence;

/// <summary>
/// A sequence's notes and numbers (CL60.3): a note is a panel over the lifelines it names, and a numbered chart puts
/// 1, 2, 3 before its messages, as Mermaid's <c>autonumber</c> does.
/// </summary>
public class SequenceNotesTests
{
    /// <summary>A host, a collector and a page: a note over the first alone, one over two, and a reply.</summary>
    private static readonly SequenceSpec Spec = new(
        [new Participant("H", "the host"), new Participant("C", "the collector"), new Participant("W", "the page")],
        [
            new NoteStep(["H"], "a turn runs: TurnSpans.Start, the model's and tools' spans, every one of them"),
            new MessageStep("H", "C", "spans, over OTLP"),
            new NoteStep(["H", "C"], "the run tag rides along"),
            new ReplyStep("C", "H", "ok")
        ],
        Numbered: true);

    /// <summary>A note over two lifelines reaches past both.</summary>
    [Test]
    public void ANote_SpansTheLifelinesItNames()
    {
        var placed = SequenceLayout.Place(Spec);
        var (x, _, w, _) = SequenceText.NoteBox(placed.Rows[2]);

        Assert.Multiple(() =>
        {
            Assert.That(x, Is.LessThan(placed.Heads[0].Center));
            Assert.That(x + w, Is.GreaterThan(placed.Heads[1].Center));
        });
    }

    /// <summary>A long note over the first lifeline stands on it, and the drawing grows to hold all of it.</summary>
    [Test]
    public void ANoteOverOne_StandsOnItsLifeline_InsideTheDrawing()
    {
        var placed = SequenceLayout.Place(Spec);
        var (x, _, w, _) = SequenceText.NoteBox(placed.Rows[0]);

        Assert.Multiple(() =>
        {
            Assert.That(x + w / 2, Is.EqualTo(placed.Heads[0].Center).Within(0.001));
            Assert.That(x, Is.GreaterThanOrEqualTo(0));
            Assert.That(x + w, Is.LessThanOrEqualTo(placed.Width));
        });
    }

    /// <summary>A numbered chart numbers its messages and replies in order, and never a note.</summary>
    [Test]
    public void TheNumbers_RunInOrder_OverMessagesOnly()
    {
        var placed = SequenceLayout.Place(Spec);

        Assert.That(placed.Rows.Select(row => row.Number), Is.EqualTo(new int?[] { null, 1, null, 2 }));
    }

    /// <summary>A note over someone the chart doesn't have is refused, naming them.</summary>
    [Test]
    public void ANote_OverNoParticipant_IsRefused()
    {
        var source = new DiagramSource(DiagramKind.Sequence, "Notes",
            Sequence: Spec with { Steps = [new NoteStep(["H", "ghost"], "who?")] });

        Assert.That(() => DiagramReader.Read(DiagramWriter.Write(source)),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'ghost'"));
    }
}
