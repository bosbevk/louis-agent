namespace LouisAgent.Site.Resumes;

/// <summary>
/// What Louis is building now (F142), the CV's proof of work: its name, where to see it, and why it counts.
/// </summary>
/// <param name="Name">The project's name.</param>
/// <param name="Url">Where to see it working.</param>
/// <param name="Description">What it is, in a sentence.</param>
/// <param name="Highlights">What it shows, one line each.</param>
public sealed record ResumeProject(
    string Name,
    string? Url = null,
    string? Description = null,
    IReadOnlyList<string>? Highlights = null)
{
    /// <summary>What it shows, none when the file says nothing.</summary>
    public IReadOnlyList<string> Highlights { get; init; } = Highlights ?? [];
}
