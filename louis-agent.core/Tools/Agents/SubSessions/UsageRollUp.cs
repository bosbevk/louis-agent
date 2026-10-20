namespace LouisAgent.Core.Tools.Agents.SubSessions;

using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;

/// <summary>A sub-session's reported usage as one row of the parent's ledger and turn (F5; S, CL15, from SpecialistTools).</summary>
internal sealed class UsageRollUp(IUsageSink? usageSink, TimeProvider time)
{
    /// <summary>
    /// One record standing for the child's requests on this reply, written to the ledger and added to the parent turn's
    /// scope, in that order, as <c>UsageRecordingChatClient.Store</c> does for a model request. Every view totals the
    /// scope, so the usage line, the chat total, <c>/usage</c> and the API's usage include it with no change of their
    /// own; the ledger's row keeps the child's cost attributable to this session later (its own rows are in the ledger it
    /// ran on, linked by <c>parent</c>). A minion's roll-up (F30) lands in the specialist's turn the same way, so the
    /// specialist's reported usage carries it up to the chat's roll-up: two levels, one rule.
    /// </summary>
    public void Record(UsageScope? scope, string model, SubSessionUsage usage, TimeSpan duration)
    {
        var record = new UsageRecord(
            At: time.GetUtcNow(),
            Round: 0,
            Purpose: UsagePurpose.SubSession,
            Host: scope?.Host,
            Session: scope?.Session,
            Turn: scope?.Turn,
            Task: scope?.Tags.Task,
            Run: scope?.Tags.Run,
            Service: scope?.Tags.Service,
            Model: model,
            Tokens: new UsageTokens(usage.Input, usage.CacheWrite, usage.CacheRead, usage.Output, null),
            DurationMs: (long)duration.TotalMilliseconds,
            Stop: UsagePurpose.SubSession,
            // The charged amount, in the child's ledger currency; its price table isn't sent, so none is named.
            Cost: usage.Cost is { } cost ? new UsageCost(usage.Currency ?? "USD", cost, null) : null,
            Parent: scope?.Tags.Parent,
            Requests: usage.Requests);
        // Not scope?.Add(usageSink?.Record(record)): the ledger row is written whether or not a scope is open (FX5).
        UsageRecord stored = usageSink?.Record(record) ?? record;
        scope?.Add(stored);
    }
}
