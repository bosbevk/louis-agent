using LouisAgent.Diagrams.Flow.Keys;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Flow.Keys;

/// <summary>
/// A flow's key says how to read the picture (F134.8): an item for each tone and each shape the picture really
/// uses, and one for what a line's colour says, side by side across the drawing's width.
/// </summary>
public class FlowKeyLayoutTests
{
    /// <summary>A placed box in a tone and a shape. Where it stands makes no difference to the key.</summary>
    private static PlacedNode Box(DiagramTone tone, NodeShape shape = NodeShape.Box) =>
        new(new GraphNode($"{tone}{shape}", "A box", Tone: tone, Shape: shape), 0, 0, 170, 52);

    /// <summary>
    /// The key has an item for each tone a box wears, in the tones' own order however the boxes come, and none for
    /// a plain box, which wears no tone.
    /// </summary>
    [Test]
    public void EachToneUsed_HasItsItem_InTheTonesOrder()
    {
        PlacedNode[] nodes =
        [
            Box(DiagramTone.Red), Box(DiagramTone.Plain), Box(DiagramTone.Blue), Box(DiagramTone.Red)
        ];

        var tones = FlowKeyLayout.Of(nodes, 880).Items.Where(item => item.Kind == FlowKeyKind.Tone).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(tones.Select(item => item.Tone), Is.EqualTo(new[] { DiagramTone.Blue, DiagramTone.Red }));
            Assert.That(tones.Select(item => item.Words), Is.EqualTo(new[] { "starts it", "outside the process" }));
        });
    }

    /// <summary>A shape the picture uses has its item, with what the shape means. A plain box has none.</summary>
    [Test]
    public void EachShapeUsed_HasItsItem()
    {
        PlacedNode[] nodes = [Box(DiagramTone.Green), Box(DiagramTone.Plain, NodeShape.Decision)];

        var shapes = FlowKeyLayout.Of(nodes, 880).Items.Where(item => item.Kind == FlowKeyKind.Shape).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(shapes.Select(item => item.Shape), Is.EqualTo(new[] { NodeShape.Decision }));
            Assert.That(shapes[0].Words, Is.EqualTo("a decision"));
        });
    }

    /// <summary>
    /// An item says how wide its sample is, so its words start close after it: a tone's small badge is narrower
    /// than a shape or a line. The first row stands under the key's rule and its heading, in the room the other
    /// kinds' keys give theirs (F134.12).
    /// </summary>
    [Test]
    public void AnItem_SaysHowWideItsSampleIs_AndTheRowsStandUnderTheHeading()
    {
        PlacedNode[] nodes = [Box(DiagramTone.Blue), Box(DiagramTone.Plain, NodeShape.Decision)];

        var items = FlowKeyLayout.Of(nodes, 880).Items;

        Assert.Multiple(() =>
        {
            Assert.That(items[0].Sample, Is.EqualTo(18), "a tone's badge");
            Assert.That(items[1].Sample, Is.EqualTo(30), "a shape");
            Assert.That(items[^1].Sample, Is.EqualTo(30), "a line");
            Assert.That(items.Select(item => item.Y), Is.All.EqualTo(34), "under the rule and the heading");
            Assert.That(items[1].X, Is.GreaterThan(items[0].X + 18 + FlowKeyLayout.Words), "after the first's words");
        });
    }

    /// <summary>The last item says what a line's colour means, in every flow.</summary>
    [Test]
    public void TheLastItem_SaysWhatALinesColourMeans()
    {
        var last = FlowKeyLayout.Of([Box(DiagramTone.Green)], 880).Items[^1];

        Assert.Multiple(() =>
        {
            Assert.That(last.Kind, Is.EqualTo(FlowKeyKind.Line));
            Assert.That(last.Words, Is.EqualTo("a line has the colour of the box it leaves"));
        });
    }

    /// <summary>
    /// The items stand side by side on one row while the width holds them, and go on to a new row when it doesn't,
    /// so a narrow picture's key is never wider than the picture.
    /// </summary>
    [Test]
    public void TheItems_GoOnToANewRow_WhenTheWidthIsFull()
    {
        PlacedNode[] nodes = [Box(DiagramTone.Blue), Box(DiagramTone.Green), Box(DiagramTone.Teal)];

        var wide = FlowKeyLayout.Of(nodes, 1600);
        var narrow = FlowKeyLayout.Of(nodes, 300);

        Assert.Multiple(() =>
        {
            Assert.That(wide.Items.Select(item => item.Y).Distinct().Count(), Is.EqualTo(1), "one row when it fits");
            Assert.That(narrow.Items.Select(item => item.Y).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(narrow.Height, Is.GreaterThan(wide.Height), "more rows make a taller key");
            Assert.That(narrow.Items.Where(item => item.Y > narrow.Items[0].Y).Min(item => item.X), Is.Zero,
                "a new row starts at the left");
        });
    }
}
