using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Flow;

/// <summary>
/// Where a box's words sit inside it: the site's <c>GraphView</c> and the docs' SVG both draw them here, so a box
/// reads the same on a page and in a doc.
/// </summary>
public static class FlowText
{
    /// <summary>Where the note's first line sits, from the box's top, each line one step lower.</summary>
    public const double NoteTop = 44, NoteStep = 12;

    /// <summary>
    /// How wide a character of an arrow's word is: the 10px code font with a little to spare, so a font that runs
    /// wide still fits its chip (CL60.2).
    /// </summary>
    public const double WordChar = 7;

    /// <summary>How far a word's next line stands under the one before.</summary>
    public const double WordStep = 12;

    /// <summary>
    /// The chip a word stands in (CL60.2, Louis's pick "Both looks"): the room beside its lines, how far its top
    /// stands above the word's point, and what it adds under the lines; its bottom stands 4 pixels above the point,
    /// so a chip over a line keeps clear of it.
    /// </summary>
    public const double ChipPad = 4, ChipRise = 22, ChipExtra = 6;

    /// <summary>How far above the word's point its first line's baseline stands, in the middle of its chip.</summary>
    public const double WordRise = 10;

    /// <summary>A word's lines: a <c>\n</c> breaks it, as the import does for a chart that runs down.</summary>
    public static IReadOnlyList<string> WordLines(string word) => word.Split('\n');

    /// <summary>How wide a word's chip is: its longest line and the room either side.</summary>
    public static double WordWidth(string word) =>
        WordLines(word).Max(line => line.Length) * WordChar + 2 * ChipPad;

    /// <summary>
    /// The ground a word covers, its chip, centred on the word's point and just above it: the layout keeps it clear
    /// of lines and other words, and the renderer draws the chip there.
    /// </summary>
    public static (double X, double Y, double W, double H) WordBox(PlacedEdge edge)
    {
        if (edge.Edge.Word is not { } word) return (edge.WordX, edge.WordY, 0, 0);
        double width = WordWidth(word);
        return (edge.WordX - width / 2, edge.WordY - ChipRise, width,
            WordStep * WordLines(word).Count + ChipExtra);
    }

    /// <summary>
    /// The round badge a box's icon sits in (CL60.2): its size, how far it stands from the box's left, and the room
    /// between it and the label.
    /// </summary>
    public const double BadgeSize = 28, BadgeLeft = GraphLayout.LabelPad, BadgeGap = 10;

    /// <summary>What a box with an icon adds to its width: the badge and the gap after it.</summary>
    public const double IconRoom = BadgeSize + BadgeGap;

    /// <summary>Where a label starts in a box with an icon: after the badge, from the left.</summary>
    public static double LabelLeft(PlacedNode node) => node.X + BadgeLeft + IconRoom;

    /// <summary>How far a label's next line stands under the one before (CL60.2).</summary>
    public const double LabelStep = 16;

    /// <summary>
    /// A box's members (CL60.4): how far each stands under the one before, the room between the divider and the
    /// first, and the room under the last.
    /// </summary>
    public const double MemberStep = 14, MemberTop = 8, MemberEnd = 10;

    /// <summary>The characters a member's line holds before it goes on to the next (CL60.4).</summary>
    public const int MemberWidth = 60;

    /// <summary>
    /// A box's members as the lines they take: one a member, or a long one over several, each after the first
    /// indented so the reader sees it goes on.
    /// </summary>
    public static IReadOnlyList<string> MemberLines(GraphNode node) =>
        (node.Members ?? []).SelectMany(member =>
            TextWrap.Lines(member, MemberWidth).Select((line, at) => at == 0 ? line : "  " + line)).ToList();

    /// <summary>A label's lines: a <c>\n</c> breaks it, as a docs flowchart's <c>&lt;br/&gt;</c> did.</summary>
    public static IReadOnlyList<string> LabelLines(GraphNode node) => node.Label.Split('\n');

    /// <summary>
    /// Where the label's first line sits: its lines centred in a plain box; in a noted one, from the top, above the
    /// note's lines.
    /// </summary>
    public static double LabelY(PlacedNode node) =>
        node.Node.Note is null ? HeadHeight(node) / 2 + 5 - (LabelLines(node.Node).Count - 1) * LabelStep / 2 : 28;

    /// <summary>
    /// How tall a placed box's head is: the whole box, or with members (CL60.4), the part above the divider, where the
    /// label and the badge stand.
    /// </summary>
    public static double HeadHeight(PlacedNode node) =>
        node.Node.Members is { Count: > 0 } ? GraphLayout.HeadOf(node.Node) : node.H;

    /// <summary>Where the note's first line sits, under however many lines the label has.</summary>
    public static double NoteY(PlacedNode node) => NoteTop + (LabelLines(node.Node).Count - 1) * LabelStep;
}
