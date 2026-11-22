using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Words;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;

namespace LouisAgent.Diagrams.Engine.Sequence.Listed;

/// <summary>
/// The measures of a sequence drawn as steps down a page, as one value the layout and the drawing read: the bar at
/// a step's left, how far a frame is inset, how tall a line of names and a line of words are, the gap between two
/// steps, and how wide a name's square and the line between two names are.
/// </summary>
/// <remarks>
/// A Parameter Object (Fowler, <i>Introduce Parameter Object</i>), as <see cref="SequenceMeasures"/> is for the wide
/// drawing, Louis's pick for those in F134.10. The layout is handed the numbers and holds none of its own, and the
/// two questions it asks of them, a text's lines for a room and how wide two names are on one line, are answered
/// here, so the layout and the drawing can't measure a step two ways. A frame's tab is measured by the wide
/// drawing's own measures (<see cref="Frames"/>), since the wide drawing's frames draw it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record StepListMeasures
{
    /// <summary>The cards' look, the one look a sequence has, laid out down a phone's width.</summary>
    public static StepListMeasures Marquee { get; } = new();

    /// <summary>The room a step's bar takes at its left, before its names and words start.</summary>
    public double Rail { get; init; } = 14;

    /// <summary>How far a block's steps stand inside its frame, at its left and at its right.</summary>
    public double Inset { get; init; } = 10;

    /// <summary>The room between one step's foot and the next one's top.</summary>
    public double StepGap { get; init; } = 10;

    /// <summary>The room above a step's names and under its last line of words, inside its bar.</summary>
    public double StepPad { get; init; } = 6;

    /// <summary>A line of names: the sender, the line between, the receiver.</summary>
    public double EndLine { get; init; } = 20;

    /// <summary>A line of a step's words.</summary>
    public double WordLine { get; init; } = 17;

    /// <summary>The square in a participant's tone before its name.</summary>
    public double Dot { get; init; } = 9;

    /// <summary>The room between a participant's square and its name.</summary>
    public double DotGap { get; init; } = 5;

    /// <summary>The beam or the dashes between two names, with the room either side.</summary>
    public double Arrow { get; init; } = 42;

    /// <summary>The turn after a name that calls itself, with the room before it.</summary>
    public double Turn { get; init; } = 26;

    /// <summary>The room a numbered step's ring takes before the sender's square.</summary>
    public double Ring { get; init; } = 24;

    /// <summary>A note's room left and right of its words.</summary>
    public double NoteSide { get; init; } = 12;

    /// <summary>A note's room above and under its words.</summary>
    public double NoteEdge { get; init; } = 8;

    /// <summary>A line of a note's words.</summary>
    public double NoteLine { get; init; } = 16;

    /// <summary>The room under a block's last step, inside its frame.</summary>
    public double BlockEnd { get; init; } = 4;

    /// <summary>The face a participant's name is measured in, a little bolder than the words.</summary>
    public Face NameFace { get; init; } = new(7);

    /// <summary>The face a step's words and a note's are measured in.</summary>
    public Face WordFace { get; init; } = FlowText.Word;

    /// <summary>The wide drawing's measures, which size a frame's tab, its kind's pill and an else's divider.</summary>
    public SequenceMeasures Frames { get; init; } = SequenceMeasures.Marquee;

    /// <summary>Words broken at each written break, then wrapped so no line is wider than the room.</summary>
    public IReadOnlyList<string> Lines(string text, double room) =>
        text.Split('\n').SelectMany(part => TextWrap.Lines(part, (int)WordFace.Holds(room))).ToList();

    /// <summary>A block's label, broken for the room its frame leaves beside the kind's pill.</summary>
    public IReadOnlyList<string> LabelLines(BlockKind kind, string label, double room) =>
        Lines(label, room - Frames.TabWidth(kind, []));

    /// <summary>An other case's label, broken for the room its frame leaves beside the "else" pill.</summary>
    public IReadOnlyList<string> CaseLines(string label, double room) => Lines(label, room - Frames.ElseWidth([]));

    /// <summary>A participant's name on one line, its written breaks made spaces.</summary>
    public static string Name(Participant participant) => participant.Label.Replace('\n', ' ');

    /// <summary>
    /// How wide a step's ends are on one line: the sender's square and name, the line between, and the receiver's,
    /// or for a call to oneself one name and its turn.
    /// </summary>
    public double EndsWidth(Participant from, Participant to) => from.Id == to.Id
        ? NameWidth(from) + Turn
        : NameWidth(from) + Arrow + NameWidth(to);

    /// <summary>How wide a participant's square and name are.</summary>
    public double NameWidth(Participant participant) => Dot + DotGap + NameFace.Width(Name(participant));

    /// <summary>How tall a call or an answer is: its line or two of names and its words, inside its padding.</summary>
    public double StepHeight(bool wrapped, IReadOnlyList<string> lines) =>
        2 * StepPad + (wrapped ? 2 : 1) * EndLine + lines.Count * WordLine;

    /// <summary>A note's words, broken for the room its glass leaves them.</summary>
    public IReadOnlyList<string> NoteLines(string text, double room) => Lines(text, room - 2 * NoteSide);

    /// <summary>How wide a note's glass is: its words and their room, never wider than the room it has.</summary>
    public double NoteWidth(IReadOnlyList<string> lines, double room) =>
        Math.Min(room, WordFace.Widest(lines) + 2 * NoteSide);

    /// <summary>How tall a note's glass is for its lines.</summary>
    public double NoteHeight(IReadOnlyList<string> lines) => lines.Count * NoteLine + 2 * NoteEdge;
}
