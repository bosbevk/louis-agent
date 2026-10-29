using LouisAgent.Choices;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Start;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>
/// Builds the run rows the admin pages read: a stored run from the store, and a live run from the runner.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see a stored or a live run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class AdminRunReplies
{
    /// <summary>A stored run's row.</summary>
    extension(StoredRunRow)
    {
        /// <summary>The row for a run the store kept.</summary>
        public static StoredRunRow Of(EvalDocuments.Run r) => new(
            r.Id,
            r.Check,
            r.Repeat,
            r.Status.Wire(),
            r.ExitCode,
            r.StartedAt,
            r.EndedAt,
            r.Total is null ? null : new RunTotalRow(r.Total.Cost, r.Total.Currency, r.Total.Tokens, r.Total.Requests),
            r.StoreError,
            r.Models?.Select(m => new RunModelRow(m.Mode, m.Level.Wire(), m.Role, m.Model)).ToList(),
            r.Eval,
            r.EvalVersion,
            r.Commit,
            r.RescoreOf,
            r.Graders?.Select(g => new RunGraderRow(g.Name, g.Version, g.Kind.Wire(), g.Question)).ToList(),
            EvalSettings.ReplayOf(r.Settings));
    }

    /// <summary>A live run's row.</summary>
    extension(LiveRunRow)
    {
        /// <summary>
        /// The row for the run the runner holds, with whether its recording kept everything once done.
        /// </summary>
        public static LiveRunRow Of(RunningEval run, EvalRecorder.Result? stored = null) => new(
            run.Id,
            run.Check,
            run.Repeat,
            run.Status.Wire(),
            run.ExitCode,
            run.StartedAt,
            run.EndedAt,
            run.Lines.Count,
            stored?.Stored,
            stored?.StoreError,
            run.Calibration?.Of,
            run.Calibration?.Grader,
            // A judge check's lines aren't streamed to a page, so its last one (the runner's refusal, if it refused)
            // rides on the row; a run's doesn't.
            run.Calibration is null ? null : run.Lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l)));
    }
}
