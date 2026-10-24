using LouisAgent.Evals.Puls.Transcripts;

namespace LouisAgent.Evals.Puls.Routing;

/// <summary>
/// The routing check's score (the Python's <c>routing/run_check.py</c> <c>score</c>): one question as numbers. Without delegation
/// (the baseline) answering itself is all it can do, so it's right unless it failed; with it, the role must be the kind's.
/// </summary>
public static class RoutingScorer
{
    /// <summary>The calls that hand a question on (the Python's stream.DELEGATIONS).</summary>
    private static readonly IReadOnlySet<string> Delegations = new HashSet<string> { "StartSubSession", "ContinueSubSession" };

    /// <summary>The role a question's kind goes to, or null where Puls answers it itself.</summary>
    public static string? ExpectedRole(string task) => RoutingSetup.Kinds[task.Split('-')[0]];

    /// <summary>Whether a mode can hand a question on: it has the specialists toolset, as the Python decides it (routing/run_check.py).</summary>
    public static bool Delegating(EvalMode mode) => mode.Toolsets.Contains("specialists", StringComparer.Ordinal);

    /// <summary>One agent with every toolset this instance has, but none it could delegate with.</summary>
    public static IReadOnlyList<string> Baseline(IEnumerable<string> available) =>
        available.Where(t => !RoutingSetup.DelegatingToolsets.Contains(t)).ToList();

    public static RoutingScores Score(string task, TurnView turn, bool delegating)
    {
        string? expected = delegating ? ExpectedRole(task) : null;
        return new RoutingScores(
            Role: turn.Role,
            ExpectedRole: expected,
            RoutedRight: turn.Error is null && turn.Role == expected,
            AnsweredItself: turn.Role is null && turn.Error is null,
            Delegations: turn.Calls.Count(c => Delegations.Contains(c.Name)),
            PulsCalls: turn.Calls.Count,
            SpecialistCalls: turn.SpecialistCalls.Count,
            Hosts: ToolHosts.Of(turn.Calls.Concat(turn.SpecialistCalls)),
            Errors: turn.Error is null ? 0 : 1);
    }
}
