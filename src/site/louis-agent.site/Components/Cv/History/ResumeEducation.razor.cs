using LouisAgent.Site.Resumes.History;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.History;

/// <summary>
/// The CV's schooling (F142): the certificates with no link to check (the 2026 AI ones), then each school or course
/// with its dates, what it was, and where.
/// </summary>
public partial class ResumeEducation
{
    /// <summary>The schools and courses, in the file's order.</summary>
    [Parameter]
    public IReadOnlyList<Resumes.History.ResumeEducation> Education { get; set; } = [];

    /// <summary>The certificates that sit with the schooling.</summary>
    [Parameter]
    public IReadOnlyList<ResumeCertificate> Certificates { get; set; } = [];

    /// <summary>The row's heading: what kind of study it was, else what was studied, else the school.</summary>
    private static string TitleOf(Resumes.History.ResumeEducation school) =>
        school.StudyType ?? school.Area ?? school.Institution;

    /// <summary>The line under it: the school, and what was studied when the heading named the kind.</summary>
    private static string WhereOf(Resumes.History.ResumeEducation school) =>
        school is { StudyType: not null, Area: { } area } ? $"{school.Institution} · {area}" : school.Institution;
}
