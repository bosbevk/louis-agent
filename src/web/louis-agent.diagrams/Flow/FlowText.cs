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
    /// How wide a character of an arrow's word is: the 600 10px code font, a pixel apart, in capitals (CL60.2).
    /// </summary>
    public const double WordChar = 7;

    /// <summary>How far a word's next line stands under the one before.</summary>
    public const double WordStep = 12;

    /// <summary>A word's lines: a <c>\n</c> breaks it, as the import does for a chart that runs down.</summary>
    public static IReadOnlyList<string> WordLines(string word) => word.Split('\n');

    /// <summary>How wide a word is: its longest line.</summary>
    public static double WordWidth(string word) => WordLines(word).Max(line => line.Length) * WordChar;

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

    /// <summary>A label's lines: a <c>\n</c> breaks it, as a docs flowchart's <c>&lt;br/&gt;</c> did.</summary>
    public static IReadOnlyList<string> LabelLines(GraphNode node) => node.Label.Split('\n');

    /// <summary>
    /// Where the label's first line sits: its lines centred in a plain box; in a noted one, from the top, above the
    /// note's lines.
    /// </summary>
    public static double LabelY(PlacedNode node) =>
        node.Node.Note is null ? node.H / 2 + 5 - (LabelLines(node.Node).Count - 1) * LabelStep / 2 : 28;

    /// <summary>Where the note's first line sits, under however many lines the label has.</summary>
    public static double NoteY(PlacedNode node) => NoteTop + (LabelLines(node.Node).Count - 1) * LabelStep;
}
