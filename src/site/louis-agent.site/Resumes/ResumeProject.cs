namespace LouisAgent.Site.Resumes;

/// <summary>
/// What Louis is building now (F142), the CV's proof of work: its name, where to see it, the line that heads its band
/// on the page, what it is, and a panel for each part that proves something.
/// </summary>
/// <remarks>
/// Our own field beside JSON Resume's, which the standard allows. The panels are data (Louis's pick of three), so the
/// page draws them, the admin's editor changes them and the guide reads the same words.
/// </remarks>
/// <param name="Name">The project's name.</param>
/// <param name="Url">Where to see it working.</param>
/// <param name="Headline">The line that heads its band on the page.</param>
/// <param name="Description">What it is, in a sentence or two.</param>
/// <param name="Parts">A panel for each part of it, in the order shown.</param>
public sealed record ResumeProject(
    string Name,
    string? Url = null,
    string? Headline = null,
    string? Description = null,
    IReadOnlyList<ResumeProjectPart>? Parts = null)
{
    /// <summary>The panels, none when the file has none.</summary>
    public IReadOnlyList<ResumeProjectPart> Parts { get; init; } = Parts ?? [];
}
