using LouisAgent.Composition.Messages;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;
using LouisAgent.DocDiagrams.Messages;

namespace LouisAgent.DocDiagrams.Mermaid.Readers;

/// <summary>
/// Reads a docs Mermaid sequence diagram, once, into a sequence diagram's records (CL60.3): its participants and
/// actors, its calls and replies, its notes, and its loop, opt and alt blocks with their else cases, nested. A line it
/// doesn't know is refused, naming it, so nothing drops out of the picture unseen.
/// </summary>
/// <remarks>
/// A block opens with its kind and closes with <c>end</c>, so the reader keeps a stack of the blocks still open and
/// adds each step to the innermost (Interpreter over the docs' small part of Mermaid's grammar, as the flow reader).
/// </remarks>
public static class MermaidSequenceReader
{
    /// <summary>The arrows the docs write, longest first so a reply's arrow isn't read as a call's.</summary>
    private static readonly string[] Arrows = ["-->>", "->>", "--x", "--)", "-->", "-x", "-)", "->"];

    /// <summary>The words that open a block, and the kind each opens.</summary>
    private static readonly Dictionary<string, BlockKind> Opens =
        new(StringComparer.Ordinal) { ["loop"] = BlockKind.Loop, ["opt"] = BlockKind.Opt, ["alt"] = BlockKind.Alt };

    /// <summary>The sequence diagram as a sequence with this title.</summary>
    public static DiagramSource Read(string block, string title)
    {
        var heads = new List<Participant>();
        var top = new List<SequenceStep>();
        var open = new Stack<Frame>();
        bool numbered = false;

        // The first line names the kind; every other line is a participant, a step, or a block's start or end.
        foreach (string line in block.ReplaceLineEndings("\n").Split('\n').Skip(1).Select(line => line.Trim()))
        {
            var steps = open.Count == 0 ? top : open.Peek().Current;
            var (word, rest) = Split(line);
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal)) continue;
            if (line == "autonumber")
            {
                numbered = true;
                continue;
            }

            if (word is "participant" or "actor")
            {
                Declare(heads, rest, word == "actor");
                continue;
            }

            if (Opens.TryGetValue(word, out var kind))
            {
                open.Push(new Frame(kind, MermaidLabel.Text(rest)));
                continue;
            }

            if (word == "else" && open.Count > 0)
            {
                open.Peek().Else(MermaidLabel.Text(rest));
                continue;
            }

            if (line == "end" && open.Count > 0)
            {
                var closed = open.Pop().Step();
                (open.Count == 0 ? top : open.Peek().Current).Add(closed);
                continue;
            }

            bool note = word.Equals("Note", StringComparison.OrdinalIgnoreCase);
            steps.Add(note ? Note(line, rest, heads) : Message(line, heads));
        }

        if (open.Count > 0) throw Unread("end");
        return new DiagramSource(DiagramKind.Sequence, title, Sequence: new SequenceSpec(heads, top, numbered));
    }

    /// <summary>A line's first word, and the rest after it.</summary>
    private static (string Word, string After) Split(string line)
    {
        int space = line.IndexOf(' ');
        return space < 0 ? (line, "") : (line[..space], line[(space + 1)..].Trim());
    }

    /// <summary>
    /// A participant or an actor, by its id and the name after <c>as</c>; one already met in a message keeps its
    /// place and takes the name.
    /// </summary>
    private static void Declare(List<Participant> heads, string rest, bool actor)
    {
        int alias = rest.IndexOf(" as ", StringComparison.Ordinal);
        string id = alias < 0 ? rest : rest[..alias].Trim();
        var head = new Participant(id, alias < 0 ? id : MermaidLabel.Text(rest[(alias + 4)..]), Actor: actor);
        int at = heads.FindIndex(known => known.Id == id);
        if (at < 0) heads.Add(head);
        else heads[at] = head;
    }

    /// <summary>A participant met first in a step joins the chart there, named by its id.</summary>
    private static string Join(List<Participant> heads, string id)
    {
        if (heads.All(known => known.Id != id)) heads.Add(new Participant(id, id));
        return id;
    }

    /// <summary>A note: <c>over A,B</c>, <c>right of A</c> or <c>left of A</c>, its words after the colon.</summary>
    private static NoteStep Note(string line, string rest, List<Participant> heads)
    {
        int colon = rest.IndexOf(':');
        if (colon < 0) throw Unread(line);
        string where = rest[..colon].Trim();
        foreach (string place in new[] { "over ", "right of ", "left of " })
            if (where.StartsWith(place, StringComparison.Ordinal))
                where = where[place.Length..];

        var over = where.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(id => Join(heads, id)).ToList();
        return new NoteStep(over, MermaidLabel.Text(rest[(colon + 1)..]));
    }

    /// <summary>A call or a reply: the first arrow on the line between two ids, its words after the colon.</summary>
    private static SequenceStep Message(string line, List<Participant> heads)
    {
        int colon = line.IndexOf(':');
        string ends = colon < 0 ? line : line[..colon];
        string text = colon < 0 ? "" : MermaidLabel.Text(line[(colon + 1)..]);
        for (int at = 1; at < ends.Length; at++)
        {
            string? arrow = Arrows.FirstOrDefault(arrow => ends.AsSpan(at).StartsWith(arrow, StringComparison.Ordinal));
            if (arrow is null) continue;

            // An activation mark (+ or -) after the arrow says nothing the drawing shows.
            string from = ends[..at].Trim(), to = ends[(at + arrow.Length)..].Trim().TrimStart('+', '-');
            if (from.Length == 0 || to.Length == 0) throw Unread(line);
            (from, to) = (Join(heads, from), Join(heads, to));
            return arrow.StartsWith("--", StringComparison.Ordinal)
                ? new ReplyStep(from, to, text)
                : new MessageStep(from, to, text);
        }

        throw Unread(line);
    }

    /// <summary>The line can't be read, said with the line.</summary>
    private static InvalidDataException Unread(string line) =>
        new(MessageText.Format(DocDiagramsUserText.UnreadMermaid, new { Line = line }));

    /// <summary>A block still open: its kind, its label, its own steps and each else case's after them.</summary>
    private sealed class Frame(BlockKind kind, string label)
    {
        /// <summary>The block's own steps, and each else case's label and steps in turn.</summary>
        private readonly List<SequenceStep> _steps = [];

        private readonly List<(string Label, List<SequenceStep> Steps)> _cases = [];

        /// <summary>Where the next step goes: the last else case's, or the block's own when it has none.</summary>
        public List<SequenceStep> Current => _cases.Count == 0 ? _steps : _cases[^1].Steps;

        /// <summary>An else starts the next case.</summary>
        public void Else(string caseLabel) => _cases.Add((caseLabel, []));

        /// <summary>The finished block, as its step.</summary>
        public BlockStep Step() => new(kind, label, _steps,
            _cases.Count == 0 ? null : _cases.Select(@case => new ElseCase(@case.Label, @case.Steps)).ToList());
    }
}
