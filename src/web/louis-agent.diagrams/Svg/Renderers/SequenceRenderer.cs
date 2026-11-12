using LouisAgent.Choices;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Tones;
using static LouisAgent.Diagrams.Svg.SvgWriter;

namespace LouisAgent.Diagrams.Svg.Renderers;

/// <summary>
/// Draws a sequence in the flows' look (CL60.3, Claude's pick Q3): heads as toned boxes with an icon badge, dashed
/// lifelines under them, each call a solid arrow in its caller's tone and each reply a dashed one in the answerer's,
/// notes on panels, and blocks in quiet frames with their kind in a chip.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0103-a-diagram-is-data-on-a-row-grid.md">ADR 0103: a diagram is data</seealso>
internal sealed class SequenceRenderer : IDiagramRenderer
{
    /// <summary>The room under the drawing a caption takes.</summary>
    private const double CaptionRoom = 34;

    /// <summary>How big an icon is drawn inside its badge, and a number's chip.</summary>
    private const double BadgeIcon = 16, NumberSize = 16;

    /// <summary>How far short of a lifeline an arrow stops, so its head touches it rather than crossing it.</summary>
    private const double ArrowStop = 2;

    /// <summary>A line of small words: where it sits under the top of its room, and a chip's height.</summary>
    private const double Baseline = 10, ChipHeight = 20, ChipBaseline = 14;

    public DiagramKind Kind => DiagramKind.Sequence;

    public string Render(DiagramSource source)
    {
        var placed = SequenceLayout.Place(source.Sequence!);
        var writer = new SvgWriter();
        var toneOf = source.Sequence!.Participants.ToDictionary(head => head.Id, head => head.Tone);

        // The frames' panels first, the outer under the inner, so every line and word stands over them.
        var frames = placed.Blocks.OrderBy(block => block.Depth).ToList();
        foreach (var block in frames) writer.Box(block.X, block.Y, block.W, block.H, 8, "group");

        // The lifelines down from the heads, dashed, behind the arrows; then the frames' tabs and dividers over them.
        double top = placed.Heads.Max(head => head.Y + head.H);
        foreach (var head in placed.Heads)
            writer.Path($"M {N(head.Center)} {N(top)} L {N(head.Center)} {N(placed.Height)}", "edge dashed",
                arrow: false);
        foreach (var block in frames) Tabs(writer, block);

        foreach (var row in placed.Rows)
            if (row.Step is NoteStep) Note(writer, row);
            else Arrow(writer, row, toneOf[row.Step.Names()[0]]);

        foreach (var head in placed.Heads) Head(writer, head);

        if (source.Caption is not { } caption)
            return writer.Write(SvgTheme.Site, source.Title, placed.Width, placed.Height);
        writer.Text(0, placed.Height + CaptionRoom - 8, caption, "caption", "start");
        return writer.Write(SvgTheme.Site, source.Title, placed.Width, placed.Height + CaptionRoom);
    }

    /// <summary>
    /// A call or a reply: its arrow from lifeline to lifeline (out and back for a turn to oneself), its words above
    /// it, and its number in a chip where it starts.
    /// </summary>
    private static void Arrow(SvgWriter writer, PlacedRow row, DiagramTone tone)
    {
        string toned = FlowRenderer.Class(tone);
        double y = row.ArrowY, from = row.FromX;
        string path = row.Self
            ? $"M {N(from)} {N(y)} L {N(from + SequenceText.SelfOut)} {N(y)} " +
              $"L {N(from + SequenceText.SelfOut)} {N(y + SequenceText.SelfDrop)} " +
              $"L {N(from + ArrowStop)} {N(y + SequenceText.SelfDrop)}"
            : $"M {N(from)} {N(y)} L {N(row.ToX - Math.Sign(row.ToX - from) * ArrowStop)} {N(y)}";
        string dashed = row.Step is ReplyStep ? " dashed" : "";
        writer.Path(path, "edge" + dashed + toned, arrow: true, tone: tone == DiagramTone.Plain ? null : tone.Wire());

        // The words in a chip centred over the arrow, or for a turn to oneself, beside the turn: the chip's ground
        // hides a lifeline the words cross.
        var box = SequenceText.WordBox(row);
        if (row.Lines.Count > 0) writer.Box(box.X, box.Y, box.W, box.H, 4, "word-chip" + toned);
        (double x, string anchor) = row.Self ? (box.X + SequenceText.WordPad, "start") : ((from + row.ToX) / 2, "middle");
        for (int at = 0; at < row.Lines.Count; at++)
            writer.Text(x, row.Y + SequenceText.TextTop + Baseline + at * SequenceText.LineStep, row.Lines[at],
                "word" + toned, anchor);

        if (row.Number is not { } number) return;
        writer.Box(from - NumberSize / 2, y - NumberSize / 2, NumberSize, NumberSize, NumberSize / 2,
                "word-chip" + toned)
            .Text(from, y + 3.5, number.ToString(System.Globalization.CultureInfo.InvariantCulture), "word" + toned);
    }

    /// <summary>A note: its panel over the lifelines it names, its words centred on it.</summary>
    private static void Note(SvgWriter writer, PlacedRow row)
    {
        var (x, y, w, h) = SequenceText.NoteBox(row);
        writer.Box(x, y, w, h, 6, "word-chip");
        for (int at = 0; at < row.Lines.Count; at++)
            writer.Text(x + w / 2, y + SequenceText.WordPad + Baseline + at * SequenceText.LineStep, row.Lines[at],
                "word");
    }

    /// <summary>
    /// A block's frame over its panel: its kind in a chip at the top left with its label beside, and a dashed
    /// divider with an else chip for each other case.
    /// </summary>
    private static void Tabs(SvgWriter writer, PlacedBlock block)
    {
        Tab(writer, block.X, block.Y, block.Block.Block.Wire(), SequenceText.KindWidth(block.Block.Block),
            block.Lines);
        foreach (var @case in block.Cases)
        {
            writer.Path($"M {N(block.X)} {N(@case.Y)} L {N(block.X + block.W)} {N(@case.Y)}", "edge dashed",
                arrow: false);
            Tab(writer, block.X, @case.Y, SequenceText.ElseWord,
                SequenceText.WordsWidth([SequenceText.ElseWord]), @case.Lines);
        }
    }

    /// <summary>A chip with a word in it at a frame's left, and a label's lines beside it.</summary>
    private static void Tab(SvgWriter writer, double x, double y, string word, double width,
        IReadOnlyList<string> lines)
    {
        writer.Box(x, y, width, ChipHeight, 6, "word-chip").Text(x + width / 2, y + ChipBaseline, word, "word");
        for (int at = 0; at < lines.Count; at++)
            writer.Text(x + width + SequenceText.TabPad, y + ChipBaseline + at * SequenceText.LineStep, lines[at],
                "word", "start");
    }

    /// <summary>
    /// A head, as a flow's box: its tone, its icon in a round badge at the left and its label after it, or the
    /// label centred when it has no icon.
    /// </summary>
    private static void Head(SvgWriter writer, PlacedHead head)
    {
        string tone = FlowRenderer.Class(head.Participant.Tone);
        writer.Box(head.X, head.Y, head.W, head.H, 10, "box" + tone);

        (double x, string anchor) = (head.Center, "middle");
        if (SequenceText.IconOf(head.Participant) is { } icon)
        {
            double centreX = head.X + FlowText.BadgeLeft + FlowText.BadgeSize / 2, centreY = head.Y + head.H / 2;
            writer.Circle(centreX, centreY, FlowText.BadgeSize / 2, "badge" + tone)
                .Icon(centreX - BadgeIcon / 2, centreY - BadgeIcon / 2, BadgeIcon, icon, "badge-icon");
            (x, anchor) = (head.X + FlowText.BadgeLeft + FlowText.IconRoom, "start");
        }

        // The label's lines centred on the head's height.
        double first = head.Y + head.H / 2 - (head.Lines.Count - 1) * FlowText.LabelStep / 2 + 4;
        for (int at = 0; at < head.Lines.Count; at++)
            writer.Text(x, first + at * FlowText.LabelStep, head.Lines[at], "label" + tone, anchor);
    }
}
