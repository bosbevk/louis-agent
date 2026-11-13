using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.History;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// Louis's CV, drawn from the record the site reads from <c>cv.json</c> (F142): the hero and facts, what he's
/// building now, the jobs, the skills, the schooling and the rest, each a band the guide can show.
/// </summary>
/// <remarks>
/// Composite View: one component per section, each handed only its part of the record, so a section is changed and
/// tested on its own; Template View (Fowler) inside each, markup with holes the record fills. The page owns what isn't
/// the CV's data (the photo, where he'd work, its closing words), so the data stays the one source the admin's
/// editor changes and the guide reads.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaCatalog/templateView.html">Template View (Fowler)</seealso>
public partial class ResumeView
{
    /// <summary>The CV.</summary>
    [Parameter, EditorRequired]
    public Resume Resume { get; set; } = default!;

    /// <summary>Where the photo is served from; with none, there's no photo.</summary>
    [Parameter]
    public string? Photo { get; set; }

    /// <summary>The page's line on where he'd work, such as "open to remote and hybrid".</summary>
    [Parameter]
    public string? Availability { get; set; }

    /// <summary>The page's own closing words, under the last band.</summary>
    [Parameter]
    public RenderFragment? End { get; set; }

    /// <summary>The certificates with no link to check, which sit with the schooling.</summary>
    private IReadOnlyList<ResumeCertificate> Unchecked => [.. Resume.Certificates.Where(cert => cert.Url is null)];

    /// <summary>The certificates with a link to check, shown under "Also".</summary>
    private IReadOnlyList<ResumeCertificate> Checkable => [.. Resume.Certificates.Where(cert => cert.Url is not null)];
}
