using LouisAgent.Site.Resumes.History;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;
using School = LouisAgent.Site.Resumes.History.ResumeEducation;

namespace LouisAgent.Site.Components.Cv.History;

/// <summary>
/// The CV's schooling (F142): the certificates with no link to check (the 2026 AI ones), then each school or course
/// with its dates, what it was, and where.
/// </summary>
public partial class ResumeEducation
{
    /// <summary>A certificate to fill in.</summary>
    private static readonly ResumeCertificate BlankCertificate = new("");

    /// <summary>A school to fill in.</summary>
    private static readonly School BlankSchool = new("");

    private int? _openCertificate, _openSchool;

    /// <summary>The schools and courses, in the file's order.</summary>
    [Parameter]
    public IReadOnlyList<School> Education { get; set; } = [];

    /// <summary>The certificates that sit with the schooling.</summary>
    [Parameter]
    public IReadOnlyList<ResumeCertificate> Certificates { get; set; } = [];

    /// <summary>Told the schools with one thing changed. With nothing listening, they are plain words.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<School>> EducationChanged { get; set; }

    /// <summary>Told these certificates with one thing changed, the same way.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeCertificate>> CertificatesChanged { get; set; }

    /// <summary>Whether the schooling is being edited.</summary>
    private bool Editable => EducationChanged.HasDelegate;

    /// <summary>The listener for a certificate's year.</summary>
    private EventCallback<string?> YearChanged(int at) => Words(at, year => Certificates[at] with { Date = year });

    /// <summary>The listener for a certificate's name, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> NameChanged(int at) =>
        Words(at, name => Certificates[at] with { Name = name ?? "" });

    /// <summary>The listener for who gave a certificate.</summary>
    private EventCallback<string?> GiverChanged(int at) =>
        Words(at, giver => Certificates[at] with { Issuer = giver });

    /// <summary>A listener for some words of a certificate, there exactly when someone listens for them all.</summary>
    private EventCallback<string?> Words(int at, Func<string?, ResumeCertificate> changed) =>
        CertificatesChanged.Of<IReadOnlyList<ResumeCertificate>, string?>(
            this, words => Certificates.Replaced(at, changed(words)));

    /// <summary>Opens a certificate's form, for its address.</summary>
    private void OpenCertificate(int at) => (_openCertificate, _openSchool) = (at, null);

    /// <summary>Opens a school's form.</summary>
    private void OpenSchool(int at) => (_openCertificate, _openSchool) = (null, at);

    /// <summary>Closes whichever form is open, its edits dropped.</summary>
    private void Close() => (_openCertificate, _openSchool) = (null, null);

    /// <summary>The open certificate is kept with its edits, and its form closes.</summary>
    private Task KeepCertificateAsync(int at, ResumeCertificate kept)
    {
        Close();
        return CertificatesChanged.InvokeAsync(Certificates.Replaced(at, kept));
    }

    /// <summary>A certificate moves a step. An open form closes, since its place has changed.</summary>
    private Task MoveCertificateAsync(int at, int step)
    {
        Close();
        return CertificatesChanged.InvokeAsync(Certificates.Moved(at, step));
    }

    /// <summary>A certificate is taken out.</summary>
    private Task RemoveCertificateAsync(int at)
    {
        Close();
        return CertificatesChanged.InvokeAsync(Certificates.Without(at));
    }

    /// <summary>One more certificate, blank, after the others.</summary>
    private Task AddCertificateAsync() => CertificatesChanged.InvokeAsync(Certificates.Added(BlankCertificate));

    /// <summary>The open school is kept with its edits, and its form closes.</summary>
    private Task KeepSchoolAsync(int at, School kept)
    {
        Close();
        return EducationChanged.InvokeAsync(Education.Replaced(at, kept));
    }

    /// <summary>A school moves a step.</summary>
    private Task MoveSchoolAsync(int at, int step)
    {
        Close();
        return EducationChanged.InvokeAsync(Education.Moved(at, step));
    }

    /// <summary>A school is taken out.</summary>
    private Task RemoveSchoolAsync(int at)
    {
        Close();
        return EducationChanged.InvokeAsync(Education.Without(at));
    }

    /// <summary>One more school, blank, at the end, with its form open to fill it in.</summary>
    private Task AddSchoolAsync()
    {
        OpenSchool(Education.Count);
        return EducationChanged.InvokeAsync(Education.Added(BlankSchool));
    }

    /// <summary>A certificate's address, in its form. With one, it is drawn in the last section as a link.</summary>
    private static Task CertificateAddressAsync(EditedItem<ResumeCertificate> certificate, string? address) =>
        certificate.Changed.InvokeAsync(certificate.Item with { Url = address });

    /// <summary>The school's name, in its form.</summary>
    private static Task SchoolAsync(EditedItem<School> school, string? name) =>
        school.Changed.InvokeAsync(school.Item with { Institution = name ?? "" });

    /// <summary>What was studied for: a diploma, matric.</summary>
    private static Task QualificationAsync(EditedItem<School> school, string? type) =>
        school.Changed.InvokeAsync(school.Item with { StudyType = type });

    /// <summary>The field it was in.</summary>
    private static Task FieldAsync(EditedItem<School> school, string? area) =>
        school.Changed.InvokeAsync(school.Item with { Area = area });

    /// <summary>The year it began.</summary>
    private static Task SchoolFromAsync(EditedItem<School> school, string? from) =>
        school.Changed.InvokeAsync(school.Item with { StartDate = from });

    /// <summary>The year it ended.</summary>
    private static Task SchoolToAsync(EditedItem<School> school, string? to) =>
        school.Changed.InvokeAsync(school.Item with { EndDate = to });

    /// <summary>The row's heading: what kind of study it was, else what was studied, else the school.</summary>
    private static string TitleOf(School school) =>
        school.StudyType ?? school.Area ?? school.Institution;

    /// <summary>The line under it: the school, and what was studied when the heading named the kind.</summary>
    private static string WhereOf(School school) =>
        school is { StudyType: not null, Area: { } area } ? $"{school.Institution} · {area}" : school.Institution;
}
