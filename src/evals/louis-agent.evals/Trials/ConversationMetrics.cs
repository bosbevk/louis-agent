using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// Adds up what a conversation's turns did into what the conversation measured. A run and a re-score build it the
/// same way, so a re-score's numbers are the run's. The tokens and the cost are the ledger's, so they stay unknown.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class ConversationMetrics
{
    /// <summary>The calls added up, each site once in the order it was first read, and one turn per fact.</summary>
    public static Metrics Of(IReadOnlyList<TurnFacts> facts, double seconds) =>
        new(facts.Sum(f => f.AgentCalls), facts.Sum(f => f.SpecialistCalls),
            [.. facts.SelectMany(f => f.Hosts).Distinct(StringComparer.Ordinal)], facts.Count, null, null, seconds,
            null);
}
