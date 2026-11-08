using LouisAgent.Web.Components.Site.Menu;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// A group in a site's menu (FX73): the label in the bar, the links it holds under it, and the styles that open it on
/// hover and on focus, so a long menu stays one line and the keyboard still reaches every page.
/// </summary>
public class SiteMenuTests
{
    private static RenderFragment Links() => builder =>
    {
        builder.AddMarkupContent(0, "<a href=\"/one\">One</a><a href=\"/two\">Two</a>");
    };

    /// <summary>The label is a button in the bar and the links sit in the list under it.</summary>
    [Test]
    public void TheGroup_HasItsLabelAndItsLinks()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteMenu>(parameters => parameters.Add(menu => menu.Label, "The framework").AddChildContent(Links()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("button.site-menu-label").TextContent, Is.EqualTo("The framework"));
            Assert.That(cut.FindAll(".site-menu-list a"), Has.Count.EqualTo(2));
        });
    }

    /// <summary>The list opens on hover and on focus within, by the styles beside the component.</summary>
    [Test]
    public void TheList_OpensOnHoverAndOnFocus()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/Menu/SiteMenu.razor.scss"));

        Assert.That(rules, Does.Contain(".site-menu:hover .site-menu-list, .site-menu:focus-within .site-menu-list { display: flex;"));
    }
}
