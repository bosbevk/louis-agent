using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Site;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Site.Search;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Search;

/// <summary>
/// F164.2 step 8 (Louis's pick A): a site's search, a button in its header that opens a window over the page, the
/// site's places as the visitor types, from the host's index with no model, and asking the site's agent the same
/// words as its last row.
/// </summary>
public class SiteSearchTests
{
    /// <summary>What the host's search answers for any words: one place, Packs on How it works.</summary>
    private static readonly PlacesReply Found =
        new([new PlaceFound("/how-it-works/packs", "Packs", "Packs What an agent can do.")]);

    /// <summary>A context whose API answers the search, with a door, the search's words given by the site.</summary>
    private static IRenderedComponent<SiteSearch> Render(BunitContext ctx, GuideDoor door, out StubHandler handler)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        handler = new StubHandler(request => request.RequestUri!.AbsolutePath == "/v1/site/places"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Found) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        ctx.Services.AddSingleton(new AgentApi(http, new ChatSettings()));
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(door);
        return ctx.Render<SiteSearch>(search => search
            .Add(s => s.Text, "Search")
            .Add(s => s.Placeholder, "Search the framework…")
            .Add(s => s.AskAbout, "Ask Orbi about \"{0}\""));
    }

    /// <summary>The search, opened, with words typed and their places drawn.</summary>
    private static IRenderedComponent<SiteSearch> Typed(BunitContext ctx, GuideDoor door, out StubHandler handler)
    {
        var cut = Render(ctx, door, out handler);
        cut.Find("button.site-search-button").Click();
        cut.Find("#site-search-input").Input("packs");
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".search-result"), Has.Count.EqualTo(2)));
        return cut;
    }

    /// <summary>Closed, it is a button with its keys, and no window.</summary>
    [Test]
    public void Closed_ItIsAButton_WithItsKeys()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, new GuideDoor(), out _);

        var button = cut.Find("button.site-search-button");
        Assert.Multiple(() =>
        {
            Assert.That(button.TextContent, Does.Contain("Search"));
            Assert.That(button.GetAttribute("aria-keyshortcuts"), Is.EqualTo("Control+K Meta+K"));
            Assert.That(cut.FindAll(".search-dialog"), Is.Empty);
        });
    }

    /// <summary>Typed words find their places from the host, and asking the agent the same words comes last.</summary>
    [Test]
    public void TypedWords_ShowTheirPlaces_AndAskTheAgentLast()
    {
        using var ctx = new BunitContext();

        var cut = Typed(ctx, new GuideDoor(), out var handler);

        var rows = cut.FindAll(".search-result");
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].QuerySelector("strong")!.TextContent, Is.EqualTo("Packs"));
            Assert.That(rows[0].QuerySelector("small")!.TextContent, Is.EqualTo("What an agent can do."),
                "the heading once, not again at the start of its words");
            Assert.That(rows[1].TextContent, Does.Contain("Ask Orbi about \"packs\""));
            Assert.That(handler.Requests.Select(request => request.Url), Has.Some.EndsWith("/v1/site/places?q=packs"));
        });
    }

    /// <summary>A place opens at its own address, and the window closes.</summary>
    [Test]
    public void APlace_OpensAtItsAddress()
    {
        using var ctx = new BunitContext();
        var cut = Typed(ctx, new GuideDoor(), out _);

        cut.FindAll(".search-result")[0].Click();

        Assert.Multiple(() =>
        {
            Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/how-it-works/packs"));
            Assert.That(cut.FindAll(".search-dialog"), Is.Empty);
        });
    }

    /// <summary>The last row asks the site's agent the words through the door, and the window closes.</summary>
    [Test]
    public void TheLastRow_AsksTheAgentTheWords()
    {
        using var ctx = new BunitContext();
        var door = new GuideDoor();
        var heard = new List<string?>();
        door.Asked += heard.Add;
        var cut = Typed(ctx, door, out _);

        cut.FindAll(".search-result")[1].Click();

        Assert.Multiple(() =>
        {
            Assert.That(heard, Is.EqualTo(new string?[] { "packs" }));
            Assert.That(cut.FindAll(".search-dialog"), Is.Empty);
        });
    }

    /// <summary>Its keys, Ctrl+K or Cmd+K, are held by the framework's shortcut module, which presses it.</summary>
    [Test]
    public void ItsKeys_AreHeldByTheShortcutModule()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Shortcut);
        module.SetupVoid("attach", _ => true);

        Render(ctx, new GuideDoor(), out _);

        Assert.That(module.Invocations["attach"].Single().Arguments[1], Is.EqualTo("k"));
    }
}
