using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence;
using LouisAgent.Diagrams.Engine.Sequence.Placed;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Tests.Sequence;

/// <summary>
/// A sequence laid out by design C's measures (F134.10): the same layout as ever, handed other numbers. A head is
/// a badge with a name under it and no box, words stand above their line, and a block's frame reaches left of
/// what it holds for its tab.
/// </summary>
public class SequenceMeasuresTests
{
    private static readonly SequenceMeasures Marquee = SequenceMeasures.Marquee;

    /// <summary>
    /// Three participants: a call across the middle one, a call a participant makes to itself, a block with two
    /// cases, a note and an answer.
    /// </summary>
    private static readonly SequenceSpec Spec = new(
        [
            new Participant("P", "Program.cs", DiagramIcon.Terminal),
            new Participant("S", "IServiceCollection", DiagramIcon.Box),
            new Participant("B", "LouisAgentBuilder", DiagramIcon.Settings)
        ],
        [
            new MessageStep("P", "S", "AddLouisAgent(agent => …)"),
            new MessageStep("P", "B",
                "the lambda: AddAnthropic(), AddWeb(), AddPack<CalculatorPack>(), AddIdentity<PulsIdentity>()"),
            new MessageStep("B", "B", "BuildCatalog() checks every name"),
            new BlockStep(BlockKind.Alt, "a name is unknown",
                [new ReplyStep("B", "P", "refused, with the name")],
                [new ElseCase("every name is known", [new NoteStep(["S", "B"], "the catalogue is built once")])]),
            new ReplyStep("B", "P", "AgentHostContext")
        ]);

    private static PlacedSequence Placed() => SequenceLayout.Place(Spec, Marquee);

    /// <summary>
    /// A head is as wide as its name needs and no narrower than its badge's room, with no room for an icon beside
    /// the name, since the icon stands over it. It is as tall as the badge and one line of name, and a line taller
    /// for each further line.
    /// </summary>
    [Test]
    public void AHead_IsABadgeAndAName_AsWideAsTheName()
    {
        var oneLetter = new Participant("a", "A", DiagramIcon.Box);
        var longName = new Participant("b", "UsageRecordingChatClient", DiagramIcon.Box);
        var two = new Participant("c", "FunctionInvokingChatClient and its guards", DiagramIcon.Box);

        Assert.Multiple(() =>
        {
            Assert.That(Marquee.HeadWidth(Marquee.HeadLines(oneLetter)), Is.EqualTo(64));
            Assert.That(Marquee.HeadWidth(Marquee.HeadLines(longName)), Is.EqualTo(24 * 9.4).Within(0.001));
            Assert.That(Marquee.HeadHeight(Marquee.HeadLines(oneLetter)), Is.EqualTo(84));
            Assert.That(Marquee.HeadLines(two), Has.Count.EqualTo(2));
            Assert.That(Marquee.HeadHeight(Marquee.HeadLines(two)), Is.EqualTo(104));
        });
    }

    /// <summary>The heads stand in a row, left to right, and no two touch.</summary>
    [Test]
    public void TheHeads_StandInARow_Apart()
    {
        var heads = Placed().Heads;

        Assert.That(heads.Zip(heads.Skip(1), (left, right) => right.Box.Left - left.Box.Right),
            Is.All.GreaterThan(0));
    }

    /// <summary>
    /// A message's words fit between the two lifelines it joins, and stand above its line: the line runs under the
    /// last of them.
    /// </summary>
    [Test]
    public void AMessagesWords_FitBetweenItsLifelines_AboveItsLine()
    {
        var rows = Placed().Rows.Where(row => row.Step is MessageStep or ReplyStep && !row.Self).ToList();

        Assert.Multiple(() =>
        {
            foreach (var row in rows)
            {
                var words = Marquee.WordBox(row, Geometries.InUse);
                Assert.That(words.Left, Is.GreaterThanOrEqualTo(Math.Min(row.FromX, row.ToX)), row.Lines[0]);
                Assert.That(words.Right, Is.LessThanOrEqualTo(Math.Max(row.FromX, row.ToX)), row.Lines[0]);
                Assert.That(words.Bottom, Is.LessThanOrEqualTo(row.ArrowY), row.Lines[0]);
            }
        });
    }

    /// <summary>
    /// A call a participant makes to itself keeps its words beside its turn and clear of the next lifeline, and
    /// its row is taller by the turn.
    /// </summary>
    [Test]
    public void ACallToOneself_KeepsItsWordsClearOfTheNextLifeline()
    {
        var placed = SequenceLayout.Place(Spec with
        {
            Participants = [.. Spec.Participants, new Participant("X", "Next")]
        }, Marquee);
        var self = placed.Rows.Single(row => row.Self);
        var box = Marquee.WordBox(self, Geometries.InUse);

        Assert.Multiple(() =>
        {
            Assert.That(box.Left, Is.EqualTo(self.FromX + Marquee.SelfOut + Marquee.WordPad));
            Assert.That(box.Right, Is.LessThan(placed.Heads[3].Center));
            Assert.That(self.H, Is.EqualTo(placed.Rows[0].H + Marquee.SelfDrop));
        });
    }

    /// <summary>
    /// A block's frame holds its rows and reaches further left of them than right, for its tab, and a case's
    /// divider stands between the rows of the two cases.
    /// </summary>
    [Test]
    public void ABlocksFrame_HoldsItsRows_AndReachesLeftForItsTab()
    {
        var placed = Placed();
        var block = placed.Blocks.Single();
        var inside = placed.Rows.Where(row => row.Y > block.Box.Top && row.Y < block.Box.Bottom).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(inside, Has.Count.EqualTo(2), "the answer and the note");
            Assert.That(block.Box.Left, Is.EqualTo(placed.Heads[0].Center - Marquee.InsetLeft));
            Assert.That(block.Box.Right, Is.GreaterThanOrEqualTo(placed.Heads[2].Center + Marquee.InsetRight));
            Assert.That(block.Cases.Single().Y, Is.InRange(inside[0].Y + inside[0].H, inside[1].Y));
            Assert.That(inside[0].Y, Is.EqualTo(block.Box.Top + Marquee.TabHeight(block.Lines)));
        });
    }

    /// <summary>
    /// A block's tab stands a little inside its frame, so a frame is as wide as that, its kind's pill, its label
    /// and the room after it need, and so is one for an else's label.
    /// </summary>
    [Test]
    public void AFrame_IsWideEnoughForItsTab_FromWhereTheTabStands()
    {
        var label = Marquee.Lines("every name is known");
        double words = Marquee.WordsWidth(label) + Marquee.TabPad;

        Assert.Multiple(() =>
        {
            Assert.That(Marquee.TabLeft, Is.EqualTo(14));
            Assert.That(Marquee.TabWidth(BlockKind.Alt, label),
                Is.EqualTo(14 + Marquee.KindWidth(BlockKind.Alt) + words));
            Assert.That(Marquee.ElseWidth(label),
                Is.EqualTo(14 + Marquee.WordsWidth([SequenceMeasures.ElseWord]) + words));
        });
    }

    /// <summary>
    /// What comes after a block starts a little under its frame, so a frame never touches a note, a divider or
    /// another frame under it.
    /// </summary>
    [Test]
    public void WhatComesAfterABlock_StartsALittleUnderItsFrame()
    {
        var placed = Placed();
        var block = placed.Blocks.Single();

        Assert.Multiple(() =>
        {
            Assert.That(Marquee.BlockGap, Is.EqualTo(12));
            Assert.That(placed.Rows[^1].Y, Is.EqualTo(block.Box.Bottom + 12), "the last answer, after the block");
        });
    }

    /// <summary>A note spans the lifelines it names and a reach each side, and is as tall as its lines need.</summary>
    [Test]
    public void ANote_SpansItsLifelines()
    {
        var placed = Placed();
        var note = placed.Rows.Single(row => row.Step is NoteStep);
        var panel = Marquee.NoteBox(note, Geometries.InUse);

        Assert.Multiple(() =>
        {
            Assert.That(panel.Left, Is.LessThanOrEqualTo(placed.Heads[1].Center - Marquee.NoteReach));
            Assert.That(panel.Right, Is.GreaterThanOrEqualTo(placed.Heads[2].Center + Marquee.NoteReach));
            Assert.That(panel.Height, Is.EqualTo(34), "one line: 16 and 9 above and under");
        });
    }

    /// <summary>
    /// Nothing reaches left of the drawing's start or right of its width, though a block's frame reaches left of
    /// the first lifeline: the lifelines move right to make room.
    /// </summary>
    [Test]
    public void Nothing_LeavesTheDrawing()
    {
        var placed = Placed();

        Assert.Multiple(() =>
        {
            Assert.That(placed.Blocks.Min(block => block.Box.Left), Is.GreaterThanOrEqualTo(0));
            Assert.That(placed.Heads[0].Box.Left, Is.GreaterThanOrEqualTo(0));
            Assert.That(placed.Blocks.Max(block => block.Box.Right), Is.LessThanOrEqualTo(placed.Width + 0.001));
        });
    }

    /// <summary>
    /// The one look there is holds the record's own numbers, so a look that needs others says only which ones
    /// differ.
    /// </summary>
    [Test]
    public void TheOneLook_HoldsTheRecordsOwnNumbers() =>
        Assert.That(SequenceMeasures.Marquee, Is.EqualTo(new SequenceMeasures()));
}
