using LouisAgent.Site.Tests.Admin.Forms;
using LouisAgent.Web.Components.Editing;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing.Rest;

/// <summary>
/// F142, step 7c: the last section is changed where it stands. An assessment is a certificate with a link: its
/// name is retyped and its address is behind a small form. What he does outside work is a name with tags. And the
/// languages, in the line under the summary, are retyped as a list with commas.
/// </summary>
public class EditingAlsoTests
{
    private static readonly CvRecord Saved = TheCv.Resume;

    /// <summary>An assessment's address is changed in its form, and the certificates without a link stay.</summary>
    [Test]
    public void AnAssessmentsAddress_IsChangedInItsForm()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);
        var first = Saved.Certificates.First(certificate => certificate.Url is not null);
        int links = view.FindAll("#also a[href]").Count;

        view.Find("#also button[aria-label='Change assessment 1']").Click();
        view.FindComponents<TextField>().Single(field => field.Instance.Label == "Address").Find(".field-box")
            .Input("https://example.org/result");
        view.Find("#also .inline-form button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(links, Is.Zero, "while editing, a link leads nowhere");
            Assert.That(handed.Value!.Certificates, Has.Count.EqualTo(Saved.Certificates.Count));
            Assert.That(handed.Value.Certificates, Does.Contain(first with { Url = "https://example.org/result" }));
        });
    }

    /// <summary>An interest's name is retyped and its tags are changed, and the interests are a list.</summary>
    [Test]
    public void AnInterest_IsAName_WithTags()
    {
        using var ctx = new BunitContext();
        var renamed = new Handed<CvRecord>();
        var untagged = new Handed<CvRecord>();
        var added = new Handed<CvRecord>();

        OnThePage.Editable(ctx, renamed).Retype("#also .cv-interest:nth-of-type(2) .cv-interest-name", "Growing");
        OnThePage.Editable(ctx, untagged).Find("#also .cv-interest .tag-remove").Click();
        OnThePage.Editable(ctx, added).Find("#also .cv-interests button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(renamed.Value!.Interests[1], Is.EqualTo(Saved.Interests[1] with { Name = "Growing" }));
            Assert.That(untagged.Value!.Interests[0].Keywords, Is.EqualTo(Saved.Interests[0].Keywords.Skip(1)));
            Assert.That(added.Value!.Interests, Has.Count.EqualTo(Saved.Interests.Count + 1));
        });
    }

    /// <summary>The languages are retyped as a list with commas, where the line under the summary names them.</summary>
    [Test]
    public void TheLanguages_AreRetyped_WithCommas()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);
        string shown = view.Find(".cv-where .cv-languages .inline-read").TextContent;

        view.Retype(".cv-where .cv-languages", "English, Afrikaans, isiXhosa");

        Assert.Multiple(() =>
        {
            Assert.That(shown, Is.EqualTo("English, Afrikaans"));
            Assert.That(handed.Value!.Languages.Select(language => language.Language),
                Is.EqualTo(new[] { "English", "Afrikaans", "isiXhosa" }));
            Assert.That(handed.Value.Languages[0], Is.SameAs(Saved.Languages[0]), "one already there is kept");
        });
    }
}
