using LouisAgent.Evals.Contracts.Runs;

namespace LouisAgent.Evals.Contracts.Api;

/// <summary>A run in short, for a list: what it was, where it is, and how many of its conversations passed.</summary>
/// <param name="Id">The run's id.</param>
/// <param name="Kind">Live, or a replay.</param>
/// <param name="Status">Where the run is.</param>
/// <param name="Repeats">How many times each conversation is played.</param>
/// <param name="StartedAt">When it started, in UTC.</param>
/// <param name="EndedAt">When it ended, in UTC, once it has.</param>
/// <param name="Passed">How many of its conversations passed.</param>
/// <param name="Conversations">How many conversations it has graded.</param>
/// <param name="Cost">What its agent's turns cost, the known ones added, or null when none had a price.</param>
/// <param name="JudgeCost">What judging its turns cost, the known ones added, or null when none is known.</param>
/// <param name="Currency">The currency its costs are in, or null when nothing was priced.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record RunRow(
    string Id,
    RunKind Kind,
    RunStatus Status,
    int Repeats,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int Passed,
    int Conversations,
    decimal? Cost = null,
    decimal? JudgeCost = null,
    string? Currency = null)
{
    /// <summary>The run a re-grade re-graded, or none (F126.4).</summary>
    public string? RegradeOf { get; init; }

    /// <summary>
    /// Each criterion the run missed or a person flipped, once, so an eval's history adds them up from its rows and
    /// reads no run whole (Louis's pick, 2026-11-23, "On each run's row in the list").
    /// </summary>
    public IReadOnlyList<CriterionTally> Criteria { get; init; } = [];

    /// <summary>The run, in short.</summary>
    public static RunRow Of(EvalRun run) => new(run.Id, run.Kind, run.Status, run.Repeats, run.StartedAt, run.EndedAt,
        run.Passed, run.Conversations.Count, run.SumCost(), run.SumJudgeCost(), run.FindCurrency())
    {
        RegradeOf = run.RegradeOf,
        Criteria = run.TallyCriteria()
    };
}
