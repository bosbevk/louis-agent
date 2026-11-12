using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Sequence;

/// <summary>
/// A sequence's blocks (CL60.3): a <c>loop</c>, an <c>opt</c> or an <c>alt</c> is a step holding steps, drawn as a
/// frame round them and the lifelines they touch, an <c>alt</c>'s other cases under dividers, a nested one inside.
/// </summary>
public class SequenceBlocksTests
{
    /// <summary>The engine and a tool: up to ten rounds, each a call that works or fails, then an answer.</summary>
    private static readonly SequenceSpec Spec = new(
        [new Participant("E", "the engine"), new Participant("T", "a tool"), new Participant("M", "the model")],
        [
            new MessageStep("E", "M", "the request"),
            new BlockStep(BlockKind.Loop, "up to 10 tool rounds",
            [
                new MessageStep("E", "T", "call"),
                new BlockStep(BlockKind.Alt, "it works", [new ReplyStep("T", "E", "its result")],
                    [new ElseCase("it fails", [new NoteStep(["T"], "the failure carries its reason")])])
            ]),
            new ReplyStep("M", "E", "the answer")
        ],
        Numbered: true);

    /// <summary>A frame holds every row inside it, and reaches past every lifeline those rows touch.</summary>
    [Test]
    public void AFrame_HoldsEveryStepInside()
    {
        var placed = SequenceLayout.Place(Spec);
        var loop = placed.Blocks.Single(block => block.Block.Block == BlockKind.Loop);
        var inside = placed.Rows.Skip(1).Take(3).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(loop.X, Is.LessThan(placed.Heads[0].Center));
            Assert.That(loop.X + loop.W, Is.GreaterThan(placed.Heads[1].Center));
            Assert.That(loop.Y, Is.LessThan(inside[0].Y));
            Assert.That(loop.Y + loop.H, Is.GreaterThanOrEqualTo(inside[^1].Y + inside[^1].H));
            Assert.That(placed.Rows[^1].Y, Is.GreaterThanOrEqualTo(loop.Y + loop.H));
        });
    }

    /// <summary>A block inside another stands inside its frame, every side.</summary>
    [Test]
    public void ANestedFrame_StandsInsideItsParent()
    {
        var placed = SequenceLayout.Place(Spec);
        var loop = placed.Blocks.Single(block => block.Block.Block == BlockKind.Loop);
        var alt = placed.Blocks.Single(block => block.Block.Block == BlockKind.Alt);

        Assert.Multiple(() =>
        {
            Assert.That(alt.Depth, Is.EqualTo(loop.Depth + 1));
            Assert.That(alt.X, Is.GreaterThan(loop.X));
            Assert.That(alt.X + alt.W, Is.LessThan(loop.X + loop.W));
            Assert.That(alt.Y, Is.GreaterThan(loop.Y));
            Assert.That(alt.Y + alt.H, Is.LessThan(loop.Y + loop.H));
        });
    }

    /// <summary>An else divides an alt's cases: under the first case's rows, over the next case's.</summary>
    [Test]
    public void AnElse_DividesTheCases()
    {
        var placed = SequenceLayout.Place(Spec);
        var alt = placed.Blocks.Single(block => block.Block.Block == BlockKind.Alt);
        var reply = placed.Rows[2];
        var note = placed.Rows[3];

        Assert.Multiple(() =>
        {
            Assert.That(alt.Cases.Select(@case => @case.Label), Is.EqualTo(new[] { "it fails" }));
            Assert.That(alt.Cases[0].Y, Is.GreaterThanOrEqualTo(reply.Y + reply.H));
            Assert.That(alt.Cases[0].Y, Is.LessThan(note.Y));
        });
    }

    /// <summary>A numbered chart counts on through its blocks, in the order the steps happen.</summary>
    [Test]
    public void TheNumbers_RunOnThroughBlocks()
    {
        var placed = SequenceLayout.Place(Spec);

        Assert.That(placed.Rows.Select(row => row.Number), Is.EqualTo(new int?[] { 1, 2, 3, null, 4 }));
    }

    /// <summary>A frame round the first lifeline alone still starts inside the drawing.</summary>
    [Test]
    public void AFrame_StaysInsideTheDrawing()
    {
        var spec = Spec with
        {
            Steps = [new BlockStep(BlockKind.Opt, "a name nobody knows", [new MessageStep("E", "E", "refuse it")])]
        };

        var frame = SequenceLayout.Place(spec).Blocks.Single();

        Assert.That(frame.X, Is.GreaterThanOrEqualTo(0));
    }

    /// <summary>A block reads from its file with its cases, and one naming a stranger inside is refused.</summary>
    [Test]
    public void ABlock_ReadsWithItsCases_AndAStrangerInsideIsRefused()
    {
        string json = DiagramWriter.Write(new DiagramSource(DiagramKind.Sequence, "Rounds", Sequence: Spec));
        string stranger = json.Replace("\"the failure carries its reason\"", "\"x\"", StringComparison.Ordinal)
            .Replace("\"T\"\n", "\"ghost\"\n", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            var loop = (BlockStep)DiagramReader.Read(json).Sequence!.Steps[1];
            Assert.That(((BlockStep)loop.Steps[1]).Else!.Single().Label, Is.EqualTo("it fails"));
            Assert.That(json, Does.Contain("\"block\": \"alt\""));
            // A loop is the first kind, but written all the same, so a person reading the file sees what it is.
            Assert.That(json, Does.Contain("\"block\": \"loop\""));
            Assert.That(() => DiagramReader.Read(stranger),
                Throws.TypeOf<InvalidDataException>().With.Message.Contains("'ghost'"));
        });
    }
}
