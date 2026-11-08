using LouisAgent.Web.Graphs;

namespace LouisAgent.CodeReview.Graphs;

/// <summary>
/// The Proof page's diagram (F111): where a number on the site comes from. An eval runs once, live, and records what
/// each call read, so a re-score costs nothing; every model request lands in the ledger, priced.
/// </summary>
public static class ProofGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("eval", "An eval", "a folder of files, in C#", 0),
            new GraphNode("run", "A run", "live, once, at a price", 1),
            new GraphNode("records", "Its records", "what every call read and said", 2),
            new GraphNode("rescore", "A re-score", "from the records, for $0", 3),
            new GraphNode("ledger", "The ledger", "every request, priced in rand", 3),
        ],
        [
            new GraphEdge("eval", "run", "runs"),
            new GraphEdge("run", "records", "writes"),
            new GraphEdge("records", "rescore", "replays"),
            new GraphEdge("records", "ledger"),
        ]);
}
