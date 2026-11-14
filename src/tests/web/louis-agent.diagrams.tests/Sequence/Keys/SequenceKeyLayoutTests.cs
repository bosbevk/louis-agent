using LouisAgent.Diagrams.Engine.Sequence.Keys;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Sequence.Keys;

/// <summary>
/// A sequence's key (F134.10): "how to read it", then an item for each tone its badges wear, a call, an answer, a
/// note and each kind of block, only where the picture has one, side by side and on to a new row when one is full.
/// </summary>
public class SequenceKeyLayoutTests
{
    /// <summary>Two participants, in blue and green, with one call and its answer.</summary>
    private static readonly SequenceSpec Plain = new(
        [new Participant("a", "A", Tone: DiagramTone.Blue), new Participant("b", "B", Tone: DiagramTone.Green)],
        [new MessageStep("a", "b", "ask"), new ReplyStep("b", "a", "the answer")]);

    /// <summary>The same with a note, a loop that holds an alt, and a participant with no tone.</summary>
    private static readonly SequenceSpec Full = Plain with
    {
        Participants = [.. Plain.Participants, new Participant("c", "C")],
        Steps =
        [
            .. Plain.Steps, new NoteStep(["a"], "once"),
            new BlockStep(BlockKind.Loop, "each item",
                [new BlockStep(BlockKind.Alt, "it is new", [new MessageStep("a", "b", "add")])])
        ]
    };

    private static List<(SequenceKeyKind Kind, string Words)> Items(SequenceSpec spec, double width = 1200) =>
        [.. SequenceKeyLayout.Of(spec, width).Items.Select(item => (item.Kind, item.Words))];

    /// <summary>
    /// A picture with calls and answers alone has an item for each tone, a call and an answer, and none for a note
    /// or a block it doesn't have.
    /// </summary>
    [Test]
    public void TheKey_HasAnItemOnlyForWhatThePictureHas() => Assert.That(Items(Plain), Is.EqualTo(new[]
    {
        (SequenceKeyKind.Tone, "starts it"), (SequenceKeyKind.Tone, "this project"),
        (SequenceKeyKind.Call, "a call"), (SequenceKeyKind.Answer, "an answer")
    }));

    /// <summary>
    /// A note and each kind of block the picture has get an item, a block's by what it does, in the kinds' own
    /// order, however deep it stands. A participant with no tone gets none.
    /// </summary>
    [Test]
    public void ANote_AndEachKindOfBlock_GetAnItem()
    {
        var items = Items(Full);

        Assert.Multiple(() =>
        {
            Assert.That(items.Skip(4), Is.EqualTo(new[]
            {
                (SequenceKeyKind.Note, "a note"), (SequenceKeyKind.Block, "repeats"),
                (SequenceKeyKind.Block, "one case or the other")
            }));
            Assert.That(items.Count(item => item.Kind == SequenceKeyKind.Tone), Is.EqualTo(2));
        });
    }

    /// <summary>A block's item carries its kind, which its sample shows in a pill, and a tone's its tone.</summary>
    [Test]
    public void AnItem_CarriesWhatItsSampleShows()
    {
        var items = SequenceKeyLayout.Of(Full, 1200).Items;

        Assert.Multiple(() =>
        {
            Assert.That(items.Where(item => item.Kind == SequenceKeyKind.Block).Select(item => item.Block),
                Is.EqualTo(new BlockKind?[] { BlockKind.Loop, BlockKind.Alt }));
            Assert.That(items.Where(item => item.Kind == SequenceKeyKind.Tone).Select(item => item.Tone),
                Is.EqualTo(new[] { DiagramTone.Blue, DiagramTone.Green }));
            Assert.That(items.Select(item => item.Sample), Is.All.GreaterThan(0));
        });
    }

    /// <summary>
    /// The items stand side by side under the heading, each after the one before with room for its words, and
    /// the key is as tall as its heading and one row.
    /// </summary>
    [Test]
    public void TheItems_StandSideBySide_UnderTheHeading()
    {
        var (items, height) = SequenceKeyLayout.Of(Plain, 1200);

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.Y).Distinct(), Is.EqualTo(new[] { SequenceKeyLayout.Heading }));
            Assert.That(items[0].X, Is.Zero);
            Assert.That(items.Zip(items.Skip(1), (left, right) => right.X - left.X - left.Sample),
                Is.All.GreaterThan(SequenceKeyLayout.Words));
            Assert.That(height, Is.EqualTo(SequenceKeyLayout.Heading + SequenceKeyLayout.Row));
        });
    }

    /// <summary>In a narrow picture an item that doesn't fit its row starts the next, at the left.</summary>
    [Test]
    public void InANarrowPicture_TheItemsWrap()
    {
        var (items, height) = SequenceKeyLayout.Of(Full, 360);
        var rows = items.GroupBy(item => item.Y).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.GreaterThan(1));
            Assert.That(rows.Select(row => row.First().X), Is.All.Zero);
            Assert.That(height, Is.EqualTo(SequenceKeyLayout.Heading + rows.Count * SequenceKeyLayout.Row));
        });
    }
}
