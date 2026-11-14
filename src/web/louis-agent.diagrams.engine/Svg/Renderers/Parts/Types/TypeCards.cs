using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Classes.Marks;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;

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

    /// <summary>How far in a contract's accent starts along its top, and how thick it is.</summary>
    private const double EdgeIn = 24, Edge = 1.6;

    /// <summary>How far under a badge's middle its letter stands, and under a member's row its words.</summary>
    private const double MarkDrop = 4.5, MemberDrop = 10;

    /// <summary>One type, where the layout put its box.</summary>
    public static void Draw(SvgWriter writer, PlacedNode node, ClassType type)
    {
        var mark = TypeMarks.Of(type);
        double head = FlowText.HeadHeight(node);
        writer.Box(node.X, node.Y, node.W, node.H, Corner, "cls-cover")
            .Box(node.X, node.Y, node.W, node.H, Corner, "mrq-glass");
        if (TypeMarks.Contract(mark))
            writer.Box(node.X + EdgeIn, node.Y - 0.5, Math.Max(0, node.W - 2 * EdgeIn), Edge, 0, "mrq-edge");

        Badge(writer, node, type, mark, head);
        Words(writer, node);
        Members(writer, node, head);
    }

    /// <summary>A group's frame, with its label at its top left.</summary>
    public static void Frame(SvgWriter writer, PlacedGroup group) => writer
        .Box(group.X, group.Y, group.W, group.H, Corner, "cls-group")
        .Text(group.X + 12, group.Y + 18, group.Group.Label, "pic-group", "start");

    /// <summary>
    /// The badge, in the middle of the head's height: the type's own icon when it names one, and the letter of
    /// what it is when it doesn't.
    /// </summary>
    private static void Badge(SvgWriter writer, PlacedNode node, ClassType type, TypeMark mark, double head)
    {
        double size = FlowText.BadgeSize, left = node.X + FlowText.BadgeLeft, top = node.Y + (head - size) / 2;
        writer.Box(left, top, size, size, BadgeCorner, "cls-badge" + PictureParts.Tone(node.Node.Tone));
        if (type.Icon is { } icon)
            writer.Icon(left + (size - BadgeIcon) / 2, top + (size - BadgeIcon) / 2, BadgeIcon, icon,
                "cls-badge-icon");
        else
            writer.Text(left + size / 2, top + size / 2 + MarkDrop, TypeMarks.Letter(mark), "cls-mark");
    }

    /// <summary>The name's lines beside the badge, and the kind's under them.</summary>
    private static void Words(SvgWriter writer, PlacedNode node)
    {
        double x = FlowText.LabelLeft(node);
        var name = FlowText.LabelLines(node.Node);
        for (int at = 0; at < name.Count; at++)
            writer.Text(x, node.Y + FlowText.LabelY(node) + FlowText.LabelStep * at, name[at], "cls-name", "start");
        var kind = node.Node.Note is { } words ? GraphLayout.NoteLines(words) : [];
        for (int at = 0; at < kind.Count; at++)
            writer.Text(x, node.Y + FlowText.NoteY(node) + FlowText.NoteStep * at, kind[at], "cls-kind", "start");
    }

    /// <summary>
    /// The members under a divider, each on its row. A member is written with a plus for public, which every
    /// member of a chart has, so the plus is left out.
    /// </summary>
    private static void Members(SvgWriter writer, PlacedNode node, double head)
    {
        var members = FlowText.MemberLines(node.Node);
        if (members.Count == 0) return;

        double divider = node.Y + head, left = node.X + GraphLayout.LabelPad;
        double right = node.X + node.W - GraphLayout.LabelPad;
        writer.Path($"M{SvgWriter.N(left)} {SvgWriter.N(divider)}H{SvgWriter.N(right)}", "cls-divide", false);
        for (int at = 0; at < members.Count; at++)
            writer.Text(left, divider + FlowText.MemberTop + MemberDrop + FlowText.MemberStep * at,
                members[at].TrimStart('+'), "cls-member", "start");
    }
}
