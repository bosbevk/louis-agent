using LouisAgent.Site.Resumes.History;
using LouisAgent.Site.Resumes.Skills;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.More;

/// <summary>
/// The CV's "Also" (F142): the certificates with a link to check (Betterview, the IQ society), and the interests
/// outside work, side by side.
/// </summary>
public partial class ResumeAlso
{
    /// <summary>The certificates with a link to check.</summary>
    [Parameter]
    public IReadOnlyList<ResumeCertificate> Certificates { get; set; } = [];

    /// <summary>What he does outside work.</summary>
    [Parameter]
    public IReadOnlyList<ResumeInterest> Interests { get; set; } = [];

    /// <summary>An interest, and what about it in brackets when the file says ("Chess (Openings)").</summary>
    private static string LineOf(ResumeInterest interest) => interest.Keywords.Count > 0
        ? $"{interest.Name} ({string.Join(", ", interest.Keywords)})"
        : interest.Name;
}
