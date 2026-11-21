namespace LouisAgent.Site.Resumes.Basics;

/// <summary>Who the CV is about and how to reach him (F142), JSON Resume's <c>basics</c>.</summary>
/// <param name="Name">His name.</param>
/// <param name="Label">What he does, in a few words.</param>
/// <param name="Email">His email address.</param>
/// <param name="Phone">His phone number, as written.</param>
/// <param name="Url">His own site.</param>
/// <param name="Summary">Who he is, in two or three sentences.</param>
/// <param name="Location">Where he lives, never a street address.</param>
/// <param name="Profiles">His profiles elsewhere: LinkedIn, GitHub.</param>
public sealed record ResumeBasics(
    string Name,
    string? Label = null,
    string? Email = null,
    string? Phone = null,
    string? Url = null,
    string? Summary = null,
    ResumeLocation? Location = null,
    IReadOnlyList<ResumeProfile>? Profiles = null)
{
    /// <summary>His profiles, none when the file has none.</summary>
    public IReadOnlyList<ResumeProfile> Profiles { get; init; } = Profiles ?? [];
}
