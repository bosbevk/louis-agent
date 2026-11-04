using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Admin.Verdict;

/// <summary>
/// The run's answer in a few numbers, worked out from the run and its eval's trend before any markup: per mode the
/// passed of the graded and the change against the run before, the run's cost and requests, and how many answers
/// still wait for a person's grade.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler): the numbers live in a plain record with its own tests, and the component only places
/// them, so the sums are checked without rendering and the markup stays free of arithmetic. The run before is the
/// trend's latest point of the mode that started earlier than this run, on any of the mode's lines (a line is a
/// mode on one model), because "did it get better since last time?" is asked of the eval, not of a model.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0030-build-or-adopt.md">ADR 0030: build or adopt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record RunVerdictView(IReadOnlyList<ModeVerdict> Modes, int ToGrade, decimal? Cost, int? Requests)
{
    /// <summary>Works the verdict out of the run and its eval's trend (none when the trend couldn't be read).</summary>
    public static RunVerdictView Of(StoredRunReply run, TrendReply? trend)
    {
        var graders = RunGraders.Of(run.Run).Graders;
        var modes = run.Conversations.GroupBy(c => c.Mode).Select(mode => ModeOf(mode, run.Run.StartedAt, trend));
        int toGrade = run.Conversations.Count(c => BlindReview.ReviewOf(graders, ConversationGrades.Of(c)) is not null);
        return new RunVerdictView([.. modes], toGrade, run.Run.Total?.Cost, run.Run.Total?.Requests);
    }

    /// <summary>One mode's passed of graded, against the mode's run before on the trend.</summary>
    private static ModeVerdict ModeOf(IGrouping<string, ConversationRow> mode, DateTimeOffset startedAt,
        TrendReply? trend)
    {
        // Graded as the trend counts: an infrastructure failure is neither a pass nor a fail, and one still
        // without an outcome isn't graded yet.
        var outcomes = mode.Select(c => SettingChoice.TryParse(c.Outcome, out TrialOutcome? o) ? o : null).ToList();
        int total = outcomes.Count(o => o is not (null or TrialOutcome.Infrastructure));
        int passed = outcomes.Count(o => o == TrialOutcome.Pass);

        var before = Before(mode.Key, startedAt, trend);
        int? beforePassed = before?.Score is { } score
            ? (int)Math.Round(score * before.Conversations, MidpointRounding.AwayFromZero)
            : null;
        return new ModeVerdict(mode.Key, passed, total, passed - beforePassed, before is null);
    }

    /// <summary>The mode's latest trend point from before this run started, on any of the mode's lines.</summary>
    private static TrendPointRow? Before(string mode, DateTimeOffset startedAt, TrendReply? trend) =>
        (trend?.Modes ?? [])
        .Where(line => line.Name == mode || line.Name.StartsWith(mode + " (", StringComparison.Ordinal))
        .SelectMany(line => line.Points)
        .Where(point => point.At < startedAt)
        .OrderBy(point => point.At)
        .LastOrDefault();
}
