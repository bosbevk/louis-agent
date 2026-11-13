using LouisAgent.Site.Components.Cv;
using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Tests.Admin.Forms;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv.Editing;

/// <summary>
/// F142, step 7b, Louis: "can we not just edit it inline?". The top of the CV is changed where it stands: his name,
/// the summary, the town and the province, his email, and his links, which move, go, are added, and open on a small
/// form for the address behind the button. On the public page, where nobody listens, there is nothing to click.
/// </summary>
public class EditingTheTopTests
{
    private static readonly ResumeBasics Top = TheCv.Resume.Basics;

    /// <summary>
    /// The public page has nothing to click: no words that open, no buttons, and its links still lead.
    /// </summary>
    [Test]
    public void ThePublicPage_HasNothingToClick()
    {
        using var ctx = new BunitContext();

        var view = ctx.Render<ResumeView>(parameters => parameters.Add(page => page.Resume, TheCv.Resume));

        Assert.Multiple(() =>
        {
            Assert.That(view.FindAll(".inline-read, .item-tools, .item-add, .inline-form"), Is.Empty);
            Assert.That(view.FindAll(".cv-reach a[href]"), Has.Count.EqualTo(Top.Profiles.Count + 1),
                "each link, and the email");
        });
    }

    /// <summary>His name is clicked and retyped, the rest of the top as it was.</summary>
    [Test]
    public void TheName_IsRetypedWhereItStands()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Retype(".cv-hero h1", "Louis J Marais");

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Basics.Name, Is.EqualTo("Louis J Marais"));
            Assert.That(handed.Value.Basics with { Name = Top.Name }, Is.EqualTo(Top));
            Assert.That(handed.Value.Facts, Is.SameAs(TheCv.Resume.Facts), "nothing else in the CV is touched");
        });
    }

    /// <summary>The summary is a longer text, retyped in a box of several lines.</summary>
    [Test]
    public void TheSummary_IsRetypedWhereItStands()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.RetypeLong(".cv-hero .lede", "Builds AI agents in C#.");

        Assert.That(handed.Value!.Basics.Summary, Is.EqualTo("Builds AI agents in C#."));
    }

    /// <summary>
    /// The town and the province are two pieces of the line under the summary, each retyped on its own.
    /// </summary>
    [Test]
    public void TheTown_IsRetyped_AndTheProvinceStays()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);
        var pieces = view.FindAll(".cv-where .inline-read").Select(piece => piece.TextContent).ToList();

        view.Retype(".cv-where .cv-town", "Paternoster");

        Assert.Multiple(() =>
        {
            Assert.That(pieces, Is.EqualTo(new[] { Top.Location!.City, Top.Location.Region }));
            Assert.That(view.Find(".cv-where").TextContent, Does.Contain("open to remote and hybrid"));
            Assert.That(handed.Value!.Basics.Location!.City, Is.EqualTo("Paternoster"));
            Assert.That(handed.Value.Basics.Location.Region, Is.EqualTo(Top.Location.Region));
        });
    }

    /// <summary>His email is retyped on its button.</summary>
    [Test]
    public void TheEmail_IsRetypedOnItsButton()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<CvRecord>();
        var view = OnThePage.Editable(ctx, handed);

        view.Retype(".cv-reach .cv-email", "louis@example.org");

        Assert.That(handed.Value!.Basics.Email, Is.EqualTo("louis@example.org"));
    }
}
