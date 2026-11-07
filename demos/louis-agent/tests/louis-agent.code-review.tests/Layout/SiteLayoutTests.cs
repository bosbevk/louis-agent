using LouisAgent.CodeReview.Components.Landing;
using LouisAgent.CodeReview.Layout;

namespace LouisAgent.CodeReview.Tests.Layout;

/// <summary>
/// The site's frame leads everywhere the site goes: to every page of the menu, and to the coding agent's chat at
/// <c>/chat</c> unless a link is told the chat lives somewhere else.
/// </summary>
public class SiteLayoutTests
{
    /// <summary>The menu leads to each page, from every page.</summary>
    [TestCase("/how-it-works")]
    [TestCase("/packs")]
    [TestCase("/build-your-own")]
    [TestCase("/proof")]
    [TestCase("/built-on-it")]
    [TestCase("/contact")]
    public void TheMenu_LeadsToThePage(string address)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll($".site-nav a[href='{address}']"), Has.Count.EqualTo(1));
    }

    /// <summary>The header's way into the chat leads to the app at /chat.</summary>
    [Test]
    public void TheHeader_LeadsToTheChat()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteLayout>();

        Assert.That(cut.FindAll(".site-actions a[href='/chat']"), Has.Count.EqualTo(1));
    }

    /// <summary>Where the chat runs elsewhere, the link is told so.</summary>
    [Test]
    public void AChatLink_GoesWhereItsTold()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ChatLink>(parameters => parameters
            .Add(link => link.Address, "https://agent.example/chat")
            .AddChildContent("Try it"));

        Assert.That(cut.Find("a").GetAttribute("href"), Is.EqualTo("https://agent.example/chat"));
    }
}
