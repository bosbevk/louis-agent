using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Resumes.History;
using LouisAgent.Site.Resumes.Skills;
using LouisAgent.Web.Components.Editing.Inline;
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

    /// <summary>
    /// Told the CV with one thing changed, when it is edited on this page (the admin's editor). With nothing
    /// listening, as on the public page, the CV is plain words with nothing to click.
    /// </summary>
    [Parameter]
    public EventCallback<Resume> ResumeChanged { get; set; }

    /// <summary>The top's listener: the CV with a changed top, when someone listens for the CV.</summary>
    private EventCallback<ResumeBasics> BasicsChanged =>
        ResumeChanged.Of<Resume, ResumeBasics>(this, basics => Resume with { Basics = basics });

    /// <summary>The facts' listener, the same way.</summary>
    private EventCallback<IReadOnlyList<ResumeFact>> FactsChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeFact>>(this, facts => Resume with { Facts = facts });

    /// <summary>The languages' listener: they are drawn in the top's line, and kept on the CV itself.</summary>
    private EventCallback<IReadOnlyList<ResumeLanguage>> LanguagesChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeLanguage>>(
            this, languages => Resume with { Languages = languages });

    /// <summary>The Now band's listener.</summary>
    private EventCallback<ResumeProject> NowChanged =>
        ResumeChanged.Of<Resume, ResumeProject>(this, now => Resume with { Now = now });

    /// <summary>The jobs' listener.</summary>
    private EventCallback<IReadOnlyList<ResumeWork>> WorkChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeWork>>(this, work => Resume with { Work = work });

    /// <summary>The skills' listener.</summary>
    private EventCallback<IReadOnlyList<ResumeSkill>> SkillsChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeSkill>>(this, skills => Resume with { Skills = skills });

    /// <summary>The schools' listener.</summary>
    private EventCallback<IReadOnlyList<ResumeEducation>> EducationChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeEducation>>(this, schools => Resume with { Education = schools });

    /// <summary>
    /// The listener for the certificates with no link, drawn with the schooling. The CV keeps one list of them all,
    /// so the changed ones are put back beside those with a link, which the last section draws.
    /// </summary>
    private EventCallback<IReadOnlyList<ResumeCertificate>> UncheckedChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeCertificate>>(
            this, changed => Resume with { Certificates = [.. changed, .. Checkable] });

    /// <summary>The listener for the certificates with a link, the same way round.</summary>
    private EventCallback<IReadOnlyList<ResumeCertificate>> CheckableChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeCertificate>>(
            this, changed => Resume with { Certificates = [.. Unchecked, .. changed] });

    /// <summary>The interests' listener.</summary>
    private EventCallback<IReadOnlyList<ResumeInterest>> InterestsChanged =>
        ResumeChanged.Of<Resume, IReadOnlyList<ResumeInterest>>(
            this, interests => Resume with { Interests = interests });

    /// <summary>The certificates with no link to check, which sit with the schooling.</summary>
    private IReadOnlyList<ResumeCertificate> Unchecked => [.. Resume.Certificates.Where(cert => cert.Url is null)];

    /// <summary>The certificates with a link to check, shown under "Also".</summary>
    private IReadOnlyList<ResumeCertificate> Checkable => [.. Resume.Certificates.Where(cert => cert.Url is not null)];
}
