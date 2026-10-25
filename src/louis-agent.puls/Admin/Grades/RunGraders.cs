namespace LouisAgent.Puls.Admin.Grades;

/// <summary>What a run recorded of its eval: the eval, its version, the commit and its graders in the run's order; each null for a run from before F18 step 2a.</summary>
public sealed record RunGraders(string? Eval, int? Version, string? Commit, IReadOnlyList<GraderRow>? Graders);
