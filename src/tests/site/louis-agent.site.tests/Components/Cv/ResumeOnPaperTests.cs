using LouisAgent.Site.Components.Cv;
using LouisAgent.Site.Tests.Admin.Forms;
using CvRecord = LouisAgent.Site.Resumes.Resume;
using TheCv = LouisAgent.Site.Content.Cv;

namespace LouisAgent.Site.Tests.Components.Cv;

/// <summary>
/// FX154, Louis, on the CV's PDF: "github and linked must also be QR codes", "again qr codes" (the panels' links),
/// "in a pdf hyperlinks is qr codes always". Every link the CV draws has a link for paper beside it, which the
/// print styles show in the ordinary link's place. A PDF never has a link that can only be clicked.
/// </summary>
public class ResumeOnPaperTests
{
    private static readonly CvRecord Saved = TheCv.Resume;

    /// <summary>The CV as the public page draws it.</summary>
    private static IRenderedComponent<ResumeView> Render(BunitContext ctx) =>
        ctx.Render<ResumeView>(parameters => parameters
            .Add(view => view.Resume, Saved)
            .Add(view => view.Photo, TheCv.Photo)
            .Add(view => view.Availability, TheCv.Availability));

    /// <summary>Every link has its code: the live CV, each profile, the email, each panel and each assessment.</summary>
    [Test]
    public void EveryLink_HasItsCode()
    {
        using var ctx = new BunitContext();

        var onPaper = Render(ctx).FindAll("a.paper-link").Select(link => link.GetAttribute("href")).ToList();

        var expected = new List<string?> { "cv" };
        expected.AddRange(Saved.Basics.Profiles.Select(profile => profile.Url));
        expected.Add($"mailto:{Saved.Basics.Email}");
        expected.AddRange(Saved.Now!.Parts.Where(part => part.Url is not null).Select(part => part.Url));
        expected.AddRange(Saved.Certificates.Where(certificate => certificate.Url is not null)
            .Select(certificate => certificate.Url));
        Assert.That(onPaper, Is.EqualTo(expected));
    }

    /// <summary>
    /// Every ordinary link the page draws is one of those, so none is left that paper can't follow.
    /// </summary>
    [Test]
    public void NoLink_IsLeftThatPaperCannotFollow()
    {
        using var ctx = new BunitContext();
        var view = Render(ctx);

        var onPaper = view.FindAll("a.paper-link").Select(link => link.GetAttribute("href")).ToHashSet();
        var ordinary = view.FindAll("a[href]:not(.paper-link)").Select(link => link.GetAttribute("href"));

        Assert.That(ordinary.Where(href => !onPaper.Contains(href)), Is.Empty);
    }

    /// <summary>
    /// Beside a code is what a person would know it by: who he is on that network, his number, his address.
    /// </summary>
    [Test]
    public void BesideACode_IsWhatAPersonKnowsItBy()
    {
        using var ctx = new BunitContext();
        var view = Render(ctx);

        var written = view.FindAll(".cv-codes a.paper-link").ToDictionary(
            link => link.QuerySelector("b")!.TextContent,
            link => link.QuerySelector(".paper-link-address")!.TextContent);

        Assert.Multiple(() =>
        {
            Assert.That(written["The live CV"], Is.EqualTo("localhost/cv"));
            Assert.That(written["LinkedIn"], Is.EqualTo("in/louismarais86"));
            Assert.That(written["GitHub"], Is.EqualTo("bosbevk"));
            Assert.That(written["WhatsApp"], Is.EqualTo(Saved.Basics.Phone));
            Assert.That(written["Email"], Is.EqualTo(Saved.Basics.Email));
        });
    }

    /// <summary>While the CV is edited in the admin there are no codes: they are for the printed page.</summary>
    [Test]
    public void WhileEditing_ThereAreNoCodes()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handed = new Handed<CvRecord>();

        var view = ctx.Render<ResumeView>(parameters => parameters
            .Add(page => page.Resume, Saved)
            .Add(page => page.ResumeChanged, changed => handed.Value = changed));

        Assert.That(view.FindAll(".paper-link"), Is.Empty);
    }
}
