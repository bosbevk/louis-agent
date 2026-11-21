using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas.Focuses;

/// <summary>
/// F127.7: the guide's widget sends the page's focus with the page, the part of the site the visitor is in as the site
/// names it, so the guide reads that part's rules; a site that names no focuses sends none.
/// </summary>
public class WidgetSendsTheFocusTests
{
    /// <summary>The bodies the widget sent, a message on a framework page, with or without the site's focuses.</summary>
    private static List<string> SentOnAFrameworkPage(bool withFocuses)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out var handler);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        if (withFocuses)
            ctx.Services.AddSiteAreas(areas => areas.Focuses(focuses => focuses.Default("selling")
                .Under("/framework", "framework")));
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Widget);
        module.Setup<Page>("pageText").SetResult(new Page("/framework/packs", "Packs", "The packs.", []));
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/framework/packs");

        var cut = ctx.Render<ChatWidget>(parameters => parameters.Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Ask the guide").Add(w => w.Greeting, "Hi.").Add(w => w.Placeholder, "Ask…"));
        cut.Find("button.widget-button").Click();
        cut.Find("#composer-input").Input("how does a pack load?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => handler.Requests.Any(r => r.Url.EndsWith("/messages")), TimeSpan.FromSeconds(10));

        return handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
    }

    /// <summary>On a framework page the message carries the framework's focus.</summary>
    [Test]
    public void OnAFrameworkPage_TheFocusGoes() =>
        Assert.That(SentOnAFrameworkPage(withFocuses: true).Single(), Does.Contain("\"focus\":\"framework\""));

    /// <summary>A site that names no focuses sends none, so its wire is as it was.</summary>
    [Test]
    public void WithoutFocuses_NoneGoes() =>
        Assert.That(SentOnAFrameworkPage(withFocuses: false).Single(), Does.Not.Contain("\"focus\""));
}
