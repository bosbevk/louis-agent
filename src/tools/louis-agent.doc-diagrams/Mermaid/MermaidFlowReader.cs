using LouisAgent.Diagrams;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.DocDiagrams.Mermaid;

/// <summary>
/// Reads a Mermaid flowchart into a flow diagram, once, so the docs keep no Mermaid (CL60.2): its direction, its boxes
/// with their labels, its arrows with their words and dashes, and its subgraphs as groups.
/// </summary>
/// <remarks>
/// What the drawing has no use for is left out: styles, classes, clicks and comments. A box stands in the innermost
/// subgraph it is first named in, so a nested subgraph's outer frame goes, and an arrow to a subgraph goes to its
/// first box, since a flow's arrows join boxes.
/// </remarks>
public static class MermaidFlowReader
{
    /// <summary>The lines that say how the chart looks, which the drawing doesn't need.</summary>
    private static readonly string[] Skipped =
        ["%%", "classDef", "class ", "style ", "linkStyle", "click ", "direction "];

    /// <summary>The flowchart as a flow diagram with this title.</summary>
    public static DiagramSource Read(string block, string title)
    {
        var lines = block.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim()).ToList();
        var direction = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1) is "LR" or "RL"
            ? FlowDirection.Right
            : FlowDirection.Down;

        var boxes = new Boxes();
        var arrows = new List<GraphEdge>();
        var groups = new List<GraphGroup>();
        var open = new Stack<string>();
        foreach (string line in lines.Skip(1).Where(line => line.Length > 0 && !Skipped.Any(line.StartsWith)))
        {
            if (line == "end")
            {
                open.TryPop(out _);
                continue;
            }

            if (line.StartsWith("subgraph", StringComparison.Ordinal))
            {
                var group = Header(line["subgraph".Length..].Trim());
                groups.Add(group);
                open.Push(group.Id);
                continue;
            }

            // A chain: every box named, and an arrow from each box of a step to each of the next.
            var (steps, links) = new MermaidChain(line).Read();
            foreach (var box in steps.SelectMany(step => step)) boxes.Name(box, open.TryPeek(out var top) ? top : null);
            for (int at = 0; at < links.Count; at++)
                arrows.AddRange(from a in steps[at]
                                from b in steps[at + 1]
                                select new GraphEdge(a.Id, b.Id, links[at].Word, links[at].Style));
        }

        // A chart wider than a doc's column would shrink its words to nothing; turned the other way it is often
        // narrow enough, so the narrower way wins (CL60.2, Claude's pick while unattended).
        var flow = Worded(boxes.Flow(arrows, groups, direction));
        int width = GraphLayout.Place(flow).Width;
        if (width <= WidestAcross) return new DiagramSource(DiagramKind.Flow, title, Flow: flow);
        var turned = Worded(flow with
        {
            Direction = flow.Direction == FlowDirection.Right ? FlowDirection.Down : FlowDirection.Right
        });
        return new DiagramSource(DiagramKind.Flow, title,
            Flow: GraphLayout.Place(turned).Width < width ? turned : flow);
    }

    /// <summary>The widest a chart may be and still read in a doc's column, in the SVG's pixels.</summary>
    private const int WidestAcross = 1100;

    /// <summary>The most characters a line of a down chart's arrow word may hold, to stand beside it.</summary>
    private const int WordWidth = 24;

    /// <summary>
    /// The flow with its arrows' words as a down chart draws them, wrapped into short lines that stand beside the
    /// arrow; a chart that runs right keeps them on one line, lying along it.
    /// </summary>
    private static GraphSpec Worded(GraphSpec flow) => flow with
    {
        Edges =
        [
            .. flow.Edges.Select(edge => edge with
            {
                Word = (edge.Word, flow.Direction) switch
                {
                    (null, _) => null,
                    ({ } word, FlowDirection.Down) => string.Join('\n',
                        TextWrap.Lines(word.Replace('\n', ' '), WordWidth)),
                    ({ } word, _) => word.Replace('\n', ' ')
                }
            })
        ]
    };

    /// <summary>The most characters a label's line may hold, so a box stays narrow enough to stand beside others.
    /// </summary>
    private const int LabelWidth = 30;

    /// <summary>A label with each of its lines wrapped at its words.</summary>
    private static string Wrapped(string label) =>
        string.Join('\n', label.Split('\n').SelectMany(line => TextWrap.Lines(line, LabelWidth)));

    /// <summary>A subgraph's header as its group: an id and a label, or words that are both.</summary>
    private static GraphGroup Header(string header)
    {
        var box = new MermaidChain(header).Box();
        if (box.Label is { } label) return new GraphGroup(box.Id, label.Replace('\n', ' '));
        return header.Contains(' ')
            ? new GraphGroup(new string([.. header.Where(char.IsLetterOrDigit)]), header)
            : new GraphGroup(box.Id, box.Id);
    }

    /// <summary>The boxes as they are named, in order: each one's label, and the group it was first named in.</summary>
    private sealed class Boxes
    {
        private readonly List<string> _order = [];
        private readonly Dictionary<string, string?> _labels = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _groups = new(StringComparer.Ordinal);

        /// <summary>
        /// A box named: added the first time, its label and group filled in when a later mention gives them.
        /// </summary>
        public void Name(MermaidChain.Mention box, string? group)
        {
            if (!_labels.ContainsKey(box.Id))
            {
                _order.Add(box.Id);
                _labels[box.Id] = null;
                _groups[box.Id] = null;
            }

            _labels[box.Id] ??= box.Label;
            _groups[box.Id] ??= group;
        }

        /// <summary>
        /// The flow: a name that is a group's is no box, an arrow to it goes to the group's first box, and a group with
        /// no box is left out.
        /// </summary>
        public GraphSpec Flow(List<GraphEdge> arrows, List<GraphGroup> groups, FlowDirection direction)
        {
            var groupIds = groups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);
            var nodes = _order.Where(id => !groupIds.Contains(id))
                .Select(id => new GraphNode(id, Wrapped(_labels[id] ?? id), Group: _groups[id])).ToList();
            string? First(string group) => nodes.FirstOrDefault(node => node.Group == group)?.Id;
            string? Box(string id) => groupIds.Contains(id) ? First(id) : id;

            var joined = arrows.Select(arrow => (arrow, from: Box(arrow.From), to: Box(arrow.To)))
                .Where(pair => pair.from is not null && pair.to is not null)
                .Select(pair => pair.arrow with
                {
                    From = pair.from!,
                    To = pair.to!
                }).ToList();
            var kept = groups.Where(group => nodes.Any(node => node.Group == group.Id)).ToList();
            return new GraphSpec(nodes, joined, direction, kept.Count == 0 ? null : kept);
        }
    }
}
