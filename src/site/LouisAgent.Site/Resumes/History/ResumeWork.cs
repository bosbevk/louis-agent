namespace LouisAgent.Site.Resumes.History;

/// <summary>
/// One job on the CV (F142): where, as what, when, and what was shipped there, never a list of duties.
/// </summary>
/// <param name="Name">The company.</param>
/// <param name="Position">The role.</param>
/// <param name="StartDate">When it started, as JSON Resume writes it: a year, or a year and month (2021-03).</param>
/// <param name="EndDate">When it ended; none while it goes on.</param>
/// <param name="Summary">The job in a sentence, when it needs one.</param>
/// <param name="Url">The company's site.</param>
/// <param name="Highlights">What was shipped, one line each.</param>
/// <param name="Keywords">The stack it was built with.</param>
public sealed record ResumeWork(
    string Name,
    string Position,
    string StartDate,
    string? EndDate = null,
    string? Summary = null,
    string? Url = null,
    IReadOnlyList<string>? Highlights = null,
    IReadOnlyList<string>? Keywords = null)
{
    /// <summary>What was shipped, none when the file says nothing.</summary>
    public IReadOnlyList<string> Highlights { get; init; } = Highlights ?? [];

    /// <summary>The stack, none when the file names none.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Keywords ?? [];
}
