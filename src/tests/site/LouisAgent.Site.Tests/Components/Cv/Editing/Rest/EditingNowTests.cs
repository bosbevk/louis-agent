using LouisAgent.Site.Resumes;
using LouisAgent.Site.Tests.Admin.Forms;
using LouisAgent.Web.Components.Editing;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing.Rest;

/// <summary>
/// F142, step 7c: the Now band is changed where it stands. Its headline, its words and each panel's name and text
/// are clicked and retyped. A panel's label words are tags, its link opens on a small form, and the panels are a
/// list to move, take from and add to.
/// </summary>
public class EditingNowTests
{
    private static readonly ResumeProject Now = TheCv.Resume.Now!;

    /// <summary>The headline and a panel's name are retyped, the rest of the band as it was.</summary>
    [Test]
    public void TheHeadline_AndAPanelsName_AreRetyped()
    {
        using var ctx = new BunitContext();
        var headline = new Handed<CvRecord>();
        var name = new Handed<CvRecord>();

        OnThePage.Editable(ctx, headline).Retype("#now h2", "What I build now.");
        OnThePage.Editable(ctx, name).Retype("#now .panel:nth-child(2) h3", "Checks in C#");

        Assert.Multiple(() =>
        {
            Assert.That(headline.Value!.Now!.Headline, Is.EqualTo("What I build now."));
            Assert.That(headline.Value.Now.Parts, Is.EqualTo(Now.Parts));
            Assert.That(name.Value!.Now!.Parts[1], Is.EqualTo(Now.Parts[1] with { Name = "Checks in C#" }));
            Assert.That(name.Value.Now.Parts[0], Is.EqualTo(Now.Parts[0]));
        });
    }

    /// <summary>A panel's link opens on its words and its address, and the tick keeps both.</summary>
    [Test]
    public void APanelsLink_OpensOnItsWordsAndItsAddress()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("#now button[aria-label='Change part 2']").Click();
        var fields = view.FindComponents<TextField>();
        var labels = fields.Select(field => field.Instance.Label).ToList();
        fields.Single(field => field.Instance.Label == "Address").Find(".field-box").Input("/framework/measured");
        view.Find("#now .inline-form button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(labels, Is.EqualTo(new[] { "Link words", "Address" }));
            Assert.That(handed.Value!.Now!.Parts[1], Is.EqualTo(Now.Parts[1] with { Url = "/framework/measured" }));
        });
    }

    /// <summary>A panel moves a step, one is taken out, and one is added at the end.</summary>
    [Test]
    public void ThePanels_AreAList()
    {
        using var ctx = new BunitContext();
        var moved = new Handed<CvRecord>();
        var removed = new Handed<CvRecord>();
        var added = new Handed<CvRecord>();

        OnThePage.Editable(ctx, moved).Find("#now button[aria-label='Move part 1 later']").Click();
        OnThePage.Editable(ctx, removed).Find("#now button[aria-label='Remove part 1']").Click();
        OnThePage.Editable(ctx, added).Find("#now button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(moved.Value!.Now!.Parts.Take(2), Is.EqualTo(new[] { Now.Parts[1], Now.Parts[0] }));
            Assert.That(removed.Value!.Now!.Parts, Is.EqualTo(Now.Parts.Skip(1)));
            Assert.That(added.Value!.Now!.Parts, Has.Count.EqualTo(Now.Parts.Count + 1));
        });
    }

    /// <summary>While the page is edited a panel's link leads nowhere, and its label words are tags to change.</summary>
    [Test]
    public void WhileEditing_APanelsLinkLeadsNowhere_AndItsLabelWordsAreTags()
    {
        using var ctx = new BunitContext();

        var view = OnThePage.Editable(ctx, new Handed<CvRecord>());

        Assert.Multiple(() =>
        {
            Assert.That(view.FindAll("#now a[href]"), Is.Empty);
            Assert.That(view.FindAll("#now .panel:nth-child(1) .tag-word").Select(tag => tag.TextContent),
                Is.EqualTo(Now.Parts[0].Keywords));
        });
    }
}
