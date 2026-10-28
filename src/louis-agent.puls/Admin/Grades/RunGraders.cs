using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Grades;

/// <summary>
/// What a run recorded of its eval: the eval, its version, the commit and its graders in the run's order; each null
/// for a run from before F18 step 2a.
/// </summary>
public sealed record RunGraders(string? Eval, int? Version, string? Commit, IReadOnlyList<GraderRow>? Graders)
{
    /// <summary>The eval and graders as the contract's run row carries them.</summary>
    /// <remarks>Fowler's <i>Replace Constructor with Factory Function</i>, the guide's <c>Record.Of</c>.</remarks>
    public static RunGraders Of(StoredRunRow run) =>
        new(run.Eval, run.EvalVersion, run.Commit, run.Graders?.Select(GraderRow.Of).ToArray());
}
