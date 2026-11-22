using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Sequence.Listed;
using LouisAgent.Diagrams.Engine.Sequence.Listed.Placed;
using LouisAgent.Diagrams.Engine.Svg.Paths;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.StepLists;

/// <summary>
/// Draws one step of a sequence's list of steps: a call as its two names with the wide drawing's beam between
/// them, an answer with its dashes, a call to oneself with a turn, each beside a bar with its words under the names,
/// and a note on glass.
/// </summary>
/// <remarks>
/// Louis's pick "A: an arrow between the two" (F169.7): a step says who calls whom with the very line the wide
/// drawing draws, so a reader who knows one drawing reads the other. The beam and the dashes are
/// <see cref="SequenceSteps.Beam"/> and <see cref="SequenceSteps.Dashes"/>, the functions the key draws its samples
/// with, so a step's line can't look other than the key's. The beam stands still, as the key's sample does. It
/// draws what <see cref="StepListLayout"/> placed and decides no place itself.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
internal static class StepListSteps
{
    /// <summary>A bar's width, a square's corner and a note's corner.</summary>
    private const double Bar = 2, DotCorner = 2.5, NoteCorner = 12;

    /// <summary>
    /// The words: how far under a line's middle a name stands, how far under a line's top a step's words and a
    /// note's stand, and how far a number is dropped to sit in the middle of its ring.
    /// </summary>
    private const double NameDrop = 4.5, WordDrop = 12.5, CountDrop = 3.5;

    /// <summary>How far the line between two names stands from each, and how far a turn reaches and how tall.</summary>
    private const double LineGap = 6, TurnOut = 14, TurnHalf = 5;

    private static readonly StepListMeasures Measures = StepListMeasures.Marquee;

    /// <summary>The tone of a turn's arrowhead, the wide drawing's violet.</summary>
    private static readonly string Turns = DiagramTone.Violet.Wire();

    /// <summary>One step, where the layout put it.</summary>
    public static void Draw(SvgWriter writer, PlacedStep step, IGeometry geometry)
    {
        if (step.Ends is not { } ends)
        {
            Note(writer, step);
            return;
        }

        bool call = step.Step is MessageStep;
        var box = step.Box;
        Rail(writer, box, call, geometry);

        // The names' first line, after the number's ring when there is one.
        double left = box.Left + Measures.Rail, middle = box.Top + Measures.StepPad + Measures.EndLine / 2;
        if (step.Number is { } number) left = Ring(writer, left, middle, number, call);
        double x = Name(writer, left, middle, ends.From, geometry);

        // A turn to oneself, or the line between the two and the receiver, on this line or the next.
        if (ends.Self) Turn(writer, x, middle, call);
        else Across(writer, ends, ends.Wrapped ? (left, middle + Measures.EndLine) : (x, middle), call, geometry);

        double words = middle - Measures.EndLine / 2 + (ends.Wrapped ? 2 : 1) * Measures.EndLine;
        for (int at = 0; at < step.Lines.Count; at++)
            writer.Text(box.Left + Measures.Rail, words + at * Measures.WordLine + WordDrop, step.Lines[at],
                call ? "stl-words" : "stl-words quiet", "start");
    }

    /// <summary>The bar at a step's left: solid in the accent for a call, dashed and quiet for an answer.</summary>
    private static void Rail(SvgWriter writer, IBox box, bool call, IGeometry geometry)
    {
        if (call)
        {
            writer.Box(geometry.Box(box.Left, box.Top, Bar, box.Height), 0, "stl-bar");
            return;
        }

        double x = box.Left + Bar / 2;
        writer.Path(new PathWriter().From(x, box.Top).Down(box.Bottom).Text, "stl-bar quiet", false);
    }

    /// <summary>
    /// A step's number in its ring, as it stands on the lifeline in the wide drawing; where the names start.
    /// </summary>
    private static double Ring(SvgWriter writer, double left, double middle, int number, bool call)
    {
        double x = left + SequenceSteps.Ring;
        writer.Circle(x, middle, SequenceSteps.Ring, call ? "mrq-node seq-ring" : "mrq-node seq-ring quiet")
            .Text(x, middle + CountDrop, number.ToString(CultureInfo.InvariantCulture), "seq-count");
        return left + Measures.Ring;
    }

    /// <summary>A participant's square in its tone and its name after it; where the name ends.</summary>
    private static double Name(SvgWriter writer, double left, double middle, Participant participant,
        IGeometry geometry)
    {
        var square = geometry.Box(left, middle - Measures.Dot / 2, Measures.Dot, Measures.Dot);
        writer.Box(square, DotCorner, "stl-dot" + PictureParts.Tone(participant.Tone));
        string name = StepListMeasures.Name(participant);
        double x = left + Measures.Dot + Measures.DotGap;
        writer.Text(x, middle + NameDrop, name, "stl-name", "start");
        return x + Measures.NameFace.Width(name);
    }

    /// <summary>
    /// The wide drawing's beam for a call or its dashes for an answer, from where it starts, and the receiver after
    /// it.
    /// </summary>
    private static void Across(SvgWriter writer, PlacedEnds ends, (double X, double Y) start, bool call,
        IGeometry geometry)
    {
        double from = start.X + LineGap, to = start.X + Measures.Arrow - LineGap;
        if (call) SequenceSteps.Beam(writer, geometry, from, to, start.Y, dots: false);
        else SequenceSteps.Dashes(writer, from, to, start.Y);
        Name(writer, start.X + Measures.Arrow, start.Y, ends.To, geometry);
    }

    /// <summary>A call a participant makes to itself: a small turn out of its name and back, as the wide one.</summary>
    private static void Turn(SvgWriter writer, double x, double middle, bool call)
    {
        double start = x + LineGap, far = start + TurnOut;
        string path = new PathWriter().From(start, middle - TurnHalf).Across(far).Down(middle + TurnHalf)
            .Across(start).Text;
        writer.Path(path, call ? "seq-turn" : "seq-answer", true, tone: call ? Turns : null);
    }

    /// <summary>A note: glass on a cover, its words on it from its left.</summary>
    private static void Note(SvgWriter writer, PlacedStep step)
    {
        var glass = step.Box;
        writer.Box(glass, NoteCorner, "seq-cover").Box(glass, NoteCorner, "mrq-glass");
        double first = glass.Top + Measures.NoteEdge + WordDrop;
        for (int at = 0; at < step.Lines.Count; at++)
            writer.Text(glass.Left + Measures.NoteSide, first + at * Measures.NoteLine, step.Lines[at], "pic-text",
                "start");
    }
}
