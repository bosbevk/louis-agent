using LouisAgent.CodeReview.Components;
using LouisAgent.CodeReview.Pages;
using LouisAgent.Web.Components.Chat.Header;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Chat.Sidebar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Pages;

/// <summary>
/// The code-review app's chat page on the framework's frame, against the stub API: its name and boot screen, its links
/// to the packs' screens and its theme picker, without what doesn't suit a coding agent. A message opens a session and
/// streams the recorded turn. The empty page and the page with the turn are pinned to golden files.
/// </summary>
public class ChatPageTests
{
    /// <summary>A bUnit context with everything the page asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new ChatHandoff("chat"));
        // The theme picker reads css/themes/themes.json on its own client.
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(FixtureApi.Answer)) { BaseAddress = new Uri("http://localhost/") });
        return ctx;
    }

    /// <summary>The page, once the frame has loaded and the message box shows.</summary>
    private static IRenderedComponent<ChatPage> Render(BunitContext ctx)
    {
        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0, TimeSpan.FromSeconds(10));
        return cut;
    }

    /// <summary>The empty page shows its boot screen with the message box, and matches its golden file.</summary>
    [Test]
    public void TheEmptyPage_ShowsTheBootScreen_AndMatchesTheGoldenFile()
    {
        using var ctx = Context();

        var cut = Render(ctx);

        Assert.That(cut.Find(".empty h1").TextContent, Is.EqualTo("LOUIS AGENT"));
        Assert.That(cut.FindAll(".composer #composer-input"), Has.Count.EqualTo(1));
        Snapshots.Match("Pages/ChatPage.empty.html", cut.Markup);
    }

    /// <summary>What doesn't suit a coding agent, or only says it's coming, isn't drawn.</summary>
    [Test]
    public void WhatDoesntSuitACodingAgent_IsOff()
    {
        using var ctx = Context();

        var cut = Render(ctx);

        Assert.That(cut.FindComponents<CreditsMenu>(), Is.Empty);
        Assert.That(cut.FindComponents<LanguageMenu>(), Is.Empty);
        Assert.That(cut.FindComponents<ProfileMenu>(), Is.Empty);
        Assert.That(cut.FindComponents<LocationPicker>(), Is.Empty);
        Assert.That(cut.FindAll(".sidebar-label-row, .i-mic"), Is.Empty);
    }

    /// <summary>The sidebar links to each pack's screen, by the names the packs gave them.</summary>
    [Test]
    public void TheSidebar_LinksToThePacksScreens()
    {
        using var ctx = Context();

        var cut = Render(ctx);

        Assert.That(cut.FindAll(".sidebar a.side-link").Select(link => link.GetAttribute("href")), Is.EqualTo(new[]
        {
            "admin/screens/workspace-files", "admin/screens/git-changes", "admin/screens/git-branches"
        }));
    }

    /// <summary>The theme picker sits with the frame's settings.</summary>
    [Test]
    public void TheThemePicker_SitsWithTheSettings()
    {
        using var ctx = Context();

        var cut = Render(ctx);

        Assert.That(cut.FindComponents<ThemeSwitcher>(), Has.Count.EqualTo(1));
    }

    /// <summary>A prompt from an admin screen waits in the box, and nothing goes until the person sends it.</summary>
    [Test]
    public void APromptFromAScreen_WaitsInTheBox_Unsent()
    {
        using var ctx = Context();
        ctx.Services.GetRequiredService<ChatHandoff>()
            .OpenChatWith("Review this", ctx.Services.GetRequiredService<NavigationManager>());

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.Find("#composer-input").GetAttribute("value"), Is.EqualTo("Review this")),
            TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".empty"), Has.Count.EqualTo(1), "still an empty chat: nothing was sent");
    }

    /// <summary>A message opens a session and streams the turn, and the page matches its golden file.</summary>
    [Test]
    public void AMessage_OpensASession_StreamsTheTurn_AndMatchesTheGoldenFile()
    {
        using var ctx = Context();
        var cut = Render(ctx);

        cut.Find("#composer-input").Input(SessionStreamFixture.Question);
        cut.Find("#send-button").Click();
        // The turn is over when the Stop button has given way to Send again and the chat was saved to localStorage.
        cut.WaitForState(() => cut.FindAll(".turn").Count > 0 && cut.FindAll("button.stop").Count == 0
            && ctx.JSInterop.Invocations.Any(i => i.Identifier == "localStorage.setItem"), TimeSpan.FromSeconds(30));

        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".user-text").TextContent, Is.EqualTo(SessionStreamFixture.Question));
        Assert.That(cut.Find(".chat-usage").TextContent, Does.StartWith("This chat:"));
        Snapshots.Match("Pages/ChatPage.turn.html", cut.Markup);
    }
}
