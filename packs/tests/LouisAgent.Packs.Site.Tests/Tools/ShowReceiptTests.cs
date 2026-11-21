using LouisAgent.Packs.Site.Tools;

namespace LouisAgent.Packs.Site.Tests.Tools;

/// <summary>
/// FX143, Louis on a sent lead shown again as a full ticket: "we should redesign that receipt", "show me 3 options",
/// his pick a sleek receipt card, "each site should have it's own", and the agent writing its words with a tool. Once
/// something went on the visitor's behalf, the agent shows a receipt: what happened, and one line on what comes next.
/// </summary>
public class ShowReceiptTests
{
    private readonly CardTools _tools = new();

    /// <summary>A title and a line make a receipt; the model hears not to say it again.</summary>
    [Test]
    public void AGoodReceipt_IsShown() =>
        Assert.That(_tools.ShowReceipt("Sent to Louis", "He'll be in touch at anna@example.com."),
            Does.StartWith("Shown:").And.Contain("receipt"));

    /// <summary>A receipt says what happened: without a title it is refused.</summary>
    [Test]
    public void NoTitle_IsRefused() =>
        Assert.That(_tools.ShowReceipt(" ", "He'll be in touch."),
            Does.StartWith("Error: not shown.").And.Contain("title"));

    /// <summary>A title over 40 characters or a line over 120 is refused, naming which and how long.</summary>
    [TestCase(41, 10, "title")]
    [TestCase(10, 121, "line")]
    public void TooLong_IsRefused(int title, int line, string part) =>
        Assert.That(_tools.ShowReceipt(new string('t', title), new string('l', line)),
            Does.StartWith("Error: not shown.").And.Contain(part));
}
