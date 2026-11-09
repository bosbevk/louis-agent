using LouisAgent.Site.Components.Landing.Live;

namespace LouisAgent.Site.Tests.Components.Landing;

/// <summary>
/// The landing's animated lead chat (F116) tells Louis's own chat of 2026-11-08 with made-up details: a visitor asks
/// about automating a task, the guide asks back, takes a name, a contact and a want, sends the lead and says so.
/// </summary>
public class LiveLeadTests
{
    /// <summary>
    /// The lines come in the order the chat ran: the visitor opens, the guide asks, the tool sends, the guide confirms.
    /// </summary>
    [Test]
    public void TheLines_TellTheChat_InOrder()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<LiveLead>();

        var lines = cut.FindAll(".turn-body .ln").Select(line => line.TextContent.Trim()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(lines.First(), Does.StartWith("›"), "the visitor opens");
            Assert.That(lines.Count(line => line.StartsWith("›")), Is.GreaterThanOrEqualTo(4),
                "the visitor speaks several times");
            Assert.That(string.Join(" ", lines), Does.Contain("SendLead"), "the tool is named");
            Assert.That(lines.Last(line => !line.StartsWith("turn:")), Does.Contain("Louis"),
                "the guide says the lead reached Louis");
        });
    }

    /// <summary>The lines fade in on their own delays, each later than the one before, like the hero's turn.</summary>
    [Test]
    public void TheLines_FadeIn_OneAfterAnother()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<LiveLead>();

        var delays = cut.FindAll(".turn-body .ln").Select(line => line.GetAttribute("style") ?? "")
            .Select(style => double.Parse(style.Replace("--d:", "").Replace("s", "").Trim(),
                System.Globalization.CultureInfo.InvariantCulture)).ToList();
        Assert.That(delays, Is.Ordered.Ascending, "later lines come later");
    }

    /// <summary>
    /// The details are made up: no real phone number, a mail address at example.co.za, and no semicolon for a reader.
    /// </summary>
    [Test]
    public void TheDetails_AreMadeUp()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<LiveLead>();

        string text = cut.Find(".turn-body").TextContent;
        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Not.Match(@"0\d{9}"), "no real phone number");
            Assert.That(text, Does.Contain("@example.co.za"));
            Assert.That(text, Does.Not.Contain(";"));
        });
    }
}
