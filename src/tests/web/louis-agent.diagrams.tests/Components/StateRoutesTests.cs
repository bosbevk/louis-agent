using LouisAgent.Diagrams.Engine.Components;
using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Components.Placed.Lines;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Components;

/// <summary>
/// F134.7: the one thing a box holds is joined to the pins that carry it, by a line down to each pin it is handed
/// down through and a line back up from each pin that changes it. The lines are routed by a rule, for any nesting:
/// straight when nothing is in the way, and otherwise down the left side of the boxes between, so no line crosses
/// a box it doesn't join or runs over another pin.
/// </summary>
public class StateRoutesTests
{
    /// <summary>The shared sample, placed.</summary>
    private static PlacedComponents Placed() =>
        ComponentLayout.Of(DiagramReader.Read(ComponentSamples.Start).Components!);

    /// <summary>The lines that carry a state, down and up: the sample's five, which every test here needs.</summary>
    private static List<PlacedLine> StateLines(PlacedComponents placed)
    {
        List<PlacedLine> lines = [.. placed.Lines.Where(line => line.Kind is LineKind.Down or LineKind.Up)];
        Assert.That(lines, Has.Count.EqualTo(5), "a line for each pin the state names");
        return lines;
    }

    /// <summary>A line's stretches, each from one point to the next.</summary>
    private static IEnumerable<(LinePoint From, LinePoint To)> Stretches(PlacedLine line) =>
        line.Points.Zip(line.Points.Skip(1));

    /// <summary>Whether a level or upright stretch touches a rectangle.</summary>
    private static bool Touches((LinePoint From, LinePoint To) stretch, double x, double y, double width,
        double height)
    {
        double left = Math.Min(stretch.From.X, stretch.To.X), right = Math.Max(stretch.From.X, stretch.To.X);
        double top = Math.Min(stretch.From.Y, stretch.To.Y), bottom = Math.Max(stretch.From.Y, stretch.To.Y);
        return left < x + width && right > x && top < y + height && bottom > y;
    }

    /// <summary>The pin a state line ends on or starts from: the one whose top its far end stands on.</summary>
    private static PlacedPin PinOf(PlacedComponents placed, PlacedLine line)
    {
        var end = line.Kind == LineKind.Down ? line.Points[^1] : line.Points[0];
        return placed.Pins.Single(pin => Math.Abs(pin.Middle - end.X) < .5 && Math.Abs(pin.Y - 9 - end.Y) < .5);
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
        var down = joined.Where(line => line.Kind == LineKind.Down).Select(line => $"{line.Pin.Box}.{line.Pin.Name}");
        var up = joined.Where(line => line.Kind == LineKind.Up).Select(line => $"{line.Pin.Box}.{line.Pin.Name}");

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
                Assert.That(atPill.X, Is.InRange(pill.X, pill.X + pill.Width), "on the pill, left to right");
                Assert.That(atPill.Y, Is.InRange(pill.Y, pill.Y + pill.Height), "on the pill, top to bottom");
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
        var deck = placed.Boxes.Single(box => box.Box.Id == "deck");

        foreach (var line in StateLines(placed))
        {
            var pin = PinOf(placed, line);
            var own = placed.Boxes.Single(box => box.Box.Id == pin.Box);
            var others = placed.Boxes.Where(box => !(box.X <= own.X && box.Right >= own.Right &&
                                                     box.Y <= own.Y && box.Bottom >= own.Bottom));

            var crossed = others.Where(box => Stretches(line).Any(stretch =>
                Touches(stretch, box.X, box.Y, box.Width, box.Height))).Select(box => box.Box.Id);

            Assert.That(crossed, Is.Empty, $"the line of {pin.Box}.{pin.Name}");
        }

        var toTheDock = StateLines(placed).Where(line => PinOf(placed, line).Box == "dock");
        Assert.That(toTheDock.Select(line => line.Points.Min(point => point.X)), Is.All.LessThan(deck.X),
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
        var stage = placed.Boxes.Single(one => one.Box.Id == "stage");
        var deck = placed.Boxes.Single(one => one.Box.Id == "deck");
        var pill = placed.Boxes[0].Pill!;

        var line = StateLines(placed).Single(one => PinOf(placed, one) is var joined &&
                                                    joined.Box == box && joined.Name == pin);
        var upright = Stretches(line).Where(stretch => stretch.From.X == stretch.To.X)
            .OrderByDescending(stretch => Math.Abs(stretch.From.Y - stretch.To.Y)).First();

        Assert.Multiple(() =>
        {
            Assert.That(line.Points, Has.Count.EqualTo(5), "level, down, level, into the pin");
            Assert.That(upright.From.X, Is.InRange(stage.X, deck.X), "down the stage's own left side");
            Assert.That(Math.Min(upright.From.Y, upright.To.Y), Is.InRange(pill.Y, pill.Y + pill.Height),
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
                Touches(stretch, pin.X, pin.Y - 9, pin.Width, 18))).Select(pin => $"{pin.Box}.{pin.Name}");

            Assert.That(over, Is.Empty, $"the line of {own.Box}.{own.Name}");
        }
    }

    /// <summary>A line wears the colour of the box that holds the thing it carries.</summary>
    [Test]
    public void ALine_WearsItsHoldersTone() =>
        Assert.That(StateLines(Placed()).Select(line => line.Tone), Is.All.EqualTo(Tones.DiagramTone.Green));
}
