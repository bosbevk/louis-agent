using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes.Marks;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Types;

/// <summary>
/// Draws a class chart's type as a glass card: a badge in its tone with a letter for what it is, its name beside
/// the badge, its kind under the name, and its members under a divider.
/// </summary>
/// <remarks>
/// Every word stands where the flow's measures put it (<see cref="FlowText"/>), since the flow's layout made the
/// room: the name beside a badge's room, the kind under it, the members under the head. A contract, an
/// interface, a record or an enum, wears the accent along its top, so the types others are written against stand
/// out before a letter is read. A type that names an icon shows the icon in its badge and no letter.
/// </remarks>
internal static class TypeCards
{
    /// <summary>A card's corner, a badge's corner, and the icon in a badge.</summary>
    private const double Corner = 14, BadgeCorner = 9, BadgeIcon = 16;

    /// <summary>How far in a contract's accent starts along its top.</summary>
    private const double EdgeIn = 24;

    /// <summary>How far under a badge's middle its letter stands, and under a member's row its words.</summary>
    private const double MarkDrop = 4.5, MemberDrop = 10;

    /// <summary>Where a group's label stands in its frame: in from its left, and down from its top.</summary>
    private const double GroupLabelIn = 12, GroupLabelLine = 18;

    /// <summary>One type, where the layout put its box.</summary>
    public static void Draw(SvgWriter writer, PlacedNode node, ClassType type, IGeometry geometry)
    {
        var mark = TypeMarks.Of(type);
        var box = node.Box;
        double head = FlowText.HeadHeight(node);
        writer.Box(box, Corner, "cls-cover").Box(box, Corner, "mrq-glass");
        if (TypeMarks.Contract(mark)) PictureParts.Accent(writer, geometry, box, EdgeIn);

        Badge(writer, node, type, mark, head, geometry);
        Words(writer, node);
        Members(writer, node, head);
    }

    /// <summary>A group's frame, with its label at its top left.</summary>
    public static void Frame(SvgWriter writer, PlacedGroup group) => writer
        .Box(group.Box, Corner, "cls-group")
        .Text(group.Box.Left + GroupLabelIn, group.Box.Top + GroupLabelLine, group.Group.Label, "pic-group", "start");

    /// <summary>
    /// The badge, in the middle of the head's height: the type's own icon when it names one, and the letter of
    /// what it is when it doesn't.
    /// </summary>
    private static void Badge(SvgWriter writer, PlacedNode node, ClassType type, TypeMark mark, double head,
        IGeometry geometry)
    {
        double size = FlowText.BadgeSize;
        var badge = geometry.Box(node.Box.Left + FlowText.BadgeLeft, node.Box.Top + (head - size) / 2, size, size);
        writer.Box(badge, BadgeCorner, "cls-badge" + PictureParts.Tone(node.Node.Tone));

        // The icon stands in the badge's middle, as far in from its left as from its top.
        double inset = (size - BadgeIcon) / 2;
        if (type.Icon is { } icon)
            writer.Icon(badge.Left + inset, badge.Top + inset, BadgeIcon, icon, "cls-badge-icon");
        else
            writer.Text(badge.Middle.X, badge.Middle.Y + MarkDrop, TypeMarks.Letter(mark), "cls-mark");
    }

    /// <summary>The name's lines beside the badge, and the kind's under them.</summary>
    private static void Words(SvgWriter writer, PlacedNode node)
    {
        double x = FlowText.LabelLeft(node), top = node.Box.Top;
        var name = FlowText.LabelLines(node.Node);
        for (int at = 0; at < name.Count; at++)
            writer.Text(x, top + FlowText.LabelY(node) + FlowText.LabelStep * at, name[at], "cls-name", "start");
        var kind = node.Node.Note is { } words ? GraphLayout.NoteLines(words) : [];
        for (int at = 0; at < kind.Count; at++)
            writer.Text(x, top + FlowText.NoteY(node) + FlowText.NoteStep * at, kind[at], "cls-kind", "start");
    }

    /// <summary>
    /// The members under a divider, each on its row. A member is written with a plus for public, which every
    /// member of a chart has, so the plus is left out.
    /// </summary>
    private static void Members(SvgWriter writer, PlacedNode node, double head)
    {
        var members = FlowText.MemberLines(node.Node);
        if (members.Count == 0) return;

        double divider = node.Box.Top + head, left = node.Box.Left + GraphLayout.LabelPad;
        double right = node.Box.Right - GraphLayout.LabelPad;
        writer.Path(PathWriter.Level(left, right, divider), "cls-divide", false);
        for (int at = 0; at < members.Count; at++)
            writer.Text(left, divider + FlowText.MemberTop + MemberDrop + FlowText.MemberStep * at,
                members[at].TrimStart('+'), "cls-member", "start");
    }
}
