using LouisAgent.Site.Resumes.History;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.History;

/// <summary>
/// The CV's jobs (F142), newest first, as a timeline: the dates on the left (a job that hasn't ended marked current),
/// the role, the company and what was shipped there on the right.
/// </summary>
public partial class ResumeJobs
{
    /// <summary>The jobs, in the file's order, newest first.</summary>
    [Parameter]
    public IReadOnlyList<ResumeWork> Work { get; set; } = [];

    /// <summary>The company, and the job's summary after it when it has one ("Kohde · eCommerce").</summary>
    private static string OrgOf(ResumeWork job) => job.Summary is { } summary ? $"{job.Name} · {summary}" : job.Name;
}
