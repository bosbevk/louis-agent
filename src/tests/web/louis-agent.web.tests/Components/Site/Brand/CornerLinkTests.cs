using LouisAgent.Web.Components.Site.Brand;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Brand;

/// <summary>
/// FX173: a small link that stands in the bottom-right corner of the screen, such as the owner's LinkedIn (Louis: "put
/// the linked in in the bottom right corner").
/// </summary>
public class CornerLinkTests
{
    /// <summary>The link, its mark before its words, opening in a tab of its own.</summary>
    [Test]
    public void TheCornerLink_IsItsMarkAndWords_InATabOfItsOwn()
    {
        using var ctx = new BunitContext();
        RenderFragment mark = builder => builder.AddMarkupContent(0, "<svg class=\"a-mark\"></svg>");

        var link = ctx.Render<CornerLink>(p => p
            .Add(c => c.Href, "https://www.linkedin.com/in/someone")
            .Add(c => c.Text, "LinkedIn")
            .Add(c => c.Mark, mark)).Find("a.corner-link");

        Assert.Multiple(() =>
        {
            Assert.That(link.GetAttribute("href"), Is.EqualTo("https://www.linkedin.com/in/someone"));
            Assert.That(link.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(link.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(link.QuerySelector(".a-mark"), Is.Not.Null);
            Assert.That(link.TextContent.Trim(), Is.EqualTo("LinkedIn"));
        });
    }
}
