using LouisAgent.Composition.Messages;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.DocDiagrams.Messages;

namespace LouisAgent.DocDiagrams.Mermaid.Readers;

/// <summary>
/// Reads one Mermaid statement, a chain of boxes and arrows (<c>a["A"] --&gt;|calls| b &amp; c -.-&gt; d</c>), into
/// its boxes, each step's boxes side by side, and the arrow between each step and the next.
/// </summary>
/// <remarks>
/// A small reader that walks the line once (ADR 0057: a regular expression is the last option): a box is an id and an
/// optional shape holding its label; an arrow is a run of dashes, dots, equals and a head, with its word between
/// pipes after it or between its two halves. A class, since it keeps its place in the line as it reads.
/// </remarks>
internal sealed class MermaidChain(string line)
{
    /// <summary>The shapes a box's label may stand in, the longer openings first, and what closes each.</summary>
    private static readonly (string Open, string Close)[] Shapes =
    [
        ("([", "])"), ("[(", ")]"), ("[[", "]]"), ("((", "))"), ("{{", "}}"), ("[/", "]"), ("[\\", "]"),
        ("[", "]"), ("(", ")"), ("{", "}"), (">", "]")
    ];

    private int _at;

    /// <summary>A box as the chain names it: its id, and its label when this mention gives one.</summary>
    public sealed record Mention(string Id, string? Label);

    /// <summary>An arrow as the chain writes it: the word on it, if any, and whether it is dashed.</summary>
    public sealed record Link(string? Word, EdgeStyle Style);

    /// <summary>
    /// The chain's steps, each the boxes joined by <c>&amp;</c>, and the arrow after each step but the last.
    /// </summary>
    public (List<List<Mention>> Steps, List<Link> Arrows) Read()
    {
        var steps = new List<List<Mention>> { Step() };
        var arrows = new List<Link>();
        while (Arrow() is { } arrow)
        {
            arrows.Add(arrow);
            steps.Add(Step());
        }

        return (steps, arrows);
    }

    /// <summary>One box, or several joined by <c>&amp;</c>.</summary>
    private List<Mention> Step()
    {
        var boxes = new List<Mention> { Box() };
        while (Peek() == '&')
        {
            _at++;
            boxes.Add(Box());
        }

        return boxes;
    }

    /// <summary>A box: its id, then its label in whatever shape holds it.</summary>
    public Mention Box()
    {
        SkipSpaces();
        int start = _at;
        while (_at < line.Length && (char.IsLetterOrDigit(line[_at]) || line[_at] == '_')) _at++;
        if (_at == start) throw Unread();

        string id = line[start.._at];
        var shape = Shapes.FirstOrDefault(pair => line.AsSpan(_at).StartsWith(pair.Open, StringComparison.Ordinal));
        if (shape.Open is null) return new Mention(id, null);

        _at += shape.Open.Length;
        SkipSpaces();
        // A quoted label may hold any bracket; an unquoted one runs to its shape's close.
        int end = Peek() == '"' ? line.IndexOf('"', _at + 1) : line.IndexOf(shape.Close, _at, StringComparison.Ordinal);
        if (end < 0) throw Unread();
        string raw = Peek() == '"' ? line[(_at + 1)..end] : line[_at..end];
        int close = line.IndexOf(shape.Close, end, StringComparison.Ordinal);
        _at = close < 0 ? line.Length : close + shape.Close.Length;
        return new Mention(id, MermaidLabel.Clean(raw.TrimEnd('/', '\\')));
    }

    /// <summary>The arrow here, with the word on it and whether it is dashed; none at the line's end.</summary>
    private Link? Arrow()
    {
        SkipSpaces();
        if (Peek() == '<') _at++;
        int start = _at;
        while (_at < line.Length && "-.=>".Contains(line[_at])) _at++;
        if (_at == start) return null;

        // A word between the halves, "-- calls -->", "-. maybe .->" or "== must ==>", runs to the second half.
        string run = line[start.._at];
        string? word = null;
        string? half = run switch
        {
            "--" => "--",
            "-." => ".-",
            "==" => "==",
            _ => null
        };
        if (half is not null && Peek() != '\0')
        {
            int end = line.IndexOf(half, _at, StringComparison.Ordinal);
            if (end < 0) throw Unread();
            word = line[_at..end];
            _at = end;
            while (_at < line.Length && "-.=>".Contains(line[_at])) _at++;
        }

        // A head of o or x ends the arrow too; a word between pipes may follow it.
        if (_at < line.Length && line[_at] is 'o' or 'x' && (_at + 1 == line.Length || line[_at + 1] == ' ')) _at++;
        SkipSpaces();
        if (Peek() == '|')
        {
            int end = line.IndexOf('|', _at + 1);
            if (end < 0) throw Unread();
            word = line[(_at + 1)..end];
            _at = end + 1;
        }

        // A word may be written in quotes, "-->|\"fix sessions\"|"; the quotes are no part of it.
        string? clean = word is null ? null : MermaidLabel.Clean(word.Trim().Trim('"'));
        return new Link(string.IsNullOrEmpty(clean) ? null : clean,
            run.Contains('.') ? EdgeStyle.Dashed : EdgeStyle.Solid);
    }

    /// <summary>The character here, past any spaces, or none at the end.</summary>
    private char Peek()
    {
        SkipSpaces();
        return _at < line.Length ? line[_at] : '\0';
    }

    private void SkipSpaces()
    {
        while (_at < line.Length && line[_at] == ' ') _at++;
    }

    /// <summary>The line can't be read, said with the line.</summary>
    private InvalidDataException Unread() =>
        new(MessageText.Format(DocDiagramsUserText.UnreadMermaid, new { Line = line }));
}
