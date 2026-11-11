using LouisAgent.Diagrams.Flow;

namespace LouisAgent.Site.Graphs;

/// <summary>
/// The Built-on-it page's diagram (F111): the framework, and the three demos that stand on it, each the way a
/// customer's own project would. A demo's id is its folder's path under <c>demos/</c>, which
/// <c>DemosGraphTests</c> holds.
/// </summary>
public static class DemosGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("framework", "The framework", "the engine, the hosts, the web, the packs", 0),
            new GraphNode("demos/puls", "Puls", "the South African assistant", 1),
            new GraphNode("demos/louis-agent", "The coding agent", "and this site, with its guide", 1),
            new GraphNode("demos/starter", "The starter", "the smallest site, from a template", 1)
        ],
        [
            new GraphEdge("framework", "demos/puls"),
            new GraphEdge("framework", "demos/louis-agent"),
            new GraphEdge("framework", "demos/starter")
        ]);
}
