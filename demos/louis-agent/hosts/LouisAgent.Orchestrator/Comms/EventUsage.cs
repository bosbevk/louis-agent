using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Orchestrator.Comms;

/// <summary>
/// What one error event used (F3): the orchestrator's triage (its own requests), louis-agent's fix (the usage it
/// reported for each reply), and both together. A part is null when it wasn't measured: no triage ledger, or no fix
/// asked for.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal sealed record EventUsage(UsageTotal? Triage, UsageTotal? LouisAgent, UsageTotal? Total)
{
    /// <summary>Nothing measured at all.</summary>
    public static EventUsage None { get; } = new(null, null, null);

    /// <summary>
    /// Adds up one event: the triage's records and the fix's replies. A triage whose requests all failed (timed out,
    /// say) measured nothing, so it shows "-", not "0 · price unknown".
    /// </summary>
    public static EventUsage Of(IReadOnlyList<UsageRecord>? triage, IReadOnlyList<UsageTotal> fixReplies)
    {
        UsageTotal? triageTotal = triage is null || triage.Count == 0 ? null : UsageReport.Total("triage", triage);
        UsageTotal? fixTotal = fixReplies.Count == 0 ? null : UsageReport.Sum("louis-agent", fixReplies);
        UsageTotal[] parts = new[] { triageTotal, fixTotal }.OfType<UsageTotal>().ToArray();
        return new EventUsage(triageTotal, fixTotal, parts.Length == 0 ? null : UsageReport.Sum("total", parts));
    }
}
