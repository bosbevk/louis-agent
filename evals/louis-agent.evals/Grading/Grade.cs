namespace LouisAgent.Evals.Grading;

/// <summary>One grader's verdict on one trial: a score, its outcome, and the reason a person reads.</summary>
public sealed record Grade(string Grader, int Version, double Score, GradeOutcome Outcome, string Reason);
