namespace LouisAgent.Puls.Admin.Grades;

/// <summary>One grader's verdict on a conversation, as the conversation row sends it.</summary>
public sealed record GradeRow(string Grader, int Version, double Score, GradeOutcome Outcome, string Reason);
