using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Site.Graphs.Framework;

/// <summary>
/// The Build-your-own page's diagram (F111): your program names a catalogue and starts a host, and the host runs the
/// engine with what the program handed it besides.
/// </summary>
public static class ProgramGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("program", "Your program", "a project of your own", 0),
            new GraphNode("catalogue", "The catalogue", "your identities and packs", 1),
            new GraphNode("extras", "What else it gets", "the web, a tracer, the providers", 1),
            new GraphNode("host", "A host", "the CLI, Rider, the API or MCP", 2),
            new GraphNode("engine", "The engine", "the loop, the guards, the ledger", 3)
        ],
        [
            new GraphEdge("program", "catalogue", "names"),
            new GraphEdge("program", "extras", "hands"),
            new GraphEdge("catalogue", "host"),
            new GraphEdge("extras", "host"),
            new GraphEdge("host", "engine", "runs")
        ]);
}
