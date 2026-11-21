using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Nested;
using LouisAgent.Diagrams.Engine.Nested.Placed;
using LouisAgent.Diagrams.Engine.Nested.Placed.Lines;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Nested;

/// <summary>
/// F134.7: the one thing a box holds is joined to the pins that carry it, by a line down to each pin it is handed
/// down through and a line back up from each pin that changes it. The lines are routed by a rule, for any nesting:
/// straight when nothing is in the way, and otherwise down the left side of the boxes between, so no line crosses
/// a box it doesn't join or runs over another pin.
/// </summary>
public class StateRoutesTests
{
    /// <summary>The shared sample, placed.</summary>
    private static PlacedNested Placed() =>
        NestedLayout.Of(DiagramReader.Read(NestedSamples.Start).Nested!);

    /// <summary>The lines that carry a state, down and up: the sample's five, which every test here needs.</summary>
    private static List<PlacedLine> StateLines(PlacedNested placed)
    {
        List<PlacedLine> lines = [.. placed.Lines.Where(line => line.Kind is LineKind.Down or LineKind.Up)];
        Assert.That(lines, Has.Count.EqualTo(5), "a line for each pin the state names");
        return lines;
    }

    /// <summary>A line's stretches, each from one point to the next.</summary>
    private static IEnumerable<(IPoint From, IPoint To)> Stretches(PlacedLine line) =>
        line.Points.Zip(line.Points.Skip(1));

    /// <summary>Whether a level or upright stretch touches a box.</summary>
    private static bool Touches((IPoint From, IPoint To) stretch, IBox box) =>
        box.CrossedBy(stretch.From, stretch.To, 0);

    /// <summary>The pin a state line ends on or starts from: the one whose top its far end stands on.</summary>
    private static PlacedPin PinOf(PlacedNested placed, PlacedLine line)
    {
        var end = line.Kind == LineKind.Down ? line.Points[^1] : line.Points[0];
        return placed.Pins.Single(pin =>
            Math.Abs(pin.Box.Middle.X - end.X) < .5 && Math.Abs(pin.Box.Top - end.Y) < .5);
    }

    /// <summary>
    /// Each pin a state names has its line: three handed down, two said back up. A line down ends on its pin's
    /// top, in the pin's middle, and a line up starts there.
    /// </summary>
    [Test]
    public void EachPinAStateNames_HasItsLine()
    {
        var placed = Placed();

        var lines = StateLines(placed);
        var joined = lines.Select(line => (line.Kind, Pin: PinOf(placed, line))).ToList();
        var down = joined.Where(line => line.Kind == LineKind.Down).Select(line => $"{line.Pin.On}.{line.Pin.Name}");
        var up = joined.Where(line => line.Kind == LineKind.Up).Select(line => $"{line.Pin.On}.{line.Pin.Name}");

        Assert.Multiple(() =>
        {
            Assert.That(down, Is.EquivalentTo(new[] { "stage.Accent", "deck.Forward", "dock.Current" }));
            Assert.That(up, Is.EquivalentTo(new[] { "deck.ForwardChanged", "dock.Chosen" }));
            Assert.That(joined.Select(line => line.Pin.Up),
                Is.EqualTo(joined.Select(line => line.Kind == LineKind.Up)));
        });
    }

    /// <summary>
    /// A line down starts at the pill and a line up ends at it, and every stretch of every line is level or
    /// upright.
    /// </summary>
    [Test]
    public void EveryLine_JoinsThePill_InLevelAndUprightStretches()
    {
        var placed = Placed();
        var pill = placed.Boxes[0].Pill!;

        foreach (var line in StateLines(placed))
        {
            var atPill = line.Kind == LineKind.Down ? line.Points[0] : line.Points[^1];

            Assert.Multiple(() =>
            {
                Assert.That(atPill.X, Is.InRange(pill.Box.Left, pill.Box.Right), "on the pill, left to right");
                Assert.That(atPill.Y, Is.InRange(pill.Box.Top, pill.Box.Bottom), "on the pill, top to bottom");
                Assert.That(Stretches(line).Select(stretch =>
                        Math.Min(Math.Abs(stretch.From.X - stretch.To.X), Math.Abs(stretch.From.Y - stretch.To.Y))),
                    Is.All.EqualTo(0), "level or upright");
            });
        }
    }

    /// <summary>
    /// No line crosses a box it doesn't join: a stretch may only pass through the boxes that hold its pin's box.
    /// The dock stands under the deck, so its lines go round the deck, down the stage's left side.
    /// </summary>
    [Test]
    public void NoLine_CrossesABoxItDoesNotJoin()
    {
        var placed = Placed();
        var deck = placed.Boxes.Single(box => box.Component.Id == "deck");

        foreach (var line in StateLines(placed))
        {
            var pin = PinOf(placed, line);
            var own = placed.Boxes.Single(box => box.Component.Id == pin.On);
            var others = placed.Boxes.Where(box => !box.Box.Holds(own.Box));

            var crossed = others.Where(box => Stretches(line).Any(stretch => Touches(stretch, box.Box)))
                .Select(box => box.Component.Id);

            Assert.That(crossed, Is.Empty, $"the line of {pin.On}.{pin.Name}");
        }

        var toTheDock = StateLines(placed).Where(line => PinOf(placed, line).On == "dock");
        Assert.That(toTheDock.Select(line => line.Points.Min(point => point.X)), Is.All.LessThan(deck.Box.Left),
            "round the deck, at its left");
    }

    /// <summary>
    /// A line that goes round runs straight down one side, with no step on the way (Louis, of a line that stepped
    /// from the root's side into the stage's: "not straight"). It goes level from the pill to the side of the box
    /// that holds its pin's box, straight down that side, and level to its pin: five points, and no more.
    /// </summary>
    [TestCase("dock", "Current")]
    [TestCase("dock", "Chosen")]
    public void ALineThatGoesRound_RunsStraightDownOneSide(string box, string pin)
    {
        var placed = Placed();
        var stage = placed.Boxes.Single(one => one.Component.Id == "stage");
        var deck = placed.Boxes.Single(one => one.Component.Id == "deck");
        var pill = placed.Boxes[0].Pill!;

        var line = StateLines(placed).Single(one => PinOf(placed, one) is var joined &&
                                                    joined.On == box && joined.Name == pin);
        var upright = Stretches(line).Where(stretch => stretch.From.X == stretch.To.X)
            .OrderByDescending(stretch => Math.Abs(stretch.From.Y - stretch.To.Y)).First();

        Assert.Multiple(() =>
        {
            Assert.That(line.Points, Has.Count.EqualTo(5), "level, down, level, into the pin");
            Assert.That(upright.From.X, Is.InRange(stage.Box.Left, deck.Box.Left), "down the stage's own left side");
            Assert.That(Math.Min(upright.From.Y, upright.To.Y), Is.InRange(pill.Box.Top, pill.Box.Bottom),
                "from the pill's row, with no step before it");
        });
    }

    /// <summary>No line runs over a pin that isn't its own.</summary>
    [Test]
    public void NoLine_RunsOverAnotherPin()
    {
        var placed = Placed();

        foreach (var line in StateLines(placed))
        {
            var own = PinOf(placed, line);
            var over = placed.Pins.Where(pin => pin != own && Stretches(line).Any(stretch =>
                Touches(stretch, pin.Box))).Select(pin => $"{pin.On}.{pin.Name}");

            Assert.That(over, Is.Empty, $"the line of {own.On}.{own.Name}");
        }
    }

    /// <summary>A line wears the colour of the box that holds the thing it carries.</summary>
    [Test]
    public void ALine_WearsItsHoldersTone() =>
        Assert.That(StateLines(Placed()).Select(line => line.Tone), Is.All.EqualTo(Tones.DiagramTone.Green));
}
