using System.Text;
using LouisAgent.Choices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Reports;

using static LouisAgent.Orchestrator.Comms.CommsMarkdown;

namespace LouisAgent.Orchestrator.Comms;

/// <summary>
/// The end of a run in the comms log: one row per error, the run's total split by agent (the figure to compare with
/// the usage ledger for the run), and the fix branches ready to review.
/// </summary>
/// <param name="events">The run's errors, in the order they were handled.</param>
/// <param name="currency">The currency the run was charged in.</param>
/// <param name="display">The currency costs are shown in (F14); null shows them as charged.</param>
internal sealed class RunSummary(IReadOnlyList<EventSummary> events, string currency, DisplayCurrency? display)
{
    /// <summary>The whole summary as Markdown, stamped with when the run finished.</summary>
    public string Write(DateTimeOffset finished)
    {
        var md = new StringBuilder();
        md.AppendLine().AppendLine("---").AppendLine().AppendLine("## Run summary").AppendLine();
        md.AppendLine($"Finished {finished:yyyy-MM-dd HH:mm:ss} UTC. {Outcomes()}.");
        md.AppendLine();
        md.AppendLine("| # | Request | Exception | Decision | louis-agent | Branch | Commit | Tokens | Cost |");
        md.AppendLine("|---|---------|-----------|----------|-------------|--------|--------|--------|------|");
        foreach (var e in events) md.AppendLine(Row(e));
        Totals(md);
        Branches(md);
        md.AppendLine();
        return md.ToString();
    }

    /// <summary>How many errors ended each way, as "1 fixed, 2 ignored".</summary>
    private string Outcomes() =>
        string.Join(", ",
            events.GroupBy(e => e.Outcome.Wire()).OrderBy(g => g.Key).Select(g => $"{g.Count()} {g.Key}"));

    /// <summary>One error's row of the table.</summary>
    private string Row(EventSummary e)
    {
        string agent = e.Messages == 0
            ? "-"
            : $"{e.Fix?.Status ?? "no result"} ({e.Messages} msg, {e.ToolCalls} tools)";
        return $"| {e.Number} | `{e.Error.Method} {e.Error.Arguments}` | {ShortType(e.Error.ExceptionType)} | " +
               $"**{e.Outcome.Wire()}** | {agent} | {Code(e.Fix?.Branch)} | {Code(e.Fix?.Commit)} | " +
               $"{Tokens(e.Usage.Total)} | {Cost(e.Usage.Total, currency, display)} |";
    }

    /// <summary>
    /// The run's total, split by agent, when anything was measured. A converted total says the rate it used and what
    /// was charged (F14), so it's never taken for the bill.
    /// </summary>
    private void Totals(StringBuilder md)
    {
        if (Sum("total", u => u.Total) is not { } runTotal) return;

        md.AppendLine().AppendLine($"**Run total:** {UsageText.Describe(runTotal, currency, display)} " +
                                   $"(triage {Cost(Sum("triage", u => u.Triage), currency, display)}, " +
                                   $"louis-agent {Cost(Sum("louis-agent", u => u.LouisAgent), currency, display)}).");
        if (display is not { Converts: true } || runTotal.Cost is not { } charged) return;

        md.AppendLine().AppendLine($"Shown in {display.Code} at {UsageText.Rate(display)} on {display.RateAsOf}; " +
                                   $"charged {UsageText.Money(charged, currency)} " +
                                   $"(the ledger, fixes.jsonl and decisions.jsonl keep {currency}).");
    }

    /// <summary>
    /// One part of every error's usage added up. A part no error measured (no fix asked for, say) is nothing, not the
    /// "price unknown" of an empty sum.
    /// </summary>
    private UsageTotal? Sum(string key, Func<EventUsage, UsageTotal?> part)
    {
        var parts = events.Select(e => part(e.Usage)).OfType<UsageTotal>().ToList();
        return parts.Count == 0 ? null : UsageReport.Sum(key, parts);
    }

    /// <summary>The branches of the errors that were fixed, for a person to review and merge.</summary>
    private void Branches(StringBuilder md)
    {
        var branches = events.Where(e => e.Outcome == TriageOutcome.Fixed)
            .Select(e => e.Fix?.Branch).OfType<string>().ToList();
        if (branches.Count == 0) return;

        md.AppendLine().AppendLine($"Branches ready to review and merge ({branches.Count}):").AppendLine();
        foreach (string branch in branches) md.AppendLine($"- `{branch}`");
    }
}
