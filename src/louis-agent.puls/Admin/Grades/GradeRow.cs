namespace LouisAgent.Puls.Admin.Grades;

/// <summary>One grader's verdict on a conversation, as the conversation row sends it; the version and the score null when the row has none, never 0 (ADR 0007).</summary>
public sealed record GradeRow(string Grader, int? Version, double? Score, GradeOutcome Outcome, string Reason);
