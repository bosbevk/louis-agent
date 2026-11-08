using LouisAgent.Web.Graphs;

namespace LouisAgent.CodeReview.Graphs.Framework;

/// <summary>
/// The How-it-works page's diagram (F111): one turn, from your program through a host into the engine's pipeline,
/// out to the model and a pack's tool, and into the ledger. The pipeline's boxes are the real middleware in its real
/// order, outermost first, which <c>TurnGraphTests</c> holds as the film's test does.
/// </summary>
public static class TurnGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("program", "Your program", "a few lines of your own", 0),
            new GraphNode("host", "A host", "Rider, the API, the CLI or MCP", 1),
            new GraphNode("loop", "Tool loop + guards", null, 2),
            new GraphNode("usage", "Usage recording", null, 2),
            new GraphNode("cutoff", "Cut-off marking", null, 2),
            new GraphNode("tracing", "Tracing", null, 2),
            new GraphNode("replay", "Replay", null, 2),
            new GraphNode("caching", "Prompt caching", null, 2),
            new GraphNode("model", "The model", "any provider, by configuration", 3),
            new GraphNode("tool", "A pack's tool", "typed arguments, data not a shell", 3),
            new GraphNode("ledger", "The ledger", "one priced record per request", 4),
        ],
        [
            new GraphEdge("program", "host", "starts"),
            new GraphEdge("host", "loop", "a turn"),
            new GraphEdge("caching", "model"),
            new GraphEdge("loop", "tool", "calls"),
            new GraphEdge("usage", "ledger"),
        ]);
}
