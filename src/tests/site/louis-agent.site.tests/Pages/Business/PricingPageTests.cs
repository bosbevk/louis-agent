using LouisAgent.Identities.Guide;
using LouisAgent.Site.Pages;
using LouisAgent.Site.Pages.Business;

namespace LouisAgent.Site.Tests.Pages.Business;

/// <summary>
/// What a website with an agent costs, on a page of its own (F128.1, Louis's pick 3A): two plans, each with what it
/// includes, the building fee and what is quoted on the call; the contact page and Louis in the corner say the same.
/// </summary>
public class PricingPageTests
{
    /// <summary>
    /// Louis's voice and his map of the site's pages (a skill he reads on demand, F129), their lines joined, as the
    /// engine reads them.
    /// </summary>
    private static string Voice() => string.Join(' ',
        new[] { "voice.md", "skills/site-map.md" }.Select(Text).SelectMany(text =>
            text.Split((char[])[' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)));

    /// <summary>One of the guide's prompt files, whole.</summary>
    private static string Text(string path)
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts
            .GetFileInfo($"agent/louis-agent-guide/{path}").CreateReadStream());
        return reader.ReadToEnd();
    }

    /// <summary>Each plan is a tile with its name, its price as the big number, a month, and what it holds.</summary>
    [TestCase("Starter", "R500", "300 conversations a month", "30 minutes of changes")]
    [TestCase("Business", "R1,000", "1,000 conversations a month", "an hour of changes")]
    public void APlan_SaysWhatItCosts_AndWhatItHolds(string name, string price, string conversations,
        string changes)
    {
        using var ctx = new BunitContext();

        var plan = ctx.Render<PricingPage>().FindAll(".plan")
            .Single(panel => panel.QuerySelector("h3")!.TextContent == name);

        Assert.Multiple(() =>
        {
            Assert.That(plan.QuerySelector(".stat-value")!.TextContent, Is.EqualTo(price));
            Assert.That(plan.QuerySelector(".stat-note")!.TextContent, Does.StartWith("a month"));
            Assert.That(plan.TextContent, Does.Contain(conversations).And.Contain(changes));
        });
    }

    /// <summary>What both plans leave to the short call: the building fee, the first files, a custom agent.</summary>
    [TestCase("once-off fee to build the site")]
    [TestCase("short call")]
    [TestCase("first Markdown files")]
    [TestCase("custom agent")]
    [TestCase("href=\"/contact\"")]
    public void ThePage_SaysWhatIsQuoted(string words)
    {
        using var ctx = new BunitContext();

        Assert.That(ctx.Render<PricingPage>().Markup, Does.Contain(words));
    }

    /// <summary>The contact page sends a buyer to the plans.</summary>
    [Test]
    public void TheContactPage_LeadsToThePlans()
    {
        using var ctx = new BunitContext();

        Assert.That(ctx.Render<ContactPage>().FindAll("a[href='/pricing']"), Is.Not.Empty);
    }

    /// <summary>Louis in the corner quotes the same two plans, and knows where they are.</summary>
    [TestCase("Starter, R500 a month")]
    [TestCase("Business, R1,000 a month")]
    [TestCase("300 conversations")]
    [TestCase("1,000 conversations")]
    [TestCase("`/pricing`")]
    public void LouisQuotesTheSamePlans(string words) =>
        Assert.That(Voice(), Does.Contain(words));
}
