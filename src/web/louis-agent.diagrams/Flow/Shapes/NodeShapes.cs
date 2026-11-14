using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow.Shapes;

/// <summary>
/// What a box's shape asks of the layout (F134.8): how much room the shape needs to hold the box's words, and how
/// far inside the box its outline is at a place along a side, so an arrow ends on the outline and not short of it.
/// </summary>
/// <remarks>
/// A shape is drawn inside the box the layout gave it, so the layout and the router go on working with boxes, and
/// only ask here for the two things a shape changes. A picture that runs down is laid out turned, so the side an
/// arrow arrives on is then the shape's top, and the questions are answered for all four sides.
/// </remarks>
public static class NodeShapes
{
    /// <summary>How far a slanted box's top is pushed to the right of its bottom.</summary>
    public const double Skew = 18;

    /// <summary>
    /// How far in from its tip a decision's corners stand: its two ends are points, and its top and bottom are
    /// flat between the corners (F134.12).
    /// </summary>
    public const double Point = 22;

    /// <summary>
    /// The size a box needs in its shape, from the size it would have as a plain box: a slanted box its slant
    /// wider, a pill wider by the round of its ends, and a decision wider by a point. A plain box's room round
    /// its words is more than half a point each side, so the words still fit between the two slanted ends.
    /// </summary>
    public static (double W, double H) Room(GraphNode node, double w, double h) => node.Shape switch
    {
        NodeShape.Decision => (w + Point, h),
        NodeShape.Data => (w + Skew, h),
        NodeShape.Terminal => (w + h / 2, h),
        _ => (w, h)
    };

    /// <summary>
    /// How far inside the box a shape's outline is, at a place along one of the box's sides. Nothing for a plain
    /// box, and nothing at the tip of a decision's end or along its flat top.
    /// </summary>
    public static double Inset(NodeShape shape, ShapeSide side, double w, double h, double along) =>
        (shape, side) switch
        {
            (NodeShape.Decision, ShapeSide.Left or ShapeSide.Right) => PointOf(h) * Math.Abs(along - h / 2) / (h / 2),
            (NodeShape.Decision, _) => h / 2 * Math.Max(0,
                Math.Max(1 - along / PointOf(h), (along - (w - PointOf(h))) / PointOf(h))),
            (NodeShape.Terminal, ShapeSide.Left or ShapeSide.Right) => Round(h / 2, along - h / 2),
            (NodeShape.Terminal, _) => Round(h / 2, Math.Max(0, Math.Max(h / 2 - along, along - (w - h / 2)))),
            (NodeShape.Data, ShapeSide.Left) => Skew * (1 - along / h),
            (NodeShape.Data, ShapeSide.Right) => Skew * along / h,
            (NodeShape.Data, ShapeSide.Top) => along < Skew ? h * (1 - along / Skew) : 0,
            (NodeShape.Data, ShapeSide.Bottom) => along > w - Skew ? h * (along - (w - Skew)) / Skew : 0,
            _ => 0
        };

    /// <summary>
    /// How far in the outline is on the side arrows arrive on, at a height of the layout: the shape's left, or its
    /// top when the picture runs down and is laid out turned.
    /// </summary>
    public static double Entry(PlacedNode node, double y, bool turned) => turned
        ? Inset(node.Node.Shape, ShapeSide.Top, node.H, node.W, y - node.Y)
        : Inset(node.Node.Shape, ShapeSide.Left, node.W, node.H, y - node.Y);

    /// <summary>
    /// How far in the outline is on the side arrows leave from, at a height of the layout: the shape's right, or
    /// its bottom laid out turned.
    /// </summary>
    public static double Exit(PlacedNode node, double y, bool turned) => turned
        ? Inset(node.Node.Shape, ShapeSide.Bottom, node.H, node.W, y - node.Y)
        : Inset(node.Node.Shape, ShapeSide.Right, node.W, node.H, y - node.Y);

    /// <summary>
    /// How far up the outline is on the side under the box in the layout, at a place along it: the shape's bottom,
    /// or its right laid out turned.
    /// </summary>
    public static double Under(PlacedNode node, double x, bool turned) => turned
        ? Inset(node.Node.Shape, ShapeSide.Right, node.H, node.W, x - node.X)
        : Inset(node.Node.Shape, ShapeSide.Bottom, node.W, node.H, x - node.X);

    /// <summary>
    /// The outline of a decision or a slanted box as a path, round the box the layout placed. Nothing for a plain
    /// box or a pill, which are drawn as a rounded box.
    /// </summary>
    public static string? PathOf(PlacedNode node)
    {
        double left = node.X, right = node.X + node.W, top = node.Y, bottom = node.Y + node.H;
        double down = node.Y + node.H / 2, point = PointOf(node.H);
        return node.Node.Shape switch
        {
            NodeShape.Decision => Closed((left, down), (left + point, top), (right - point, top), (right, down),
                (right - point, bottom), (left + point, bottom)),
            NodeShape.Data => Closed((left + Skew, top), (right, top), (right - Skew, bottom), (left, bottom)),
            _ => null
        };
    }

    /// <summary>A closed path through corners, each number as an SVG writes it.</summary>
    private static string Closed(params (double X, double Y)[] corners) =>
        "M" + string.Join("L", corners.Select(corner => FormattableString.Invariant($"{corner.X:0.#} {corner.Y:0.#}")))
            + "Z";

    /// <summary>
    /// A decision's point for its height: the usual one, or half the height for a shape so small that the usual
    /// one would cross itself, as the key's sample is.
    /// </summary>
    private static double PointOf(double h) => Math.Min(Point, h / 2);

    /// <summary>How far in a half circle's edge is, a way off the middle of its flat side.</summary>
    private static double Round(double radius, double off) =>
        radius - Math.Sqrt(Math.Max(0, radius * radius - off * off));
}
