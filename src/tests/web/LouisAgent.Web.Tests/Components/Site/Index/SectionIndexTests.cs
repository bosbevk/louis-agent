using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Components.Site.Index;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Index;

/// <summary>
/// The section index comes from the bands (F118, Louis: "we should also have the sections in the left like on the
/// landing page"): every band on the page puts itself on the frame's list as it draws and takes itself off as it
/// goes, and the index draws that list, in page order, each band by its short label or its topic.
/// </summary>
public class SectionIndexTests
{
    /// <summary>A context whose browser answers the index's module with nothing, so the list keeps its order.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    /// <summary>
    /// The index lists the bands in page order, a band by its short label when it has one, else its topic; the hero
    /// marks its place.
    /// </summary>
    [Test]
    public void TheIndex_ListsTheBands_InOrder_ByTheirLabels()
    {
        using var ctx = Context();
        var areas = new PageAreas();

        RenderFragment page = builder =>
        {
            builder.OpenComponent<AreaMark>(0);
            builder.AddComponentParameter(1, nameof(AreaMark.Id), "top");
            builder.AddComponentParameter(2, nameof(AreaMark.Label), "Top");
            builder.CloseComponent();
            builder.OpenComponent<Band>(3);
            builder.AddComponentParameter(4, nameof(Band.Id), "rule");
            builder.AddComponentParameter(5, nameof(Band.Topic), "One rule");
            builder.CloseComponent();
            builder.OpenComponent<Band>(6);
            builder.AddComponentParameter(7, nameof(Band.Id), "proof");
            builder.AddComponentParameter(8, nameof(Band.Topic), "Measured, not assumed");
            builder.AddComponentParameter(9, nameof(Band.Short), "Measured");
            builder.CloseComponent();
            builder.OpenComponent<SectionIndex>(10);
            builder.CloseComponent();
        };
        var cut = ctx.Render<CascadingValue<PageAreas>>(parameters =>
            parameters.Add(c => c.Value, areas).AddChildContent(page));

        var links = cut.FindAll("nav.site-index a");
        Assert.Multiple(() =>
        {
            Assert.That(links.Select(a => a.GetAttribute("href")), Is.EqualTo(new[] { "#top", "#rule", "#proof" }));
            Assert.That(links.Select(a => a.TextContent), Is.EqualTo(new[] { "Top", "One rule", "Measured" }));
            Assert.That(cut.Find("nav.site-index").GetAttribute("aria-label"), Is.EqualTo("Sections"));
        });
    }

    /// <summary>
    /// The order is the page's, not the draw's: a band inside a component (the hero's mark, a tile strip) registers
    /// after the page's own bands, so the index asks the browser where each section stands and lists them so.
    /// </summary>
    [Test]
    public void TheIndex_FollowsThePagesOrder_NotTheDrawOrder()
    {
        using var ctx = Context();
        ctx.JSInterop.SetupModule(FrameworkScripts.SectionIndex).Setup<string[]>("order", _ => true)
            .SetResult(["top", "rule", "proof"]);
        var areas = new PageAreas();
        areas.Add("rule", "One rule");
        areas.Add("proof", "Measured");
        areas.Add("top", "Top");
        RenderFragment index = builder =>
        {
            builder.OpenComponent<SectionIndex>(0);
            builder.CloseComponent();
        };

        var cut = ctx.Render<CascadingValue<PageAreas>>(parameters =>
            parameters.Add(c => c.Value, areas).AddChildContent(index));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("nav.site-index a").Select(a => a.TextContent),
            Is.EqualTo(new[] { "Top", "One rule", "Measured" })));
        Assert.That(areas.Items.Select(entry => entry.Id), Is.EqualTo(new[] { "top", "rule", "proof" }));
    }

    /// <summary>A band that leaves the page leaves the index, and the list redraws by itself.</summary>
    [Test]
    public void ABandThatGoes_LeavesTheIndex()
    {
        using var ctx = Context();
        var areas = new PageAreas();
        areas.Add("top", "Top");
        areas.Add("rule", "One rule");
        areas.Add("proof", "Measured");
        RenderFragment index = builder =>
        {
            builder.OpenComponent<SectionIndex>(0);
            builder.CloseComponent();
        };
        var cut = ctx.Render<CascadingValue<PageAreas>>(parameters =>
            parameters.Add(c => c.Value, areas).AddChildContent(index));

        areas.Remove("rule");

        cut.WaitForAssertion(() => Assert.That(cut.FindAll("nav.site-index a").Select(a => a.TextContent),
            Is.EqualTo(new[] { "Top", "Measured" })));
    }

    /// <summary>
    /// The index is a glass panel, no lens, holding a list of tags, white and the one in view violet (FX72, FX163,
    /// Louis: "change this to a glass panel", "does not need water, just a list of tags alternating violet white").
    /// The rules sit beside the component (ADR 0094).
    /// </summary>
    [Test]
    public void TheIndex_IsAGlassPanelOfTags_ItsSectionInViewViolet()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Index/SectionIndex.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(rules, Does.Contain("@include surface.pane();"));
            Assert.That(rules, Does.Contain("--web-lens-piece: 0;"), "the panel is no lens");
            Assert.That(rules, Does.Contain("@include surface.tag();"));
            Assert.That(rules, Does.Contain(".site-index a.active { @include surface.tag-agent(); }"));
        });
    }

    /// <summary>
    /// The index has one width on every page and a long label wraps inside it (Louis: "a fixed width and wrapped").
    /// </summary>
    [Test]
    public void TheIndex_HasOneWidth_AndALongLabelWraps()
    {
        string rules = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Index/SectionIndex.razor.scss"));

        Assert.That(rules, Does.Contain("width: 172px;"));
        Assert.That(rules, Does.Not.Contain("white-space: nowrap"));
    }

    /// <summary>With nothing to choose between, no area or one, there is no index to draw.</summary>
    [TestCase(0)]
    [TestCase(1)]
    public void OneAreaOrNone_NoIndex(int count)
    {
        using var ctx = Context();
        var areas = new PageAreas();
        if (count == 1) areas.Add("top", "Top");

        RenderFragment index = builder =>
        {
            builder.OpenComponent<SectionIndex>(0);
            builder.CloseComponent();
        };
        var cut = ctx.Render<CascadingValue<PageAreas>>(parameters =>
            parameters.Add(c => c.Value, areas).AddChildContent(index));

        Assert.That(cut.FindAll("nav.site-index"), Is.Empty);
    }
}
