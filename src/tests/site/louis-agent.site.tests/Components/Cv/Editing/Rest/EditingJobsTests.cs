using LouisAgent.Site.Resumes.History;
using LouisAgent.Site.Tests.Admin.Forms;
using LouisAgent.Web.Components.Editing;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing.Rest;

/// <summary>
/// F142, step 7c: the jobs are changed where they stand. A job's title and each point under it are clicked and
/// retyped, its stack is tags, and what the page draws as one line (the dates, the company with its few words)
/// opens on a small form. The jobs are a list, and so are a job's points.
/// </summary>
public class EditingJobsTests
{
    private static readonly IReadOnlyList<ResumeWork> Work = TheCv.Resume.Work;

    /// <summary>A job's title and a point under it are retyped, the other jobs as they were.</summary>
    [Test]
    public void AJobsTitle_AndAPoint_AreRetyped()
    {
        using var ctx = new BunitContext();
        var title = new Handed<CvRecord>();
        var point = new Handed<CvRecord>();

        OnThePage.Editable(ctx, title).Retype("#experience .job:nth-child(1) h3", "Principal developer");
        OnThePage.Editable(ctx, point).RetypeLong("#experience .job:nth-child(1) li:nth-child(2)", "Shipped it.");

        Assert.Multiple(() =>
        {
            Assert.That(title.Value!.Work[0], Is.EqualTo(Work[0] with { Position = "Principal developer" }));
            Assert.That(title.Value.Work.Skip(1), Is.EqualTo(Work.Skip(1)));
            Assert.That(point.Value!.Work[0].Highlights[1], Is.EqualTo("Shipped it."));
            Assert.That(point.Value.Work[0].Highlights[0], Is.EqualTo(Work[0].Highlights[0]));
        });
    }

    /// <summary>A job opens on its company, the few words about it and its dates, and the tick keeps them.</summary>
    [Test]
    public void AJob_OpensOnItsCompanyAndItsDates()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Find("#experience button[aria-label='Change job 2']").Click();
        var fields = view.FindComponents<TextField>();
        var labels = fields.Select(field => field.Instance.Label).ToList();
        fields.Single(field => field.Instance.Label == "To").Find(".field-box").Input("2024-12");
        view.Find("#experience .inline-form button.inline-keep").Click();

        Assert.Multiple(() =>
        {
            Assert.That(labels, Is.EqualTo(new[] { "Company", "About it", "From", "To" }));
            Assert.That(handed.Value!.Work[1], Is.EqualTo(Work[1] with { EndDate = "2024-12" }));
        });
    }

    /// <summary>A job's points are a list of their own: one is taken out, one is added under the others.</summary>
    [Test]
    public void AJobsPoints_AreAList()
    {
        using var ctx = new BunitContext();
        var removed = new Handed<CvRecord>();
        var added = new Handed<CvRecord>();

        OnThePage.Editable(ctx, removed).Find("#experience .job:nth-child(1) button[aria-label='Remove point 1']")
            .Click();
        OnThePage.Editable(ctx, added).Find("#experience .job:nth-child(1) .job-points button.item-add").Click();

        Assert.Multiple(() =>
        {
            Assert.That(removed.Value!.Work[0].Highlights, Is.EqualTo(Work[0].Highlights.Skip(1)));
            Assert.That(added.Value!.Work[0].Highlights, Has.Count.EqualTo(Work[0].Highlights.Count + 1));
            Assert.That(added.Value.Work[1], Is.EqualTo(Work[1]));
        });
    }

    /// <summary>
    /// The jobs are a list: one moves a step, one is taken out, one is added. And a stack is tags: one is taken out.
    /// </summary>
    [Test]
    public void TheJobs_AreAList_AndAStackIsTags()
    {
        using var ctx = new BunitContext();
        var moved = new Handed<CvRecord>();
        var removed = new Handed<CvRecord>();
        var added = new Handed<CvRecord>();
        var untagged = new Handed<CvRecord>();

        OnThePage.Editable(ctx, moved).Find("#experience button[aria-label='Move job 1 later']").Click();
        OnThePage.Editable(ctx, removed).Find("#experience button[aria-label='Remove job 1']").Click();
        OnThePage.Editable(ctx, added).Find("#experience .cv-jobs > button.item-add").Click();
        OnThePage.Editable(ctx, untagged).Find("#experience .job:nth-child(1) .tag-remove").Click();

        Assert.Multiple(() =>
        {
            Assert.That(moved.Value!.Work.Take(2), Is.EqualTo(new[] { Work[1], Work[0] }));
            Assert.That(removed.Value!.Work, Is.EqualTo(Work.Skip(1)));
            Assert.That(added.Value!.Work, Has.Count.EqualTo(Work.Count + 1));
            Assert.That(untagged.Value!.Work[0].Keywords, Is.EqualTo(Work[0].Keywords.Skip(1)));
        });
    }
}
