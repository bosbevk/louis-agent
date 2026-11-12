using System.Text;
using LouisAgent.Composition.Messages;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.DocDiagrams.Messages;

namespace LouisAgent.DocDiagrams.Mermaid.Readers;

/// <summary>
/// Reads a docs Mermaid class diagram, once, into a class chart's records (CL60.4): its classes, written on one line
/// or over several, with their stereotype as their kind and their members, generics written as C# writes them; its
/// relations, each arrow read as its kind; and its direction. A line it doesn't know is refused, naming it.
/// </summary>
public static class MermaidClassReader
{
    /// <summary>
    /// The arrows the docs write, and the relation each means; reversed when the arrow's head is on the left, so
    /// <c>A &lt;|-- B</c> reads as B a kind of A. Longest first, so a longer arrow isn't read as a shorter one.
    /// </summary>
    private static readonly (string Arrow, RelationKind Kind, bool Reversed)[] Arrows =
    [
        ("<|--", RelationKind.Inherits, true), ("<|..", RelationKind.Implements, true),
        ("--|>", RelationKind.Inherits, false), ("..|>", RelationKind.Implements, false),
        ("o--", RelationKind.Holds, false), ("*--", RelationKind.Holds, false), ("-->", RelationKind.Holds, false),
        ("..>", RelationKind.Uses, false), ("--", RelationKind.Uses, false), ("..", RelationKind.Uses, false)
    ];

    /// <summary>The class diagram as a class chart with this title.</summary>
    public static DiagramSource Read(string block, string title)
    {
        var types = new List<ClassType>();
        var relations = new List<ClassRelation>();
        var direction = FlowDirection.Right;
        string[] lines = block.ReplaceLineEndings("\n").Split('\n').Skip(1).Select(line => line.Trim()).ToArray();

        // Every line is a class, a relation or the direction; a class's body may run over the lines after it.
        for (int at = 0; at < lines.Length; at++)
        {
            string line = lines[at];
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal)) continue;
            if (line.StartsWith("direction ", StringComparison.Ordinal))
            {
                direction = line.EndsWith("LR", StringComparison.Ordinal) || line.EndsWith("RL", StringComparison.Ordinal)
                    ? FlowDirection.Right
                    : FlowDirection.Down;
                continue;
            }

            if (line.StartsWith("class ", StringComparison.Ordinal))
            {
                var (head, body, last) = Body(lines, at);
                Declare(types, head, body);
                at = last;
                continue;
            }

            relations.Add(Relation(line, types) ?? throw Unread(line));
        }

        return new DiagramSource(DiagramKind.Classes, title, Classes: new ClassesSpec(types, relations, direction));
    }

    /// <summary>
    /// A class line's name and body: the body between braces on the same line, or the lines up to the closing
    /// brace, or none; and the last line it took.
    /// </summary>
    private static (string Head, IReadOnlyList<string> Body, int Last) Body(string[] lines, int at)
    {
        string line = lines[at]["class ".Length..].Trim();
        int open = line.IndexOf('{');
        if (open < 0) return (line, [], at);

        string head = line[..open].Trim(), rest = line[(open + 1)..];
        int close = rest.LastIndexOf('}');
        if (close >= 0) return (head, OneLine(rest[..close]), at);

        int end = Array.FindIndex(lines, at + 1, next => next == "}");
        if (end < 0) throw Unread(lines[at]);
        return (head, lines[(at + 1)..end].Where(next => next.Length > 0).ToList(), end);
    }

    /// <summary>
    /// A one-line body as its stereotype and its members: a member starts at each word that opens with its visibility
    /// (+, -, #), a bracket's words staying together, and words before any of those are one member.
    /// </summary>
    private static IReadOnlyList<string> OneLine(string body)
    {
        var items = new List<string>();
        string rest = body.Trim();
        int stereotype = rest.StartsWith("<<", StringComparison.Ordinal) ? rest.IndexOf(">>", StringComparison.Ordinal) : -1;
        if (stereotype > 0)
        {
            items.Add(rest[..(stereotype + 2)]);
            rest = rest[(stereotype + 2)..];
        }

        var member = new StringBuilder();
        int depth = 0;
        foreach (string word in rest.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            bool starts = depth == 0 && word[0] is '+' or '-' or '#';
            if (starts && member.Length > 0)
            {
                items.Add(member.ToString());
                member.Clear();
            }

            member.Append(member.Length > 0 ? " " : "").Append(word);
            depth += word.Count(c => c == '(') - word.Count(c => c == ')');
        }

        if (member.Length > 0) items.Add(member.ToString());
        return items;
    }

    /// <summary>
    /// A class with its kind (its stereotype) and members; one already met in a relation keeps its place and takes
    /// them.
    /// </summary>
    private static void Declare(List<ClassType> types, string head, IReadOnlyList<string> body)
    {
        string? kind = body.Count > 0 && body[0].StartsWith("<<", StringComparison.Ordinal) ? body[0][2..^2].Trim() : null;
        var members = body.Skip(kind is null ? 0 : 1).Select(member => Generics(member.TrimStart('~'))).ToList();
        var type = new ClassType(IdOf(head), Generics(head), kind, members.Count == 0 ? null : members);
        int at = types.FindIndex(known => known.Id == type.Id);
        if (at < 0) types.Add(type);
        else types[at] = type;
    }

    /// <summary>A relation line: the first arrow between two names, the label after the colon as its word.</summary>
    private static ClassRelation? Relation(string line, List<ClassType> types)
    {
        int colon = line.IndexOf(" : ", StringComparison.Ordinal);
        string ends = colon < 0 ? line : line[..colon];
        string? word = colon < 0 ? null : MermaidLabel.Text(line[(colon + 3)..]);
        foreach (var (arrow, kind, reversed) in Arrows)
        {
            int at = ends.IndexOf($" {arrow} ", StringComparison.Ordinal);
            if (at < 0) continue;

            string left = Join(types, ends[..at].Trim()), right = Join(types, ends[(at + arrow.Length + 2)..].Trim());
            return reversed ? new ClassRelation(right, left, kind, word) : new ClassRelation(left, right, kind, word);
        }

        return null;
    }

    /// <summary>A class met first in a relation joins the chart there, named by its id.</summary>
    private static string Join(List<ClassType> types, string name)
    {
        string id = IdOf(name);
        if (types.All(known => known.Id != id)) types.Add(new ClassType(id, Generics(name)));
        return id;
    }

    /// <summary>A class's id: its name without its generics, as Mermaid's relations name it.</summary>
    private static string IdOf(string name)
    {
        int tilde = name.IndexOf('~');
        return tilde < 0 ? name : name[..tilde];
    }

    /// <summary>
    /// Mermaid's <c>~</c> generics as C# writes them: a <c>~</c> before a name opens one, any other closes one, so
    /// <c>Results~Ok~T~, Bad~</c> reads <c>Results&lt;Ok&lt;T&gt;, Bad&gt;</c>.
    /// </summary>
    private static string Generics(string text)
    {
        var written = new StringBuilder(text.Length);
        for (int at = 0; at < text.Length; at++)
        {
            if (text[at] != '~')
            {
                written.Append(text[at]);
                continue;
            }

            bool opens = at + 1 < text.Length && char.IsLetterOrDigit(text[at + 1]);
            written.Append(opens ? '<' : '>');
        }

        return written.ToString();
    }

    /// <summary>The line can't be read, said with the line.</summary>
    private static InvalidDataException Unread(string line) =>
        new(MessageText.Format(DocDiagramsUserText.UnreadMermaid, new { Line = line }));
}
