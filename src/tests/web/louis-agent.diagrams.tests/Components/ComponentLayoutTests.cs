using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Components.Placed.Keys;
using LouisAgent.Diagrams.Engine.Components.Placed.Lines;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Components;

/// <summary>
/// F134.7: no place is written in a component diagram's JSON. The layout puts every box inside the one that holds
/// it, every pin on its box's top edge, and every neighbour beside the root, level with the box it talks to. It is
/// a pure function of the data, so it is tested with no browser.
/// </summary>
public class ComponentLayoutTests
{
    /// <summary>The shared sample, placed.</summary>
    private static PlacedComponents Placed() =>
        ComponentLayout.Of(DiagramReader.Read(ComponentSamples.Start).Components!);

    /// <summary>The box with this id, which the test fails without.</summary>
    private static PlacedBox Box(PlacedComponents placed, string id)
    {
        var box = placed.Boxes.FirstOrDefault(candidate => candidate.Box.Id == id);
        Assert.That(box, Is.Not.Null, $"the box {id} is placed");
        return box!;
    }

    /// <summary>The neighbour with this id, which the test fails without.</summary>
    private static PlacedNeighbour Neighbour(PlacedComponents placed, string id)
    {
        var neighbour = placed.Neighbours.FirstOrDefault(candidate => candidate.Neighbour.Id == id);
        Assert.That(neighbour, Is.Not.Null, $"the neighbour {id} is placed");
        return neighbour!;
    }

    /// <summary>
    /// A box inside a box is held by it: every held box lies within its holder, clear of its edges, and one level
    /// deeper.
    /// </summary>
    [TestCase("start", "stage")]
    [TestCase("stage", "deck")]
    [TestCase("stage", "dock")]
    [TestCase("deck", "window")]
    [TestCase("deck", "plain")]
    public void AHeldBox_LiesInsideItsHolder(string holderId, string heldId)
    {
        var placed = Placed();
        var holder = Box(placed, holderId);
        var held = Box(placed, heldId);

        Assert.Multiple(() =>
        {
            Assert.That(held.X, Is.GreaterThan(holder.X), "clear of the left edge");
            Assert.That(held.Right, Is.LessThan(holder.Right), "clear of the right edge");
            Assert.That(held.Y, Is.GreaterThan(holder.Y + 30), "under the holder's name");
            Assert.That(held.Bottom, Is.LessThan(holder.Bottom), "clear of the bottom edge");
            Assert.That(held.Depth, Is.EqualTo(holder.Depth + 1));
        });
    }

    /// <summary>
    /// The boxes one holder holds are stacked in the JSON's order, as wide as each other, with room between them
    /// for the pins on the lower one's edge.
    /// </summary>
    [TestCase("deck", "dock")]
    [TestCase("window", "plain")]
    public void BoxesOfOneHolder_AreStacked_WithRoomForPins(string upperId, string lowerId)
    {
        var placed = Placed();
        var upper = Box(placed, upperId);
        var lower = Box(placed, lowerId);

        Assert.Multiple(() =>
        {
            Assert.That(lower.Y, Is.GreaterThanOrEqualTo(upper.Bottom + 18), "room for a pin and a gap");
            Assert.That(lower.X, Is.EqualTo(upper.X));
            Assert.That(lower.Width, Is.EqualTo(upper.Width));
        });
    }

    /// <summary>
    /// A pin stands on its box's top edge, within the box's width. What is handed down comes before what is said
    /// back up, and no two pins of a box touch.
    /// </summary>
    [Test]
    public void APin_StandsOnItsBoxsTopEdge()
    {
        var placed = Placed();
        var deck = Box(placed, "deck");

        var pins = placed.Pins.Where(pin => pin.Box == "deck").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(pins.Select(pin => pin.Name), Is.EqualTo(new[] { "Items", "Forward", "ForwardChanged" }));
            Assert.That(pins.Select(pin => pin.Up), Is.EqualTo(new[] { false, false, true }));
            Assert.That(pins.Select(pin => pin.Y), Is.All.EqualTo(deck.Y));
            Assert.That(pins[0].X, Is.GreaterThan(deck.X));
            Assert.That(pins[^1].X + pins[^1].Width, Is.LessThan(deck.Right));
            Assert.That(pins.Zip(pins.Skip(1), (left, right) => right.X - (left.X + left.Width)),
                Is.All.GreaterThan(0), "a gap between pins");
            Assert.That(placed.Pins, Has.Count.EqualTo(9), "every pin of every box");
        });
    }

    /// <summary>
    /// The one thing a box holds is a pill inside it, under its name and above the boxes it holds. A box that
    /// holds nothing of its own has none.
    /// </summary>
    [Test]
    public void TheOneThingABoxHolds_IsAPillAboveWhatItHolds()
    {
        var placed = Placed();
        var start = Box(placed, "start");
        var stage = Box(placed, "stage");

        Assert.Multiple(() =>
        {
            Assert.That(start.Pill, Is.Not.Null);
            Assert.That(start.Pill?.Says, Is.EqualTo("which demo is forward"));
            Assert.That(start.Pill?.X, Is.GreaterThan(start.X));
            Assert.That(start.Pill?.X + start.Pill?.Width, Is.LessThan(start.Right));
            Assert.That(start.Pill?.Y, Is.GreaterThan(start.Y + 30), "under the name");
            Assert.That(start.Pill?.Y + start.Pill?.Height, Is.LessThan(stage.Y - 8), "above what it holds, pins too");
            Assert.That(stage.Pill, Is.Null);
        });
    }

    /// <summary>
    /// A neighbour stands in a column beside the root: before it on the left, after it on the right, clear of it.
    /// It is level with the box it talks to, and two in one column never touch.
    /// </summary>
    [Test]
    public void ANeighbour_StandsBesideTheRoot_LevelWithItsBox()
    {
        var placed = Placed();
        var start = Box(placed, "start");
        var deck = Box(placed, "deck");
        var page = Neighbour(placed, "page");
        var demos = Neighbour(placed, "demos");
        var script = Neighbour(placed, "script");

        Assert.Multiple(() =>
        {
            Assert.That(page.Right, Is.LessThan(start.X - 30), "before the root, with room for its lines");
            Assert.That(script.X, Is.GreaterThan(start.Right + 30), "after the root");
            Assert.That(page.Y, Is.InRange(start.Y, start.Y + 20), "level with the root, under its heading");
            Assert.That(start.Y, Is.GreaterThanOrEqualTo(ComponentLayout.TitleRoom), "under the picture's title");
            Assert.That(demos.Y, Is.GreaterThan(page.Bottom), "under the first in its column");
            Assert.That(script.Y, Is.InRange(deck.Y, deck.Bottom), "level with the deck it talks to");
            Assert.That(placed.Width, Is.GreaterThanOrEqualTo(script.Right));
            Assert.That(placed.Height, Is.GreaterThanOrEqualTo(Math.Max(start.Bottom, demos.Bottom)));
        });
    }

    /// <summary>
    /// The picture says how to read it (Louis, of the key in the design he picked: "where is this?"). The key
    /// stands under the column before the root, clear of it and of the root, with a row for each thing the picture
    /// really uses: its tones in their order, a pin each way, a read, the one thing held, and that a box inside a
    /// box is held by it.
    /// </summary>
    [Test]
    public void TheKey_StandsUnderTheColumnBeforeTheRoot_WithARowForWhatThePictureUses()
    {
        var placed = Placed();
        var key = placed.Key;
        var start = Box(placed, "start");

        Assert.Multiple(() =>
        {
            Assert.That(key.Rows.Select(row => row.Kind), Is.EqualTo(new[]
            {
                KeyRowKind.Tone, KeyRowKind.Tone, KeyRowKind.Tone, KeyRowKind.HandedDown, KeyRowKind.SaidBackUp,
                KeyRowKind.Reads, KeyRowKind.Holds, KeyRowKind.Sentence
            }));
            Assert.That(key.Rows.Take(3).Select(row => row.Tone),
                Is.EqualTo(new[] { DiagramTone.Blue, DiagramTone.Green, DiagramTone.Teal }));
            Assert.That(key.Rows.Take(3).Select(row => row.Words),
                Is.EqualTo(new[] { "starts it", "this project", "contracts and state" }));
            Assert.That(key.Y, Is.GreaterThan(Neighbour(placed, "demos").Bottom + 16), "under the column");
            Assert.That(key.X + key.Width, Is.LessThan(start.X), "clear of the root");
            Assert.That(placed.Height, Is.GreaterThanOrEqualTo(key.Bottom), "inside the picture");
        });
    }

    /// <summary>
    /// A picture with nothing before its root has its key under the root, and a row only for what it uses: no read
    /// and nothing held make no rows for them.
    /// </summary>
    [Test]
    public void WithNothingBeforeTheRoot_TheKeyStandsUnderIt_WithoutRowsForWhatIsNotThere()
    {
        const string alone = """
                             { "kind": "components", "title": "A dock",
                               "components": { "root": { "id": "dock", "name": "Dock", "tone": "green",
                                                         "in": [ "Items" ] } } }
                             """;

        var placed = ComponentLayout.Of(DiagramReader.Read(alone).Components!);

        Assert.Multiple(() =>
        {
            Assert.That(placed.Key.Rows.Select(row => row.Kind),
                Is.EqualTo(new[] { KeyRowKind.Tone, KeyRowKind.HandedDown }));
            Assert.That(placed.Key.Y, Is.GreaterThan(placed.Boxes[0].Bottom));
            Assert.That(placed.Width, Is.GreaterThanOrEqualTo(placed.Key.X + placed.Key.Width));
        });
    }

    /// <summary>
    /// A neighbour's lines run straight across to the box it talks to: one for each thing it gives, one for a
    /// service that is only read, and a call out with its answer back, which flow opposite ways.
    /// </summary>
    [Test]
    public void ANeighboursLines_RunStraightAcrossToItsBox()
    {
        var placed = Placed();
        var start = Box(placed, "start");
        var deck = Box(placed, "deck");
        var page = Neighbour(placed, "page");
        var script = Neighbour(placed, "script");

        var gives = placed.Lines.Where(line => line.Kind == LineKind.Gives).ToList();
        var calls = placed.Lines.Single(line => line.Kind == LineKind.Calls);
        var back = placed.Lines.Single(line => line.Kind == LineKind.Back);

        Assert.Multiple(() =>
        {
            Assert.That(gives.Select(line => line.Word), Is.EqualTo(new[] { "Featured", "Group" }));
            Assert.That(gives.Select(line => line.Points[0].X), Is.All.EqualTo(page.Right));
            Assert.That(gives.Select(line => line.Points[^1].X), Is.All.EqualTo(start.X));
            Assert.That(placed.Lines.Count(line => line.Kind == LineKind.Reads), Is.EqualTo(1));
            Assert.That(placed.Lines.Where(line => line.Kind is not (LineKind.Down or LineKind.Up))
                    .Select(line => line.Points[0].Y - line.Points[^1].Y), Is.All.EqualTo(0),
                "every line of a neighbour is level");
            Assert.That((calls.Points[0].X, calls.Points[^1].X), Is.EqualTo((deck.Right, script.X)), "out to it");
            Assert.That((back.Points[0].X, back.Points[^1].X), Is.EqualTo((script.X, deck.Right)), "and back");
            Assert.That((calls.Word, back.Word), Is.EqualTo(("calls", "Viewed")));
            Assert.That(back.Points[0].Y, Is.GreaterThan(calls.Points[0].Y), "the answer under the call");
        });
    }
}
