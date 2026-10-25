namespace LouisAgent.Puls.Admin.Grades;

/// <summary>A grade's or a conversation's outcome as the admin API sends it (F18 step 4.5c). Unknown is shown and never read as a pass.</summary>
public enum GradeOutcome
{
    Pass,
    Fail,
    Unknown,
}
