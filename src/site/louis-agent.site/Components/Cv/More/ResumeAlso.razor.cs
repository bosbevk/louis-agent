using LouisAgent.Site.Resumes.History;
using LouisAgent.Site.Resumes.Skills;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.More;

/// <summary>
/// The CV's "Also" (F142): the certificates with a link to check (Betterview, the IQ society), and the interests
/// outside work, side by side.
/// </summary>
public partial class ResumeAlso
{
    /// <summary>An assessment to fill in: a certificate with an address still to come, which keeps it here.</summary>
    private static readonly ResumeCertificate BlankAssessment = new("", Url: "");

    /// <summary>An interest to fill in.</summary>
    private static readonly ResumeInterest BlankInterest = new("");

    private int? _open;

    /// <summary>The certificates with a link to check.</summary>
    [Parameter]
    public IReadOnlyList<ResumeCertificate> Certificates { get; set; } = [];

    /// <summary>What he does outside work.</summary>
    [Parameter]
    public IReadOnlyList<ResumeInterest> Interests { get; set; } = [];

    /// <summary>Told these certificates with one thing changed. With nothing listening, they are plain links.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeCertificate>> CertificatesChanged { get; set; }

    /// <summary>Told the interests with one thing changed, the same way.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeInterest>> InterestsChanged { get; set; }

    /// <summary>Whether the section is being edited.</summary>
    private bool Editable => InterestsChanged.HasDelegate;

    /// <summary>The interests' panel, named when it is edited so its rows can be found.</summary>
    private string InterestsPanel => Editable ? "panel cv-interests" : "panel";

    /// <summary>While the page is edited a link leads nowhere, so a click on it doesn't leave the page.</summary>
    private string? AddressOf(ResumeCertificate certificate) => Editable ? null : certificate.Url;

    /// <summary>The listener for an assessment's name, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> NameChanged(int at) =>
        CertificatesChanged.Of<IReadOnlyList<ResumeCertificate>, string?>(
            this, name => Certificates.Replaced(at, Certificates[at] with { Name = name ?? "" }));

    /// <summary>The listener for an interest's name.</summary>
    private EventCallback<string?> InterestChanged(int at) =>
        InterestsChanged.Of<IReadOnlyList<ResumeInterest>, string?>(
            this, name => Interests.Replaced(at, Interests[at] with { Name = name ?? "" }));

    /// <summary>Opens an assessment's form, for its address.</summary>
    private void Open(int at) => _open = at;

    /// <summary>Closes the form, its edits dropped.</summary>
    private void Close() => _open = null;

    /// <summary>The open assessment is kept with its edits, and its form closes.</summary>
    private Task KeepCertificateAsync(int at, ResumeCertificate kept)
    {
        _open = null;
        return CertificatesChanged.InvokeAsync(Certificates.Replaced(at, kept));
    }

    /// <summary>An assessment moves a step. An open form closes, since its place has changed.</summary>
    private Task MoveCertificateAsync(int at, int step)
    {
        _open = null;
        return CertificatesChanged.InvokeAsync(Certificates.Moved(at, step));
    }

    /// <summary>An assessment is taken out.</summary>
    private Task RemoveCertificateAsync(int at)
    {
        _open = null;
        return CertificatesChanged.InvokeAsync(Certificates.Without(at));
    }

    /// <summary>One more assessment, blank, with its form open for the address that keeps it in this section.</summary>
    private Task AddCertificateAsync()
    {
        _open = Certificates.Count;
        return CertificatesChanged.InvokeAsync(Certificates.Added(BlankAssessment));
    }

    /// <summary>An interest's tags, with one added or taken out.</summary>
    private Task TagsAsync(int at, IReadOnlyList<string> tags) =>
        InterestsChanged.InvokeAsync(Interests.Replaced(at, Interests[at] with { Keywords = tags }));

    /// <summary>An interest moves a step.</summary>
    private Task MoveInterestAsync(int at, int step) => InterestsChanged.InvokeAsync(Interests.Moved(at, step));

    /// <summary>An interest is taken out.</summary>
    private Task RemoveInterestAsync(int at) => InterestsChanged.InvokeAsync(Interests.Without(at));

    /// <summary>One more interest, blank, at the end.</summary>
    private Task AddInterestAsync() => InterestsChanged.InvokeAsync(Interests.Added(BlankInterest));

    /// <summary>An assessment's address, in its form. With none it is drawn with the certificates instead.</summary>
    private static Task AddressAsync(EditedItem<ResumeCertificate> certificate, string? address) =>
        certificate.Changed.InvokeAsync(certificate.Item with { Url = address });

    /// <summary>An interest, and what about it in brackets when the file says ("Chess (Openings)").</summary>
    private static string LineOf(ResumeInterest interest) => interest.Keywords.Count > 0
        ? $"{interest.Name} ({string.Join(", ", interest.Keywords)})"
        : interest.Name;
}
