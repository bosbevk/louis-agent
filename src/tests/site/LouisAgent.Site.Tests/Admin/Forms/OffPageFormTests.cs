using LouisAgent.Site.Admin.Forms;
using LouisAgent.Site.Content;
using LouisAgent.Site.Resumes.Basics;

namespace LouisAgent.Site.Tests.Admin.Forms;

/// <summary>
/// F142, Louis: "can we not just edit it inline?". His pick: the CV is edited on the page itself. A few things the CV
/// holds are never drawn on the page (the headline, the phone number, the website, the country), so they have nowhere
/// to be clicked. This small form beside the page is where they are changed. It never changes what it was given:
/// each edit is handed back as a new copy with that one thing changed.
/// </summary>
public class OffPageFormTests
{
    /// <summary>The top of Louis's CV.</summary>
    private static readonly ResumeBasics Top = Cv.Resume.Basics;

    /// <summary>The form over the top of the CV, handing its edits to the holder.</summary>
    private static IRenderedComponent<OffPageForm> Render(BunitContext ctx, Handed<ResumeBasics> handed) =>
        ctx.Render<OffPageForm>(parameters => parameters
            .Add(form => form.Basics, Top)
            .Add(form => form.BasicsChanged, changed => handed.Value = changed));

    /// <summary>The form asks only for what the page doesn't draw, and holds what the CV says.</summary>
    [Test]
    public void TheForm_AsksOnlyForWhatThePageDoesNotDraw()
    {
        using var ctx = new BunitContext();

        var form = Render(ctx, new Handed<ResumeBasics>());

        Assert.Multiple(() =>
        {
            Assert.That(form.Labels(), Is.EqualTo(new[] { "Headline", "Phone", "Website", "Country code" }));
            Assert.That(form.Box("Headline").GetAttribute("value"), Is.EqualTo(Top.Label));
            Assert.That(form.Box("Country code").GetAttribute("value"), Is.EqualTo(Top.Location!.CountryCode));
        });
    }

    /// <summary>A changed headline comes back with everything else as it was.</summary>
    [Test]
    public void AChangedHeadline_ComesBack_WithTheRestKept()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<ResumeBasics>();
        var form = Render(ctx, handed);

        form.Box("Headline").Input("Builds AI agents in C#");

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Label, Is.EqualTo("Builds AI agents in C#"));
            Assert.That(handed.Value with { Label = Top.Label }, Is.EqualTo(Top));
        });
    }

    /// <summary>A box left empty takes its field out of the CV.</summary>
    [Test]
    public void AnEmptiedBox_TakesItsFieldOut()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<ResumeBasics>();
        var form = Render(ctx, handed);

        form.Box("Phone").Change("");

        Assert.That(handed.Value!.Phone, Is.Null);
    }

    /// <summary>The country is one part of where he lives: it changes, and the town and the province stay.</summary>
    [Test]
    public void ACountry_ComesBack_WithTheTownKept()
    {
        using var ctx = new BunitContext();
        var handed = new Handed<ResumeBasics>();
        var form = Render(ctx, handed);

        form.Box("Country code").Input("NA");

        Assert.Multiple(() =>
        {
            Assert.That(handed.Value!.Location!.CountryCode, Is.EqualTo("NA"));
            Assert.That(handed.Value.Location.City, Is.EqualTo(Top.Location!.City));
            Assert.That(handed.Value.Location.Region, Is.EqualTo(Top.Location.Region));
        });
    }
}
