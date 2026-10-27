using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Contracts.Admin.Reviews;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>Builds a judge check's report row, in the agreement card's shape, from the report the store kept.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see the store's report.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class CalibrationReplies
{
    /// <summary>A report's row copies the stored report across, each class as the judge's three counts.</summary>
    extension(CalibrationReportRow)
    {
        /// <summary>The row for one stored judge check.</summary>
        public static CalibrationReportRow Of(CalibrationRecord c) => new(
            c.Run,
            c.Grader,
            c.JudgeModel,
            c.At,
            c.Agreed,
            c.Total,
            c.Bar,
            c.Passes,
            Counts(c.PersonPass),
            Counts(c.PersonFail),
            [
                .. c.Disagreements.Select(d =>
                    new DisagreementRow(d.Scenario, d.Mode, d.Repeat, d.Label, d.Judge, d.Reason))
            ]);
    }

    /// <summary>One class's counts, by what the judge said.</summary>
    private static CalibrationClassRow Counts(CalibrationClassCounts counts) =>
        new(counts.JudgePass, counts.JudgeFail, counts.JudgeUnknown);
}
