using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Layout;

/// <summary>
/// A page in the framework can't name a layout in a site, and a page naming none gets the router's default, which on a
/// site can be its chat's. So a framework page names <see cref="AdminFrame"/>, which hands over to the site's layout.
/// </summary>
public class AdminFrameTests
{
    /// <summary>A framework page draws inside the site's layout, and the router's default stays out of it.</summary>
    [Test]
    public void AFrameworkPage_DrawsInTheSitesLayout_NotTheDefault()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new SiteFrames(typeof(SiteLayout), typeof(SiteLayout)));

        var cut = ctx.Render<RouteView>(parameters => parameters
            .Add(view => view.RouteData, new RouteData(typeof(FrameworkPage), new Dictionary<string, object?>()))
            .Add(view => view.DefaultLayout, typeof(DefaultLayout)));

        Assert.That(cut.FindAll(".site-layout .page"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".default-layout"), Is.Empty);
    }

    /// <summary>A site that forgot the setting is told which one it forgot.</summary>
    [Test]
    public void AForgottenSetting_SaysWhichOne()
    {
        using var ctx = new BunitContext();

        var error = Assert.Catch(() => ctx.Render<RouteView>(parameters => parameters
            .Add(view => view.RouteData, new RouteData(typeof(FrameworkPage), new Dictionary<string, object?>()))));

        Assert.That(error!.Message, Does.Contain(nameof(SiteFrames)));
    }

    /// <summary>A page as the framework writes one: it names the frame and nothing of any site.</summary>
    [Layout(typeof(AdminFrame))]
    private sealed class FrameworkPage : ComponentBase
    {
        /// <summary>Draws one marked element, to find inside the layouts.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Marked(builder, "page", null);
    }

    /// <summary>The admin layout a site would write.</summary>
    private sealed class SiteLayout : LayoutComponentBase
    {
        /// <summary>Draws the page inside a marked element.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Marked(builder, "site-layout", Body);
    }

    /// <summary>The layout a site's router gives a page naming none, such as its chat's.</summary>
    private sealed class DefaultLayout : LayoutComponentBase
    {
        /// <summary>Draws the page inside a marked element.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Marked(builder, "default-layout", Body);
    }

    /// <summary>A div with this class, holding this content.</summary>
    private static void Marked(RenderTreeBuilder builder, string name, RenderFragment? content)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", name);
        builder.AddContent(2, content);
        builder.CloseElement();
    }
}
