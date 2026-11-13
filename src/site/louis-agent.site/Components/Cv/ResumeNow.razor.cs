using LouisAgent.Site.Resumes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// The CV's "now" band (F142): the project Louis is building as proof of work, drawn from the CV's own data (his pick
/// of three), so the admin's editor changes it and the guide reads the same words.
/// </summary>
public partial class ResumeNow
{
    /// <summary>The project.</summary>
    [Parameter, EditorRequired]
    public ResumeProject Project { get; set; } = null!;

    /// <summary>Whether a link leaves the site: a place on the site starts with a slash.</summary>
    private static bool Leaves(string url) => !url.StartsWith('/');

    /// <summary>A link that leaves the site opens a new tab.</summary>
    private static string? TargetOf(string url) => Leaves(url) ? "_blank" : null;

    /// <summary>A link that leaves the site tells the other site nothing about this one.</summary>
    private static string? RelOf(string url) => Leaves(url) ? "noopener" : null;
}
