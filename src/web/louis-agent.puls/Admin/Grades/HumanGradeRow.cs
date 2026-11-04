namespace LouisAgent.Puls.Admin.Grades;

/// <summary>A person's grade of a conversation as the admin API sends it (F18 step 6a.3): the review it answers, the judge it is about, their verdict (pass, fail or unsure), note, name and time.</summary>
public sealed record HumanGradeRow(
    string Grader,
    string ModelGrader,
    string Verdict,
    string Note,
    string Reviewer,
    DateTimeOffset At);
