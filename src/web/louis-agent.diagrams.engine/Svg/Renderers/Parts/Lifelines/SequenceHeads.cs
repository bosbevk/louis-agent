using LouisAgent.Diagrams.Engine.Sequence;
using LouisAgent.Diagrams.Engine.Sequence.Placed;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// Draws a sequence's participant: a badge filled with its tone, its icon in the badge, its name under it with no
/// box, and its lifeline from a node under the name down the picture.
/// </summary>
/// <remarks>
/// The badge is the one place a tone is shown in this look, so it says what a participant is, as a box's tone
/// does in a flow. A person who names no icon wears a person's (<see cref="SequenceMeasures.IconOf"/>), and a
/// participant with no icon at all has an empty badge.
/// </remarks>
internal static class SequenceHeads
{
    /// <summary>A badge's side and its corner, and the icon in it.</summary>
    private const double Badge = 44, Corner = 13, BadgeIcon = 22;

    /// <summary>
    /// How far under the badge's top the name's first line stands, how big the node under the name is, and how
    /// wide a lifeline is.
    /// </summary>
    private const double NameDrop = 68, Node = 4.5, Line = 1.5;

    /// <summary>
    /// A participant's lifeline, from under its name down to where the picture's steps end. It is a thin box and
    /// not a line, so the fade that runs down it has a width to be drawn in.
    /// </summary>
    public static void Lifeline(SvgWriter writer, PlacedHead head, double bottom)
    {
        double top = head.Y + head.H;
        writer.Box(head.Center - Line / 2, top, Line, bottom - top, 0, "seq-life");
    }

    /// <summary>A participant where the layout put it: its badge, its icon, its name and the node under it.</summary>
    public static void Draw(SvgWriter writer, PlacedHead head)
    {
        double left = head.Center - Badge / 2, inset = (Badge - BadgeIcon) / 2;
        writer.Box(left, head.Y, Badge, Badge, Corner, "seq-badge" + PictureParts.Tone(head.Participant.Tone));
        if (SequenceMeasures.IconOf(head.Participant) is { } icon)
            writer.Icon(left + inset, head.Y + inset, BadgeIcon, icon, "seq-badge-icon");

        for (int at = 0; at < head.Lines.Count; at++)
            writer.Text(head.Center, head.Y + NameDrop + at * SequenceMeasures.Marquee.HeadLine, head.Lines[at],
                "seq-name");
        writer.Circle(head.Center, head.Y + head.H, Node, "mrq-node");
    }
}
