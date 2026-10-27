namespace LouisAgent.Evals.Grading;

/// <summary>
/// One grader's verdict on one trial: a score, its outcome, and the reason a person reads. A model grader's also says what judged (<see cref="Judge"/>); a code grader's
/// has none.
/// </summary>
public sealed record Grade(
    string Grader,
    int Version,
    double Score,
    GradeOutcome Outcome,
    string Reason,
    GradeJudge? Judge = null);

/// <summary>What judged a grade (F18 step 6): the judge's model, and the hash of the judge prompt it was given (a re-score with another prompt is not the same measure).</summary>
public sealed record GradeJudge(string Model, string PromptHash);
