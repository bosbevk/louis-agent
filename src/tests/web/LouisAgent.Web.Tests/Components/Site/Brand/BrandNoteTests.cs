using LouisAgent.Web.Components.Site.Brand;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Brand;

/// <summary>
/// FX168: a brand's line with a link under it as its subtext, as the site stands its tagline and Louis's LinkedIn
/// beside the agent bar (Louis: "a link to my linked in", "in one of those bottom spaces, as subtext").
/// </summary>
public class BrandNoteTests
{
    /// <summary>The line, then the link under it, its mark before its words, opening in a tab of its own.</summary>
    [Test]
    public void TheNote_IsItsLine_ThenItsLinkUnderIt()
    {
        using var ctx = new BunitContext();
        RenderFragment mark = builder => builder.AddMarkupContent(0, "<svg class=\"a-mark\"></svg>");

        var note = ctx.Render<BrandNote>(p => p
            .Add(n => n.Line, "100% C#, because you can")
            .Add(n => n.Href, "https://www.linkedin.com/in/someone")
            .Add(n => n.LinkText, "linkedin.com/in/someone")
            .Add(n => n.Mark, mark));
        var link = note.Find(".brand-note > a.brand-note-link");

        Assert.Multiple(() =>
        {
            Assert.That(note.Find(".brand-note > .brand-note-line").TextContent, Is.EqualTo("100% C#, because you can"));
            Assert.That(link.GetAttribute("href"), Is.EqualTo("https://www.linkedin.com/in/someone"));
            Assert.That(link.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(link.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(link.QuerySelector(".a-mark"), Is.Not.Null);
            Assert.That(link.TextContent.Trim(), Is.EqualTo("linkedin.com/in/someone"));
        });
    }
}
