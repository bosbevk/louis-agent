using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Site.Graphs.Framework;

/// <summary>
/// The Packs page's diagram (F111): a pack, what it holds, what a session loads from it, and the engine's guards and
/// MCP on the far side. The shape of every pack, not the list of them, which the page gives in words.
/// </summary>
public static class PacksGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("pack", "A pack", "one package, only the contracts", 0),
            new GraphNode("toolsets", "Its toolsets", "one or more, each named", 1),
            new GraphNode("guide", "Its guide", "the file the model reads", 1),
            new GraphNode("tools", "Tools", "typed arguments, never a shell", 2),
            new GraphNode("session", "A session", "loads only the toolsets it names", 2),
            new GraphNode("guards", "The engine's guards", "the fetcher, the browser, the context", 3),
            new GraphNode("mcp", "MCP", "the same tools, served as they are", 3)
        ],
        [
            new GraphEdge("pack", "toolsets"),
            new GraphEdge("pack", "guide"),
            new GraphEdge("toolsets", "tools"),
            new GraphEdge("toolsets", "session"),
            new GraphEdge("guide", "session", "read"),
            new GraphEdge("tools", "guards", "through"),
            new GraphEdge("tools", "mcp", "served")
        ]);
}
