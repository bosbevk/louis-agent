using LouisAgent.Diagrams.Engine.Components.Placed;
using LouisAgent.Diagrams.Engine.Components.Placed.Lines;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Components.Routing;

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
    /// How far from the pill's middle a line leaves or arrives, and how far from the pill's rounded ends a line may
    /// leave its bottom.
    /// </summary>
    private const double Apart = 4, PillEnd = 14;

    /// <summary>Every state's lines, for every box that holds one.</summary>
    public static IEnumerable<PlacedLine> Of(IReadOnlyList<PlacedBox> boxes, IReadOnlyList<PlacedPin> pins,
        IGeometry geometry) =>
        boxes.Where(box => box.Pill is not null).SelectMany(holder => OfHolder(holder, boxes, pins, geometry));

    /// <summary>One holder's lines: down to each pin its state names, and up from each.</summary>
    private static IEnumerable<PlacedLine> OfHolder(PlacedBox holder, IReadOnlyList<PlacedBox> boxes,
        IReadOnlyList<PlacedPin> pins, IGeometry geometry)
    {
        var state = holder.Component.State!;
        foreach (string name in state.Down ?? []) yield return Line(holder, name, false, boxes, pins, geometry);
        foreach (string name in state.Up ?? []) yield return Line(holder, name, true, boxes, pins, geometry);
    }

    /// <summary>
    /// One line: straight when its way is clear, else round. It is worked out from the pill to the pin, and a line
    /// up flows the other way.
    /// </summary>
    private static PlacedLine Line(PlacedBox holder, string name, bool up, IReadOnlyList<PlacedBox> boxes,
        IReadOnlyList<PlacedPin> pins, IGeometry geometry)
    {
        var pin = pins.First(one => one.Up == up && $"{one.On}.{one.Name}" == name);
        var own = boxes.First(box => box.Component.Id == pin.On);

        // The boxes from the holder down to the pin's own box: the only ones the line may pass through.
        var between = boxes.Where(box => holder.Box.Holds(box.Box) && box.Box.Holds(own.Box))
            .OrderBy(box => box.Depth).ToList();
        var blocks = Obstacles.For(between, boxes, pins, pin, geometry);

        // Straight first. Then round, down the deepest side that is clear: the side of the box that holds the
        // pin's box, then of the box that holds that one, and so on out to the holder's own side, which always is.
        var ways = Enumerable.Range(0, between.Count - 1).Reverse()
            .Select(side => Around(holder.Pill!.Box, pin, between, side, up, geometry))
            .Prepend(Straight(holder.Pill!.Box, pin, up, geometry)).ToList();
        var points = ways.FirstOrDefault(way => Obstacles.Clear(way, blocks)) ?? ways[^1];
        if (up) points.Reverse();
        return new PlacedLine(up ? LineKind.Up : LineKind.Down, points, null, holder.Component.Tone);
    }


    /// <summary>
    /// The straight way: down from the pill's bottom when the pin stands under it, else level from the pill's
    /// nearer end and then down.
    /// </summary>
    private static List<IPoint> Straight(IBox pill, PlacedPin pin, bool up, IGeometry geometry)
    {
        double x = pin.Box.Middle.X, top = pin.Box.Top;
        bool under = x > pill.Left + PillEnd && x < pill.Right - PillEnd;
        if (under) return [geometry.Point(x, pill.Bottom), geometry.Point(x, top)];

        double y = Leaves(pill, up), end = x < pill.Left ? pill.Left : pill.Right;
        return [geometry.Point(end, y), geometry.Point(x, y), geometry.Point(x, top)];
    }

    /// <summary>
    /// A way round: level from the pill's left end to the left side of one of the boxes between, straight down
    /// that side, and level through the gap above the pin's own box to the pin. Started at the side of the box
    /// that holds the pin's box, it has no step. Started further out, it steps in once at each box it still has to
    /// enter, through the gap above that box.
    /// </summary>
    private static List<IPoint> Around(IBox pill, PlacedPin pin, List<PlacedBox> between, int side, bool up,
        IGeometry geometry)
    {
        double lane = up ? LaneUp : LaneDown, gap = up ? GapUp : GapDown, y = Leaves(pill, up);
        List<IPoint> points = [geometry.Point(pill.Left, y), geometry.Point(between[side].Box.Left + lane, y)];
        for (int at = side + 1; at < between.Count; at++)
        {
            double above = between[at].Box.Top - gap;
            points.Add(geometry.Point(between[at - 1].Box.Left + lane, above));
            if (at < between.Count - 1) points.Add(geometry.Point(between[at].Box.Left + lane, above));
        }

        points.Add(geometry.Point(pin.Box.Middle.X, pin.Edge - gap));
        points.Add(geometry.Point(pin.Box.Middle.X, pin.Box.Top));
        return points;
    }

    /// <summary>
    /// The height a line leaves the pill's end at, or arrives at it: a little above its middle for a line down, and
    /// a little under it for a line up, so the two never run on top of each other.
    /// </summary>
    private static double Leaves(IBox pill, bool up) => pill.Middle.Y + (up ? Apart : -Apart);
}
