using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Resumes.History;
using LouisAgent.Site.Resumes.Skills;

namespace LouisAgent.Site.Resumes;

/// <summary>
/// Louis's CV (F142), as the site keeps it in a JSON Resume file: who he is, the facts row, what he's building now, his
/// jobs, schooling, skills, certificates, languages and interests. The page draws it, and (F143) the guide reads it.
/// </summary>
/// <remarks>
/// Single source of truth: one file, read once into this record, that the page and the guide's tool both read, so they
/// never disagree. The shape is JSON Resume's (jsonresume.org, an open MIT standard, ADR 0030), so the file works with
/// other CV tools too; <see cref="Facts"/> and <see cref="Now"/> are our own, which the standard allows. It is the
/// site's, not the framework's (Louis: "this belongs to .site only").
/// </remarks>
/// <param name="Basics">Who he is and how to reach him.</param>
/// <param name="Facts">The few numbers that say the most, drawn in a row under the name.</param>
/// <param name="Now">What he's building now, the proof of work.</param>
/// <param name="Work">The jobs, newest first.</param>
/// <param name="Education">The schooling, newest first.</param>
/// <param name="Skills">The skills, grouped, never rated.</param>
/// <param name="Certificates">The certificates.</param>
/// <param name="Languages">The languages spoken.</param>
/// <param name="Interests">What he does outside work.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record Resume(
    ResumeBasics Basics,
    IReadOnlyList<ResumeFact>? Facts = null,
    ResumeProject? Now = null,
    IReadOnlyList<ResumeWork>? Work = null,
    IReadOnlyList<ResumeEducation>? Education = null,
    IReadOnlyList<ResumeSkill>? Skills = null,
    IReadOnlyList<ResumeCertificate>? Certificates = null,
    IReadOnlyList<ResumeLanguage>? Languages = null,
    IReadOnlyList<ResumeInterest>? Interests = null)
{
    /// <summary>The facts row, none when the file has none.</summary>
    public IReadOnlyList<ResumeFact> Facts { get; init; } = Facts ?? [];

    /// <summary>The jobs, none when the file has none.</summary>
    public IReadOnlyList<ResumeWork> Work { get; init; } = Work ?? [];

    /// <summary>The schooling, none when the file has none.</summary>
    public IReadOnlyList<ResumeEducation> Education { get; init; } = Education ?? [];

    /// <summary>The skills, none when the file has none.</summary>
    public IReadOnlyList<ResumeSkill> Skills { get; init; } = Skills ?? [];

    /// <summary>The certificates, none when the file has none.</summary>
    public IReadOnlyList<ResumeCertificate> Certificates { get; init; } = Certificates ?? [];

    /// <summary>The languages, none when the file has none.</summary>
    public IReadOnlyList<ResumeLanguage> Languages { get; init; } = Languages ?? [];

    /// <summary>The interests, none when the file has none.</summary>
    public IReadOnlyList<ResumeInterest> Interests { get; init; } = Interests ?? [];
}
