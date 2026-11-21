using LouisAgent.Web.Accounts.Layout;
using LouisAgent.Web.Components.Frames;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Accounts.Tests.Layout;

/// <summary>
/// An account page in the framework names <see cref="AccountFrame"/>, which hands over to the site's account layout,
/// not to its admin one and not to the router's default.
/// </summary>
public class AccountFrameTests
{
    [Test]
    public void AnAccountPage_DrawsInTheSitesAccountLayout()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new SiteFrames(typeof(AdminSiteLayout), typeof(AccountSiteLayout)));

        var cut = ctx.Render<RouteView>(parameters => parameters
            .Add(view => view.RouteData, new RouteData(typeof(AccountPage), new Dictionary<string, object?>()))
            .Add(view => view.DefaultLayout, typeof(DefaultLayout)));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".account-site-layout .page"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".admin-site-layout"), Is.Empty);
            Assert.That(cut.FindAll(".default-layout"), Is.Empty);
        });
    }

    /// <summary>A page as the framework writes one: it names the frame and nothing of any site.</summary>
    [Layout(typeof(AccountFrame))]
    private sealed class AccountPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Marked(builder, "page", null);
    }

    private sealed class AccountSiteLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            Marked(builder, "account-site-layout", Body);
    }

    private sealed class AdminSiteLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            Marked(builder, "admin-site-layout", Body);
    }

    private sealed class DefaultLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Marked(builder, "default-layout", Body);
    }

    /// <summary>One element with this class, holding the body if there is one.</summary>
    private static void Marked(RenderTreeBuilder builder, string cssClass, RenderFragment? body)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", cssClass);
        if (body is not null) builder.AddContent(2, body);
        builder.CloseElement();
    }
}
