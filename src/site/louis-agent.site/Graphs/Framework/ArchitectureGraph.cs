using LouisAgent.Web.Graphs;

namespace LouisAgent.Site.Graphs.Framework;

/// <summary>
/// The Architecture page's diagram (F111): the repository's groups as the solution file lays them out, and which rests
/// on which. The engine first, the libraries on it, the packs beside, the demos and the framework's own site (F124)
/// built on all of it.
/// </summary>
public static class ArchitectureGraph
{
    /// <summary>The boxes and arrows; a box's id is its folder in the solution file.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("src/engine", "src/engine", "core, and what sits on it", 0),
            new GraphNode("src/hosts", "src/hosts", "the hosts, as libraries", 1),
            new GraphNode("src/web", "src/web", "the framework's web", 1),
            new GraphNode("src/evals", "src/evals", "the harness and its runner", 1),
            new GraphNode("src/tools", "src/tools", "a developer's tools", 1),
            new GraphNode("packs", "packs", "tools, evals, web and tests", 1),
            new GraphNode("demos", "demos", "louis-agent, puls, starter", 2),
            new GraphNode("src/site", "src/site", "the framework's own site", 2),
        ],
        [
            new GraphEdge("src/engine", "src/hosts"),
            new GraphEdge("src/engine", "src/web"),
            new GraphEdge("src/engine", "src/evals"),
            new GraphEdge("src/engine", "src/tools"),
            new GraphEdge("src/engine", "packs", "contracts"),
            new GraphEdge("src/hosts", "demos"),
            new GraphEdge("src/web", "demos"),
            new GraphEdge("packs", "demos", "named"),
            new GraphEdge("src/hosts", "src/site"),
            new GraphEdge("src/web", "src/site"),
            new GraphEdge("packs", "src/site", "named"),
        ]);
}
