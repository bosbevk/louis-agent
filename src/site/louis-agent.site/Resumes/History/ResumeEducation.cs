namespace LouisAgent.Site.Resumes.History;

/// <summary>One school or course on the CV (F142): where, what, and when.</summary>
/// <param name="Institution">The school, college or university.</param>
/// <param name="Area">What was studied.</param>
/// <param name="StudyType">The kind of study, such as Diploma.</param>
/// <param name="StartDate">When it started, a year or a year and month.</param>
/// <param name="EndDate">When it ended.</param>
public sealed record ResumeEducation(
    string Institution,
    string? Area = null,
    string? StudyType = null,
    string? StartDate = null,
    string? EndDate = null);
