namespace LouisAgent.Web.Admin.Verdict;

/// <summary>
/// One mode's line of the verdict: how many of its graded conversations passed, and how that compares with the
/// eval's run before (the difference in passed, none when that run has no score, and "first" when there is none).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record ModeVerdict(string Mode, int Passed, int Total, int? Change, bool First);
