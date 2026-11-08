using LouisAgent.Web.Graphs;

namespace LouisAgent.CodeReview.Graphs.Framework;

/// <summary>
/// The Identities page's diagram (F111): an identity, the five things it holds, and the level each one speaks at.
/// </summary>
/// <remarks>
/// Data beside the page's words, so <c>IdentityGraphTests</c> can hold the boxes to the parts the page lists and the
/// levels to the ones the engine knows.
/// </remarks>
public static class IdentityGraph
{
    /// <summary>The boxes and arrows.</summary>
    public static GraphSpec Spec { get; } = new(
        [
            new GraphNode("identity", "An identity", "who the agent is, as files", 0),
            new GraphNode("principles", "The principles", "first in every prompt", 1),
            new GraphNode("voice", "A voice", "who it is, how it speaks", 1),
            new GraphNode("roles", "Roles", "a specialist with fewer tools", 1),
            new GraphNode("small-jobs", "Minions", "a small job on a local model", 1),
            new GraphNode("packs", "The packs it names", "what its sessions may load", 1),
            new GraphNode("level0", "Level 0: the agent", "a session with no role", 2),
            new GraphNode("level1", "Level 1: a specialist", "a second session", 2),
            new GraphNode("level2", "Level 2: a minion", "the cheap part, handed off", 2),
        ],
        [
            new GraphEdge("identity", "principles"),
            new GraphEdge("identity", "voice"),
            new GraphEdge("identity", "roles"),
            new GraphEdge("identity", "small-jobs"),
            new GraphEdge("identity", "packs"),
            new GraphEdge("principles", "level0", "opens"),
            new GraphEdge("voice", "level0"),
            new GraphEdge("roles", "level1"),
            new GraphEdge("small-jobs", "level2"),
            new GraphEdge("packs", "level0", "tools"),
        ]);
}
