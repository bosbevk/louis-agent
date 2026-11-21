using System.Reflection;
using AngleSharp.Dom;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// Every link on the site leads to a place on it (F128.1): when pages move, as the framework's did under
/// <c>/framework</c>, no page or menu is left pointing where nothing answers.
/// </summary>
public class SiteLinksTests
{
    /// <summary>The site's own links on every page, files left out.</summary>
    private static List<(string Where, string Href)> ThePagesLinks()
    {
        using var ctx = SitePageContext.Create();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any()).ToList();

        // Rendered now, while the context is alive.
        return pages.SelectMany(page => Links(page.Name, ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        }).FindAll("a[href^='/']"))).ToList();
    }

    /// <summary>The frame's own links, the menu's among them, with what the guide in its corner asks for.</summary>
    private static List<(string Where, string Href)> TheFramesLinks()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());

        return Links(nameof(SiteLayout), ctx.Render<SiteLayout>().FindAll("a[href^='/']")).ToList();
    }

    /// <summary>
    /// Each link's address, by where it is. A link to a file (an image, a film) has a dot in its last part.
    /// </summary>
    private static IEnumerable<(string Where, string Href)> Links(string where, IEnumerable<IElement> links) =>
        links.Select(link => (where, Href: link.GetAttribute("href")!))
            .Where(link => !link.Href.Split('/')[^1].Contains('.'));

    /// <summary>Each link names a page of the site, or a place on one.</summary>
    [Test]
    public void EveryLink_LeadsToAPlace()
    {
        var addresses = SiteAddresses.From("home", typeof(HomePage).Assembly);

        var links = ThePagesLinks().Concat(TheFramesLinks()).ToList();
        var dead = links.Where(link => addresses.Read(link.Href) is null)
            .Select(link => $"{link.Where}: {link.Href}").Distinct().ToList();

        Assert.That(links, Has.Count.GreaterThanOrEqualTo(20), "the links were found");
        Assert.That(dead, Is.Empty);
    }
}
