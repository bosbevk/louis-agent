using LouisAgent.Site.Components.Cv;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.History;
using CvRecord = LouisAgent.Site.Resumes.Resume;

namespace LouisAgent.Site.Tests.Components.Cv;

/// <summary>
/// F142: the CV's view draws each section from the record (the hero, the facts, the jobs, the skills, the schooling
/// and the rest), as the approved mockup lays them out; the page hands it the record, the photo and its own words.
/// </summary>
public class ResumeViewTests
{
    /// <summary>The sample CV beside the reader's test.</summary>
    private static CvRecord Sample()
    {
        using var file = File.OpenRead(Path.Combine(TestPaths.RepoRoot,
            "src/tests/site/louis-agent.site.tests/Resumes/sample-resume.json"));
        return ResumeReader.Read(file);
    }

    /// <summary>The view of this CV, with a photo and the page's line about where Louis would work.</summary>
    private static IRenderedComponent<ResumeView> Render(BunitContext ctx, CvRecord resume) =>
        ctx.Render<ResumeView>(parameters => parameters
            .Add(view => view.Resume, resume)
            .Add(view => view.Photo, "images/ada.jpg")
            .Add(view => view.Availability, "open to remote and hybrid"));

    /// <summary>The whole view, as the approved mockup lays it out.</summary>
    [Test]
    public void TheView_DrawsTheCv()
    {
        using var ctx = new BunitContext();

        var view = Render(ctx, Sample());

        Snapshots.Match("Components/Cv/ResumeView.html", view.Markup);
    }

    /// <summary>
    /// The "now" band is drawn from the CV (Louis's pick of three: in cv.json, as panels): its headline and words,
    /// and a panel per part with its label words, its text and its link into the site.
    /// </summary>
    [Test]
    public void TheNowBand_DrawsAPanelPerPart()
    {
        using var ctx = new BunitContext();

        var now = Render(ctx, Sample()).Find("#now");

        Assert.Multiple(() =>
        {
            Assert.That(now.QuerySelector("h2")!.TextContent, Is.EqualTo("louis-agent: an AI agent framework."));
            Assert.That(now.QuerySelector(".lede")!.TextContent, Is.EqualTo("An AI agent framework in C#."));
            Assert.That(now.QuerySelector(".panel .cv-part-label")!.TextContent, Is.EqualTo("Evaluation"));
            Assert.That(now.QuerySelector(".panel h3")!.TextContent, Is.EqualTo("Evals in C#"));
            Assert.That(now.QuerySelector(".panel p")!.TextContent, Is.EqualTo("One harness for every check."));
            Assert.That(now.QuerySelector(".panel a")!.GetAttribute("href"), Is.EqualTo("/framework/measured"));
            Assert.That(now.QuerySelector(".panel a")!.TextContent, Does.Contain("Measured, not assumed"));
        });
    }

    /// <summary>A CV with no "now" project has no such band.</summary>
    [Test]
    public void WithNoNowProject_ThereIsNoNowBand()
    {
        using var ctx = new BunitContext();

        var view = Render(ctx, Sample() with { Now = null });

        Assert.That(view.FindAll("#now"), Is.Empty);
    }

    /// <summary>A job shows its dates, title, company and what was shipped, and the stack it was built with.</summary>
    [Test]
    public void AJob_ShowsItsDatesTitleCompanyAndHighlights()
    {
        using var ctx = new BunitContext();

        var job = Render(ctx, Sample()).Find(".job");

        Assert.Multiple(() =>
        {
            Assert.That(job.QuerySelector(".when")!.TextContent, Does.Contain("Mar 2021 – Jun 2024"));
            Assert.That(job.QuerySelector("h3")!.TextContent, Is.EqualTo("Senior developer"));
            Assert.That(job.QuerySelector(".org")!.TextContent, Does.Contain("Kohde"));
            Assert.That(job.QuerySelector("li")!.TextContent, Is.EqualTo("Shipped the price-rule agent"));
            Assert.That(job.QuerySelectorAll(".stack span").Select(tag => tag.TextContent),
                Is.EqualTo(new[] { "C#", "Azure" }));
        });
    }

    /// <summary>A job that hasn't ended runs to now and is marked current.</summary>
    [Test]
    public void AJobThatHasntEnded_IsCurrent()
    {
        using var ctx = new BunitContext();
        var sample = Sample();
        var current = sample with { Work = [sample.Work[0] with { EndDate = null }] };

        var when = Render(ctx, current).Find(".job .when").TextContent;

        Assert.That(when, Does.Contain("Mar 2021 – now").And.Contain("current"));
    }

    /// <summary>Every link that leaves the site opens in a new tab and tells the other site nothing.</summary>
    [Test]
    public void ALinkOut_OpensANewTab_WithNoOpener()
    {
        using var ctx = new BunitContext();

        var outward = Render(ctx, Sample()).FindAll("a[href^='http']");

        Assert.Multiple(() =>
        {
            Assert.That(outward, Is.Not.Empty);
            Assert.That(outward.Select(link => link.GetAttribute("target")), Has.All.EqualTo("_blank"));
            Assert.That(outward.Select(link => link.GetAttribute("rel")), Has.All.EqualTo("noopener"));
        });
    }

    /// <summary>
    /// A certificate with a link to check is shown under "Also" with the interests; one without sits with the
    /// schooling, as the mockup has the AI certificates.
    /// </summary>
    [Test]
    public void ACertificate_SitsWithTheSchooling_UnlessItHasALink()
    {
        using var ctx = new BunitContext();
        var sample = Sample();
        var both = sample with
        {
            Certificates = [.. sample.Certificates, new ResumeCertificate("Evaluate AI Applications", Date: "2026")]
        };

        var view = Render(ctx, both);

        Assert.Multiple(() =>
        {
            Assert.That(view.Find(".edu").TextContent, Does.Contain("Evaluate AI Applications"));
            Assert.That(view.Find(".resume-also").TextContent, Does.Contain("Azure Developer").And.Contain("Chess"));
        });
    }
}
