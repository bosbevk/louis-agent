using LouisAgent.Diagrams.Engine.Sequence.Listed;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Tests.Sequence.Listed;

/// <summary>
/// A sequence's blocks in a list of steps (F169.7): a frame round its steps, which stand a level in, an alt's other
/// case under its divider, a block inside a block inside its parent, and what follows under the frame.
/// </summary>
public class StepListBlocksTests
{
    /// <summary>The width a phone's picture is laid out at.</summary>
    private const double Width = 338;

    private static readonly StepListMeasures Measures = StepListMeasures.Marquee;

    /// <summary>
    /// The engine and a tool: a call, then up to ten rounds that call the tool, an alt inside them with its other
    /// case, and the answer after.
    /// </summary>
    private static readonly SequenceSpec Spec = new(
        [new Participant("E", "the engine"), new Participant("T", "a tool"), new Participant("M", "the model")],
        [
            new MessageStep("E", "M", "the request"),
            new BlockStep(BlockKind.Loop, "up to 10 tool rounds, each one a call the model asks for by name",
            [
                new MessageStep("E", "T", "call"),
                new BlockStep(BlockKind.Alt, "it works", [new ReplyStep("T", "E", "its result")],
                    [new ElseCase("it fails", [new NoteStep(["T"], "the failure carries its reason")])])
            ]),
            new ReplyStep("M", "E", "the answer")
        ],
        Numbered: true);

    private static readonly PlacedStepList Placed = StepListLayout.Place(Spec, Width, Measures);

    /// <summary>The loop's frame, the outer one.</summary>
    private static PlacedBlock Loop => Placed.Blocks.Single(block => block.Block.Block == BlockKind.Loop);

    /// <summary>The alt's frame, inside the loop.</summary>
    private static PlacedBlock Alt => Placed.Blocks.Single(block => block.Block.Block == BlockKind.Alt);

    /// <summary>A block's frame holds every step inside it, in every case, and the steps run in order.</summary>
    [Test]
    public void AFrame_HoldsEveryStepInside()
    {
        var inside = Placed.Steps.Skip(1).Take(3).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Placed.Steps, Has.Count.EqualTo(5));
            Assert.That(Placed.Blocks, Has.Count.EqualTo(2));
            foreach (var step in inside) Assert.That(Loop.Box.Holds(step.Box), step.Lines[0]);
            Assert.That(Placed.Steps.Select(step => step.Number), Is.EqualTo(new int?[] { 1, 2, 3, null, 4 }));
        });
    }

    /// <summary>The steps inside a frame start a level in from its left, and end a level in from its right.</summary>
    [Test]
    public void TheStepsInside_StandALevelIn()
    {
        var call = Placed.Steps[1];

        Assert.Multiple(() =>
        {
            Assert.That(call.Box.Left, Is.EqualTo(Loop.Box.Left + Measures.Inset).Within(0.01));
            Assert.That(call.Box.Right, Is.EqualTo(Loop.Box.Right - Measures.Inset).Within(0.01));
            Assert.That(Loop.Box.Left, Is.Zero);
            Assert.That(Loop.Box.Width, Is.EqualTo(Width).Within(0.01));
        });
    }

    /// <summary>A block inside a block is inside its parent's frame, one level deeper.</summary>
    [Test]
    public void ABlockInsideABlock_IsInsideItsParent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Loop.Depth, Is.Zero);
            Assert.That(Alt.Depth, Is.EqualTo(1));
            Assert.That(Loop.Box.Holds(Alt.Box));
            Assert.That(Placed.Blocks[0], Is.SameAs(Alt), "a frame comes after the frames inside it");
        });
    }

    /// <summary>A label too long for the room beside its kind's pill wraps, and none of its lines is wider.</summary>
    [Test]
    public void ALongLabel_Wraps()
    {
        double room = Loop.Box.Width - Measures.Frames.TabWidth(BlockKind.Loop, []);

        Assert.Multiple(() =>
        {
            Assert.That(Loop.Lines, Has.Count.GreaterThan(1));
            Assert.That(Loop.Lines.Select(Measures.WordFace.Width), Has.All.LessThanOrEqualTo(room));
            Assert.That(Alt.Lines, Is.EqualTo(new[] { "it works" }));
            Assert.That(Placed.Steps[1].Box.Top,
                Is.EqualTo(Loop.Box.Top + Measures.Frames.TabHeight(Loop.Lines)).Within(0.01));
        });
    }

    /// <summary>An alt's other case has its divider between the first case's steps and its own.</summary>
    [Test]
    public void AnOtherCase_HasItsDividerBetweenTheCases()
    {
        var other = Alt.Cases.Single();
        var (result, note) = (Placed.Steps[2], Placed.Steps[3]);

        Assert.Multiple(() =>
        {
            Assert.That(other.Lines, Is.EqualTo(new[] { "it fails" }));
            Assert.That(other.Y, Is.GreaterThan(result.Box.Bottom));
            Assert.That(note.Box.Top, Is.GreaterThanOrEqualTo(other.Y + Measures.Frames.TabHeight(other.Lines)));
            Assert.That(note.Ends, Is.Null);
        });
    }

    /// <summary>What follows a block starts under its frame, by the gap.</summary>
    [Test]
    public void WhatFollows_StartsUnderTheFrame()
    {
        var answer = Placed.Steps[^1];

        Assert.Multiple(() =>
        {
            Assert.That(answer.Box.Top, Is.EqualTo(Loop.Box.Bottom + Measures.StepGap).Within(0.01));
            Assert.That(Alt.Box.Bottom, Is.LessThanOrEqualTo(Loop.Box.Bottom - Measures.BlockEnd));
            Assert.That(Placed.Height, Is.EqualTo(answer.Box.Bottom).Within(0.01));
        });
    }
}
