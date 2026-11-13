using LouisAgent.Site.Tests.Admin.Forms;
using LouisAgent.Web.Components.Editing;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing.Rest;

/// <summary>
/// F142, step 7c: the schooling is changed where it stands. A certificate's name and year are clicked and retyped.
/// A school is drawn as two composed lines, so it opens on a small form. The certificates with a link are drawn in
/// the last section, and a change here leaves them as they were.
/// </summary>
public class EditingSchoolingTests
{
    private static readonly CvRecord Saved = TheCv.Resume;

    /// <summary>A certificate's name is retyped, and the certificates drawn elsewhere are all still there.</summary>
    [Test]
    public void ACertificatesName_IsRetyped_AndTheLinkedOnesStay()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var first = Saved.Certificates.First(certificate => certificate.Url is null);

        OnThePage.Editable(ctx, handed).Retype("#education .edu-row:nth-child(1) h3", "Scope AI work");

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Certificates, Has.Count.EqualTo(Saved.Certificates.Count));
            Assert.That(handed.Value.Certificates, Does.Contain(first with { Name = "Scope AI work" }));
            Assert.That(handed.Value.Certificates.Where(certificate => certificate.Url is not null),
                Is.EqualTo(Saved.Certificates.Where(certificate => certificate.Url is not null)));
        });
    }

    /// <summary>A school opens on what its two lines are made of, and the tick keeps a changed field.</summary>
    [Test]
    public void ASchool_OpensOnWhatItsLinesAreMadeOf()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("#education button[aria-label='Change school 1']").Click();
        var fields = view.FindComponents<TextField>();
        var labels = fields.Select(field => field.Instance.Label).ToList();
        fields.Single(field => field.Instance.Label == "School").Find(".field-box").Input("CTI");
        view.Find("#education .inline-form button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(labels, Is.EqualTo(new[] { "School", "Qualification", "Field", "From", "To" }));
            Assert.That(handed.Value!.Education[0], Is.EqualTo(Saved.Education[0] with { Institution = "CTI" }));
            Assert.That(handed.Value.Education.Skip(1), Is.EqualTo(Saved.Education.Skip(1)));
        });
    }

    /// <summary>The schools and the certificates are each a list: one is taken out, one is added.</summary>
    [Test]
    public void TheSchools_AndTheCertificates_AreLists()
    {
        using var ctx = new BunitContext();
        var noSchool = new Handed<CvRecord>();
        var oneMore = new Handed<CvRecord>();
        var noCertificate = new Handed<CvRecord>();

        OnThePage.Editable(ctx, noSchool).Find("#education button[aria-label='Remove school 1']").Click();
        OnThePage.Editable(ctx, oneMore).Find("#education .add-school button.item-add").Click();
        OnThePage.Editable(ctx, noCertificate).Find("#education button[aria-label='Remove certificate 1']").Click();

        Assert.Multiple(() =>
        {
            Assert.That(noSchool.Value!.Education, Is.EqualTo(Saved.Education.Skip(1)));
            Assert.That(oneMore.Value!.Education, Has.Count.EqualTo(Saved.Education.Count + 1));
            Assert.That(noCertificate.Value!.Certificates, Has.Count.EqualTo(Saved.Certificates.Count - 1));
        });
    }
}
