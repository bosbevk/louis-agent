using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Markdown;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Tools.Results;

/// <summary>
/// Lists the rows of a tool's tables that came back with a mark in their last cell, so each run says what they were:
/// "2 marked ? by EstimatePrices: rotis 5, flaked almonds 100 g". It measures and never judges, so it only ever passes,
/// and a run's pass rate can't move because of it.
/// </summary>
/// <remarks>
/// A Specification over one tool's results, as <c>tool-used-with</c> is over its inputs, and general on purpose: the
/// harness knows a table and a mark, and the eval file names the tool and the mark, so no pack references the evals and
/// the harness names no product (ADR 0073). It is a check and not a log line because a pack's log lines hash what users
/// sent, and an eval's lists are test data, safe to name. The rows are told apart by their words, and a retried line
/// has other words, so a row the agent fixed on a retry still shows here.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0073-packs-reference-no-evals.md">ADR 0073: packs reference no evals</seealso>
public sealed class ToolRowsMarkedCheck(string tool, string mark) : ICheck
{
    /// <inheritdoc />
    public string Name => "tool-rows-marked";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => false;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Reads what the tool gave back in every turn, the specialists' calls too.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Count(trial.Transcript.Turns));

    /// <summary>
    /// Unknown when a turn broke, since half a conversation isn't the run's list; otherwise a pass that says what it
    /// found.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Count(IReadOnlyList<TurnTranscript> turns)
    {
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;

        var results = turns.SelectMany(turn => EngineEvents.ResultsOfTool(turn, tool)).ToList();
        if (results.Count == 0)
            return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ToolNotUsed, new { Tool = tool }));

        // The same row in two calls is one row: the list says what was marked, not how often it was asked for.
        var marked = results.SelectMany(MarkedRows).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (marked.Count == 0)
            return Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.ToolRowsMarkedNone, new
                {
                    Mark = mark,
                    Tool = tool
                }));

        return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ToolRowsMarked, new
        {
            marked.Count,
            Mark = mark,
            Tool = tool,
            Rows = string.Join(", ", marked)
        }));
    }

    /// <summary>The first cell of every table row in this result whose last cell is exactly the mark.</summary>
    private IEnumerable<string> MarkedRows(string result) =>
        Lists.Tables(result).SelectMany(table => table).Select(Lists.Cells)
            .Where(cells => cells.Count > 1 && cells[^1] == mark).Select(cells => cells[0]);

    /// <summary>What a file can set: the tool to read, and the mark a row's last cell holds.</summary>
    public sealed record Settings(string Tool, string Mark);

    /// <summary>The check as a file names it, <c>tool-rows-marked</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("tool-rows-marked", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a blank tool or mark, naming which.</summary>
        /// <remarks>Guard clauses, one blank setting each from the top (ADR 0051).</remarks>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            if (string.IsNullOrWhiteSpace(settings.Tool)) throw Refused(CheckCatalogMessage.NoText, "tool");
            if (string.IsNullOrWhiteSpace(settings.Mark)) throw Refused(CheckCatalogMessage.NoText, "mark");
            return new ToolRowsMarkedCheck(settings.Tool, settings.Mark);
        }
    }
}
