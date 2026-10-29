using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Admin.Explorer.Rings;

/// <summary>A tool's reason for failing and how often it came up, for the note under the tools ring.</summary>
public sealed record ToolFailure(string Tool, string Reason, int Count)
{
    /// <summary>
    /// The commonest reason a tool failed over some turns, so a run where every search was refused shows it at a
    /// glance; nothing when no tool failed.
    /// </summary>
    public static ToolFailure? Commonest(IEnumerable<TurnNode> turns) =>
        turns.SelectMany(t => t.ToolReasons).GroupBy(r => (r.Tool, r.Reason))
            .Select(g => new ToolFailure(g.Key.Tool, g.Key.Reason, g.Sum(r => r.Count)))
            .OrderByDescending(r => r.Count).FirstOrDefault(r => r.Count > 0);
}
