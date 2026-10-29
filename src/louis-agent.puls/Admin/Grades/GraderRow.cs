using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Grades;

/// <summary>
/// One of a run's graders, as the run row sends it: its name, its version (none when the row has none, never 0), its
/// kind (<c>FixedRule</c>, <c>Model</c>, <c>Human</c>) and, for a person's review, its question.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record GraderRow(string Name, int? Version, string Kind, string? Question = null)
{
    /// <summary>A grader as the contract's run row names it.</summary>
    /// <remarks>Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>.</remarks>
    public static GraderRow Of(RunGraderRow row) => new(row.Name, row.Version, row.Kind, row.Question);
}
