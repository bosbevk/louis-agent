using LouisAgent.Diagrams.Components.Placed;
using LouisAgent.Diagrams.Components.Placed.Lines;

namespace LouisAgent.Diagrams.Components.Routing;

/// <summary>
/// Routes the lines of the one thing a box holds (F134.7): from its pill down to each pin it is handed down
/// through, and from each pin that changes it back up to the pill.
/// </summary>
/// <remarks>
/// <para>
/// The ways are tried in order, and the first that is clear is taken. A line goes straight when nothing is in its
/// way: down from the pill, or level from the pill's end and then down. When a box, another pin or a box's own
/// words stand in that way, it goes round: level to the left side of the box that holds the pin's box, straight
/// down that side, then level through the gap above the pin's box. Only when that side is not clear either does
/// it start further out and step in, a box at a time (Louis, of a line with a step it didn't need: "not
/// straight"). The gap is always there, since the layout keeps it for the pins.
/// </para>
/// <para>
/// Lines down and lines up keep to lanes of their own, in a box's left side and in the gap above a box, so two
/// lines never run on top of each other the wrong way. A line round may still cross another at a right angle.
/// </para>
/// </remarks>
internal static class StateRoutes
{
    /// <summary>
    /// How far inside a box's left edge a line down runs, and a line up. The box's marker stands before both, and
    /// the boxes it holds start after both.
    /// </summary>
    private const double LaneDown = 14, LaneUp = 19;

    /// <summary>How far above a box's top edge a line down runs level, and a line up: both over the pins.</summary>
    private const double GapDown = 14, GapUp = 20;

    /// <summary>
    /// Half a pin's height, how far from the pill's middle a line leaves or arrives, and how far from the pill's
    /// rounded ends a line may leave its bottom.
    /// </summary>
    private const double PinTop = 9, Apart = 4, PillEnd = 14;

    /// <summary>Every state's lines, for every box that holds one.</summary>
    public static IEnumerable<PlacedLine> Of(IReadOnlyList<PlacedBox> boxes, IReadOnlyList<PlacedPin> pins) =>
        boxes.Where(box => box.Pill is not null).SelectMany(holder => OfHolder(holder, boxes, pins));

    /// <summary>One holder's lines: down to each pin its state names, and up from each.</summary>
    private static IEnumerable<PlacedLine> OfHolder(PlacedBox holder, IReadOnlyList<PlacedBox> boxes,
        IReadOnlyList<PlacedPin> pins)
    {
        var state = holder.Box.State!;
        foreach (string name in state.Down ?? []) yield return Line(holder, name, false, boxes, pins);
        foreach (string name in state.Up ?? []) yield return Line(holder, name, true, boxes, pins);
    }

    /// <summary>
    /// One line: straight when its way is clear, else round. It is worked out from the pill to the pin, and a line
    /// up flows the other way.
    /// </summary>
    private static PlacedLine Line(PlacedBox holder, string name, bool up, IReadOnlyList<PlacedBox> boxes,
        IReadOnlyList<PlacedPin> pins)
    {
        var pin = pins.First(one => one.Up == up && $"{one.Box}.{one.Name}" == name);
        var own = boxes.First(box => box.Box.Id == pin.Box);

        // The boxes from the holder down to the pin's own box: the only ones the line may pass through.
        var between = boxes.Where(box => Holds(holder, box) && Holds(box, own)).OrderBy(box => box.Depth).ToList();
        var blocks = Obstacles.For(between, boxes, pins, pin);

        // Straight first. Then round, down the deepest side that is clear: the side of the box that holds the
        // pin's box, then of the box that holds that one, and so on out to the holder's own side, which always is.
        var ways = Enumerable.Range(0, between.Count - 1).Reverse()
            .Select(side => Around(holder.Pill!, pin, between, side, up))
            .Prepend(Straight(holder.Pill!, pin, up)).ToList();
        var points = ways.FirstOrDefault(way => Obstacles.Clear(way, blocks)) ?? ways[^1];
        if (up) points.Reverse();
        return new PlacedLine(up ? LineKind.Up : LineKind.Down, points, null, holder.Box.Tone);
    }

    /// <summary>
    /// The straight way: down from the pill's bottom when the pin stands under it, else level from the pill's
    /// nearer end and then down.
    /// </summary>
    private static List<LinePoint> Straight(PlacedPill pill, PlacedPin pin, bool up)
    {
        double x = pin.Middle, top = pin.Y - PinTop;
        double y = pill.Y + pill.Height / 2 + (up ? Apart : -Apart);
        bool under = x > pill.X + PillEnd && x < pill.X + pill.Width - PillEnd;
        if (under) return [new(x, pill.Y + pill.Height), new(x, top)];

        double end = x < pill.X ? pill.X : pill.X + pill.Width;
        return [new(end, y), new(x, y), new(x, top)];
    }

    /// <summary>
    /// A way round: level from the pill's left end to the left side of one of the boxes between, straight down
    /// that side, and level through the gap above the pin's own box to the pin. Started at the side of the box
    /// that holds the pin's box, it has no step. Started further out, it steps in once at each box it still has to
    /// enter, through the gap above that box.
    /// </summary>
    private static List<LinePoint> Around(PlacedPill pill, PlacedPin pin, List<PlacedBox> between, int side,
        bool up)
    {
        double lane = up ? LaneUp : LaneDown, gap = up ? GapUp : GapDown;
        double y = pill.Y + pill.Height / 2 + (up ? Apart : -Apart);
        List<LinePoint> points = [new(pill.X, y), new(between[side].X + lane, y)];
        for (int at = side + 1; at < between.Count; at++)
        {
            double above = between[at].Y - gap;
            points.Add(new(between[at - 1].X + lane, above));
            if (at < between.Count - 1) points.Add(new(between[at].X + lane, above));
        }

        points.Add(new(pin.Middle, pin.Y - gap));
        points.Add(new(pin.Middle, pin.Y - PinTop));
        return points;
    }

    /// <summary>Whether one box holds another: the other lies within it, or is the box itself.</summary>
    private static bool Holds(PlacedBox holder, PlacedBox held) =>
        holder.X <= held.X && holder.Right >= held.Right && holder.Y <= held.Y && holder.Bottom >= held.Bottom;
}
