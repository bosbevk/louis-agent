using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Recording;

/// <summary>
/// Puts the ledger's numbers into what a conversation measured: the tokens it sent and got back, and what it cost.
/// Only the agent's own requests count; a judge's grading is the grader's cost, not the conversation's.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class ConversationUsage
{
    /// <summary>
    /// The metrics with the conversation's tokens and cost filled in from its requests. With no request of its own the
    /// metrics stay as the runner wrote them; a number the ledger didn't know for any request leaves the total unknown,
    /// never 0.
    /// </summary>
    public static Metrics With(Metrics metrics, IEnumerable<Request> requests)
    {
        var own = requests.Where(r => r.Purpose != UsagePurpose.Grading).ToList();
        if (own.Count == 0) return metrics;

        return metrics with
        {
            // What it sent: the prompt's tokens, cached or not.
            TokensIn = own.Any(r => r.InputTokens is null)
                ? null
                : own.Sum(r => r.InputTokens!.Value + (r.CacheReadTokens ?? 0) + (r.CacheWriteTokens ?? 0)),
            TokensOut = own.Any(r => r.OutputTokens is null) ? null : own.Sum(r => r.OutputTokens!.Value),
            Cost = own.Any(r => r.Cost is null) ? null : own.Sum(r => r.Cost!.Value),
        };
    }
}
