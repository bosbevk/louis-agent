using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Staging.Windows;

namespace LouisAgent.Web.Tests.Components.Staging.Windows;

/// <summary>
/// F134.1: Louis's pick for what a window holds: one of two bodies the framework offers, which the site fills. This
/// one is a website with its chat: its photo with its name and a line over it, and beside it one question, the
/// agent's answer and one thing the agent showed.
/// </summary>
public class SiteBodyTests
{
    /// <summary>A website's body with everything given.</summary>
    private static IRenderedComponent<SiteBody> Full(BunitContext ctx) =>
        ctx.Render<SiteBody>(parameters => parameters
            .Add(body => body.Photo, "images/bakery.webp")
            .Add(body => body.Name, "Dawn Bakery")
            .Add(body => body.Line, "Bread at dawn")
            .Add(body => body.Note, "Open from five.")
            .Add(body => body.Menu, new[] { "Bread", "Hours" })
            .Add(body => body.Icon, DiagramIcon.Dining)
            .Add(body => body.Ask, "Ask Dawn")
            .Add(body => body.AskNote, "answers in seconds")
            .Add(body => body.Question, "Do you bake rye?")
            .Add(body => body.Answer, "On Fridays.")
            .Add(body => body.Shown, "<span class=\"loaf\">The rye loaf</span>")
            .Add(body => body.Hint, "Type your question"));

    /// <summary>
    /// The website's side: its photo, which is decoration, its name as its mark, its menu and its line.
    /// </summary>
    [Test]
    public void TheWebsite_IsItsPhoto_ItsName_AndItsLine()
    {
        using var ctx = new BunitContext();

        var site = Full(ctx).Find(".site-body .body-site");

        Assert.Multiple(() =>
        {
            Assert.That(site.QuerySelector("img")!.GetAttribute("src"), Is.EqualTo("images/bakery.webp"));
            Assert.That(site.QuerySelector("img")!.GetAttribute("alt"), Is.Empty);
            Assert.That(site.QuerySelector(".body-mark")!.TextContent, Is.EqualTo("Dawn Bakery"));
            Assert.That(site.QuerySelectorAll(".body-menu span").Select(word => word.TextContent),
                Is.EqualTo(new[] { "Bread", "Hours" }));
            Assert.That(site.QuerySelector(".body-line")!.TextContent, Is.EqualTo("Bread at dawn"));
            Assert.That(site.QuerySelector(".body-note")!.TextContent, Is.EqualTo("Open from five."));
        });
    }

    /// <summary>The chat's side: its icon and title, the question, the answer, the thing shown, and the box.</summary>
    [Test]
    public void TheChat_IsTheQuestion_TheAnswer_AndWhatWasShown()
    {
        using var ctx = new BunitContext();

        var chat = Full(ctx).Find(".site-body .body-chat");

        Assert.Multiple(() =>
        {
            Assert.That(chat.QuerySelectorAll(".body-ask svg.line-icon"), Has.Count.EqualTo(1));
            Assert.That(chat.QuerySelector(".body-ask b")!.TextContent, Is.EqualTo("Ask Dawn"));
            Assert.That(chat.QuerySelector(".body-ask em")!.TextContent, Is.EqualTo("answers in seconds"));
            Assert.That(chat.QuerySelector(".body-question")!.TextContent, Is.EqualTo("Do you bake rye?"));
            Assert.That(chat.QuerySelector(".body-answer")!.TextContent, Is.EqualTo("On Fridays."));
            Assert.That(chat.QuerySelector(".body-shown .loaf")!.TextContent, Is.EqualTo("The rye loaf"));
            Assert.That(chat.QuerySelector(".body-hint")!.TextContent, Is.EqualTo("Type your question"));
        });
    }

    /// <summary>What is not given is not drawn: no empty note, menu, side note, shown thing or box.</summary>
    [Test]
    public void WhatIsNotGiven_IsNotDrawn()
    {
        using var ctx = new BunitContext();

        var body = ctx.Render<SiteBody>(parameters => parameters
            .Add(site => site.Photo, "images/bakery.webp")
            .Add(site => site.Name, "Dawn Bakery")
            .Add(site => site.Line, "Bread at dawn")
            .Add(site => site.Icon, DiagramIcon.Dining)
            .Add(site => site.Ask, "Ask Dawn")
            .Add(site => site.Question, "Do you bake rye?")
            .Add(site => site.Answer, "On Fridays."));

        Assert.Multiple(() =>
        {
            Assert.That(body.Find(".body-question").TextContent, Is.EqualTo("Do you bake rye?"));
            Assert.That(body.FindAll(".body-note, .body-menu, .body-ask em, .body-shown, .body-hint"), Is.Empty);
        });
    }

    /// <summary>
    /// In a narrow window, such as one on a phone, the website stands over its chat and no longer beside it. The
    /// body goes by the width of the window it is in, not the screen's, so the frame's body is what it measures.
    /// </summary>
    [Test]
    public void InANarrowWindow_TheWebsiteStandsOverItsChat()
    {
        string folder = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Components/Staging/Windows");
        string body = File.ReadAllText(Path.Combine(folder, "SiteBody.razor.scss")).ReplaceLineEndings("\n");
        string frame = File.ReadAllText(Path.Combine(folder, "FramedWindow.razor.scss"));
        int start = body.IndexOf("@container (max-width: 520px) {", StringComparison.Ordinal);
        string narrow = start < 0 ? "" : body[start..body.IndexOf("\n}", start, StringComparison.Ordinal)];

        Assert.Multiple(() =>
        {
            Assert.That(frame, Does.Contain("container-type: inline-size;"), "the frame's body is what is measured");
            Assert.That(narrow, Does.Contain("grid-template-columns: minmax(0, 1fr);"), "one column");
            Assert.That(narrow, Does.Contain("grid-template-rows: 36% minmax(0, 1fr);"), "the website, then the chat");
        });
    }

    /// <summary>
    /// The website's line is as large as its window lets it be, not as the screen does: a window can be small on a
    /// wide screen, and a line sized by the screen ran into the website's name and menu there.
    /// </summary>
    [Test]
    public void TheWebsitesLine_FollowsItsWindow_NotTheScreen()
    {
        string body = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Staging/Windows/SiteBody.razor.scss")).ReplaceLineEndings("\n");
        int start = body.IndexOf("\n.body-line {", StringComparison.Ordinal);
        string line = start < 0 ? "" : body[start..body.IndexOf("\n}", start + 1, StringComparison.Ordinal)];

        Assert.That(line, Does.Contain("cqi").And.Not.Contain("vw"));
    }
}
