using LouisAgent.Web.Components.Site.Actions;

namespace LouisAgent.Web.Tests.Components.Site.Actions;

/// <summary>
/// A link drawn as one of the theme's buttons (ADR 0125): the main or the quiet one, or a third party's, its mark
/// before its words, opening a site of another in a new tab (FX163, the contact page made of controls).
/// </summary>
public class LinkButtonTests
{
    /// <summary>Each kind is drawn by its class.</summary>
    [TestCase(ButtonKind.Main, "btn-fill")]
    [TestCase(ButtonKind.Quiet, "btn-line")]
    [TestCase(ButtonKind.WhatsApp, "btn-whatsapp")]
    [TestCase(ButtonKind.LinkedIn, "btn-linkedin")]
    public void AKind_IsDrawnByItsClass(ButtonKind kind, string drawn)
    {
        using var ctx = new BunitContext();

        var link = ctx.Render<LinkButton>(p => p.Add(b => b.Href, "/contact").Add(b => b.Text, "Contact us")
            .Add(b => b.Kind, kind)).Find("a");

        Assert.That(link.ClassList, Is.EquivalentTo(new[] { "btn", drawn }));
    }

    /// <summary>A link to another site opens in a new tab, and its mark stands before its words.</summary>
    [Test]
    public void ALinkAway_OpensANewTab_WithItsMark()
    {
        using var ctx = new BunitContext();

        var link = ctx.Render<LinkButton>(p => p.Add(b => b.Href, "https://wa.me/27000000000")
            .Add(b => b.Text, "Chat on WhatsApp").Add(b => b.NewTab, true)
            .Add(b => b.Mark, "<svg class=\"mark\"></svg>")).Find("a");

        Assert.Multiple(() =>
        {
            Assert.That(link.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(link.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(link.FirstElementChild!.ClassName, Is.EqualTo("mark"));
            Assert.That(link.TextContent.Trim(), Is.EqualTo("Chat on WhatsApp"));
        });
    }
}
