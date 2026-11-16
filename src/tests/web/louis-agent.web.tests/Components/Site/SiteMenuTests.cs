using LouisAgent.Web.Components.Site;
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

    /// <summary>
    /// FX176: a site may hide a group for now and keep it, its links still in the page but out of sight and out of
    /// reach (Louis: "disable the demos menu items for now until it is fixed and framework").
    /// </summary>
    [Test]
    public void AHiddenGroup_IsKept_OutOfSight()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteMenu>(parameters =>
            parameters.Add(menu => menu.Label, "Demos").Add(menu => menu.Hidden, true).AddChildContent(Links()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".site-drop").HasAttribute("hidden"), Is.True);
            Assert.That(cut.FindAll(".site-drop-list a"), Has.Count.EqualTo(2));
        });
    }

    /// <summary>The label is a button in the bar and the links sit in the list under it.</summary>
    [Test]
    public void TheGroup_HasItsLabelAndItsLinks()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteMenu>(parameters =>
            parameters.Add(menu => menu.Label, "The framework").AddChildContent(Links()));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("button.site-drop-label").TextContent, Is.EqualTo("The framework"));
            Assert.That(cut.FindAll(".site-drop-list a"), Has.Count.EqualTo(2));
        });
    }

    /// <summary>
    /// Each group opens alone (FX158): a scoped rule marks only its last part, so the class its list opens under must
    /// be the group's own and nothing round it in the frame, or a pointer anywhere in the menu opens every list.
    /// </summary>
    [Test]
    public void EachList_OpensUnderItsOwnGroupOnly()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        string opener = OpenerOf(File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/Menu/SiteMenu.razor.scss")));
        RenderFragment twoGroups = builder =>
        {
            foreach (string label in new[] { "Demos", "The framework" })
            {
                builder.OpenComponent<SiteMenu>(0);
                builder.AddComponentParameter(1, nameof(SiteMenu.Label), label);
                builder.AddComponentParameter(2, nameof(SiteMenu.ChildContent), Links());
                builder.CloseComponent();
            }
        };

        var cut = ctx.Render<SiteShell>(parameters => parameters.Add(shell => shell.Links, twoGroups));
        var openers = cut.FindAll($".{opener}-list")
            .Select(list => Ancestors(list).Count(element => element.ClassList.Contains(opener)))
            .ToList();

        Assert.That(openers, Is.EqualTo(new[] { 1, 1 }));
    }

    /// <summary>The class a list opens under: the one its open rule hovers on.</summary>
    private static string OpenerOf(string rules)
    {
        string rule = rules.Split('\n').First(line => line.Contains(":hover .") && line.Contains("-list"));
        return rule[(rule.IndexOf('.') + 1)..rule.IndexOf(":hover", StringComparison.Ordinal)];
    }

    /// <summary>Every element round this one, up to the page.</summary>
    private static IEnumerable<AngleSharp.Dom.IElement> Ancestors(AngleSharp.Dom.IElement element)
    {
        for (var up = element.ParentElement; up is not null; up = up.ParentElement) yield return up;
    }

    /// <summary>The list opens on hover and on focus within, by the styles beside the component.</summary>
    [Test]
    public void TheList_OpensOnHoverAndOnFocus()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/Menu/SiteMenu.razor.scss"));

        Assert.That(rules,
            Does.Contain(".site-drop:hover .site-drop-list, .site-drop:focus-within .site-drop-list { display: flex;"));
    }
}
