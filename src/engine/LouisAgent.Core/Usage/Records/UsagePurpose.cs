namespace LouisAgent.Core.Usage.Records;

/// <summary>Why a request was made. Strings, so the ledger stays readable as JSON.</summary>
public static class UsagePurpose
{
    public const string Turn = "turn";
    public const string Continue = "continue";
    public const string Summary = "summary";
    public const string Compaction = "compaction";
    public const string Triage = "triage";

    /// <summary>A model grader's request (F18 step 6): the judge's, tagged with the run alone, so the run's total includes it and no turn does.</summary>
    public const string Grading = "grading";

    /// <summary>
    /// Not a model request: a roll-up of a sub-session's requests (F5), added to the parent turn so its totals include
    /// the specialist's work, and written to the parent's ledger. Its <see cref="UsageRecord.Requests"/> says how many.
    /// </summary>
    public const string SubSession = "sub_session";
}
