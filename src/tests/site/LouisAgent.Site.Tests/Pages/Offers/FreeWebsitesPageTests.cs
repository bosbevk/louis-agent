using LouisAgent.Site.Pages.Offers;

namespace LouisAgent.Site.Tests.Pages.Offers;

/// <summary>
/// The two free websites on a page of their own (Louis, 2026-11-18: "give 2 websites for free in order to build a
/// portfolio", then "a company can send me their details and I decide who gets the build"): entries are open, three
/// steps, what is asked in return, and a button that asks Orbi to take the entry.
/// </summary>
public class FreeWebsitesPageTests
{
    /// <summary>The page says how it goes and what is asked, and Orbi takes the entry.</summary>
    [Test]
    public void ThePage_SaysHowItGoes_AndWhatIsAskedInReturn()
    {
        using var ctx = new BunitContext();

        var band = ctx.Render<FreeWebsitesPage>().Find("section#top");

        Assert.Multiple(() =>
        {
            Assert.That(band.TextContent, Does.Contain("Entries open").And.Contain("we choose two")
                .And.Contain("hosted free for the first year"));
            Assert.That(band.QuerySelectorAll(".tiles article h3").Select(title => title.TextContent),
                Is.EqualTo(new[] { "A few words from you", "Your website in our work", "Honest feedback" }));
            Assert.That(band.QuerySelector("button.btn")!.TextContent, Is.EqualTo("Enter your business"));
            Assert.That(band.QuerySelector("a[href='/contact']")!.TextContent, Is.EqualTo("Write to us"));
            Assert.That(band.TextContent, Does.Not.Contain(":").And.Not.Contain(";").And.Not.Match(@"R\d"), "no price");
        });
    }
}
