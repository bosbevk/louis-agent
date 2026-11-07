using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Chat.Header;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Chat.Sidebar;
using LouisAgent.Web.Components.Chat.Turns;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Testing;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Chat.Shell;

/// <summary>
/// The chat's whole frame: the sidebar, the top bar, the messages, the message box and the work panel. A site brings
/// its logo, views and profile through slots, and says how a session opens, since the toolsets and the voice are its
/// own.
/// </summary>
public class ChatShellTests
{
    /// <summary>The frame draws in English with the framework's own parts, and an empty slot leaves no gap.</summary>
    [Test]
    public void TheFrame_Draws_InEnglish_AndEmptySlotsLeaveNoGap()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx);

        Assert.That(cut.FindAll(".app .sidebar"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".messages"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll("aside.work"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".menu-button").GetAttribute("aria-label"), Is.EqualTo("Open the menu"));
        Assert.That(cut.Find(".work-tab-open").TextContent, Is.EqualTo("Work"));
        Assert.That(cut.FindAll(".side-nav"), Is.Empty);
        Assert.That(cut.FindAll(".tagline"), Is.Empty);
    }

    /// <summary>With no empty slot from the site, a new chat is still a centred message box.</summary>
    [Test]
    public void WithNoEmptySlot_ANewChat_IsStillAMessageBox()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx);

        Assert.That(cut.FindAll(".messages .empty .composer #composer-input"), Has.Count.EqualTo(1));
    }

    /// <summary>A site's logo, tagline, views and profile go in their slots.</summary>
    [Test]
    public void ASitesSlots_AreFilled()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx, parameters => parameters
            .Add(shell => shell.Logo, "<b class=\"logo\">L</b>")
            .Add(shell => shell.Tagline, "Our line")
            .Add(shell => shell.Views, "<a class=\"my-view\" href=\"x\">X</a>")
            .Add(shell => shell.Profile, "<div class=\"who\">Me</div>"));

        Assert.That(cut.FindAll(".topbar .brand .logo"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".sidebar-top .logo"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".sidebar-top .tagline").TextContent, Is.EqualTo("Our line"));
        Assert.That(cut.FindAll(".side-nav .my-view"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".side-footer .who"), Has.Count.EqualTo(1));
    }

    /// <summary>A view's content replaces the empty chat, and there's no background behind it.</summary>
    [Test]
    public void AViewsContent_ReplacesTheEmptyChat()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx, parameters => parameters
            .Add(shell => shell.ViewContent, "<div class=\"my-page\">Page</div>")
            .Add(shell => shell.Background, "<div class=\"water\"></div>")
            .Add(shell => shell.Empty, _ => "<div class=\"my-empty\"></div>"));

        Assert.That(cut.FindAll(".my-page"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".my-empty"), Is.Empty);
        Assert.That(cut.FindAll(".water"), Is.Empty);
    }

    /// <summary>An empty chat shows the site's empty slot, its background, and the message box it's given.</summary>
    [Test]
    public void AnEmptyChat_ShowsTheSitesEmptySlot_AndItsBackground()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx, parameters => parameters
            .Add(shell => shell.Background, "<div class=\"water\"></div>")
            .Add(shell => shell.Empty, empty => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "my-empty");
                builder.AddContent(2, empty.MessageBox);
                builder.CloseElement();
            }));

        Assert.That(cut.FindAll(".water"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".my-empty #composer-input"), Has.Count.EqualTo(1));
    }

    /// <summary>A chat this browser doesn't have says so.</summary>
    [Test]
    public void AChatThisBrowserDoesntHave_SaysSo()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx, parameters => parameters.Add(shell => shell.SessionId, "gone"));

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".notice").TextContent, Does.Contain("isn't saved in this browser")));
    }

    /// <summary>A message opens the session the site's way, with the voice, then goes to its address.</summary>
    [Test]
    public void AMessage_OpensTheSession_TheSitesWay_AndGoesToItsAddress()
    {
        using var ctx = Context(out var api);
        ChatOpening? opening = null;
        var cut = Render(ctx, parameters => parameters.Add(shell => shell.ChatPath, "talk"), asked =>
        {
            opening = asked;
            return api.CreateSessionAsync(["acme"], asked.Role, asked.Location, asked.TimeZone);
        });

        Send(cut);

        Assert.That(opening!.Role, Is.EqualTo("site/voice-xx"));
        Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri,
            Does.EndWith($"/talk/{FixtureApi.SessionId}"));
        Assert.That(cut.FindAll(".composer #composer-input"), Has.Count.EqualTo(1));
    }

    /// <summary>A site's page redraws for its own reasons, and that doesn't close the chat that's open.</summary>
    [Test]
    public void ARedrawFromTheSite_KeepsTheOpenChat()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);
        Send(cut);

        cut.Render(parameters => parameters.Add(shell => shell.Title, "Again"));

        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
    }

    /// <summary>The line under a chat is the framework's unless the site brings its own words.</summary>
    [Test]
    public void TheCostLine_IsTheFrameworks_UnlessTheSiteBringsItsOwn()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);
        Send(cut);
        Assert.That(cut.Find(".chat-usage").TextContent, Does.Contain("This chat"));

        cut.Render(parameters => parameters.Add(shell => shell.UsageLine, _ => "<b>Ours</b>"));

        Assert.That(cut.Find(".chat-usage").TextContent, Is.EqualTo("Ours"));
    }

    /// <summary>A placeholder says it's coming, in the framework's words.</summary>
    [Test]
    public void APlaceholder_SaysItsComing()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);

        _ = cut.InvokeAsync(() => cut.Instance.ComingSoonAsync("Voice"));

        cut.WaitForAssertion(() => Assert.That(cut.Find(".toast-pill").TextContent, Is.EqualTo("Voice: coming soon.")));
    }

    /// <summary>Every part is on unless a site says otherwise.</summary>
    [Test]
    public void ByDefault_EveryPartShows()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx);

        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0);
        Assert.That(cut.FindComponents<CreditsMenu>(), Has.Count.EqualTo(1));
        Assert.That(cut.FindComponents<LanguageMenu>(), Has.Count.EqualTo(1));
        Assert.That(cut.FindComponents<ProfileMenu>(), Has.Count.EqualTo(1));
        Assert.That(cut.FindComponents<LocationPicker>(), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".sidebar-label-row"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".i-mic"), Has.Count.EqualTo(1));
    }

    /// <summary>A site can turn off what doesn't suit it, and each part takes its gap with it.</summary>
    [Test]
    public void ASite_CanTurnPartsOff()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx, parameters => parameters.Add(shell => shell.Features,
            new ChatFeatures(Credits: false, Language: false, Location: false, Projects: false, Unfinished: false)));

        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0);
        Assert.That(cut.FindComponents<CreditsMenu>(), Is.Empty);
        Assert.That(cut.FindComponents<LanguageMenu>(), Is.Empty);
        Assert.That(cut.FindComponents<ProfileMenu>(), Is.Empty);
        Assert.That(cut.FindComponents<LocationPicker>(), Is.Empty);
        Assert.That(cut.FindAll(".sidebar-label-row"), Is.Empty);
        Assert.That(cut.FindAll(".i-mic, .i-screen, .i-image"), Is.Empty);
    }

    /// <summary>What's unfinished goes from under the messages too, and copying stays.</summary>
    [Test]
    public void WithUnfinishedOff_OnlyCopyStaysUnderAMessage()
    {
        using var ctx = Context(out _);
        var turn = new Turn { UserText = "Hello" };

        var user = ctx.Render<UserActions>(p => p.Add(c => c.Turn, turn).Add(c => c.Unfinished, false));
        var answer = ctx.Render<AnswerActions>(p => p.Add(c => c.Turn, turn).Add(c => c.Unfinished, false));

        Assert.That(user.FindAll(".action"), Has.Count.EqualTo(1));
        Assert.That(answer.FindAll(".action"), Has.Count.EqualTo(1));
    }

    /// <summary>A site's own settings sit with the frame's.</summary>
    [Test]
    public void ASitesSettings_SitWithTheFrames()
    {
        using var ctx = Context(out _);

        var cut = Render(ctx, parameters => parameters.Add(shell => shell.Settings, "<label class=\"my-setting\">Mine</label>"));

        Assert.That(cut.FindAll(".sidebar-settings .my-setting"), Has.Count.EqualTo(1));
    }

    /// <summary>Sends the recorded question and waits for its turn to finish.</summary>
    private static void Send(IRenderedComponent<ChatShell> cut)
    {
        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0);
        cut.Find("#composer-input").Input(SessionStreamFixture.Question);
        cut.Find("#send-button").Click();
        cut.WaitForState(() => cut.FindAll(".turn").Count > 0 && cut.FindAll("button.stop").Count == 0,
            TimeSpan.FromSeconds(30));
    }

    /// <summary>A bUnit context with everything the shell asks for, over the stub API.</summary>
    private static BunitContext Context(out AgentApi api)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The shell with the least a site must say, and whatever a test adds.</summary>
    private static IRenderedComponent<ChatShell> Render(BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<ChatShell>>? more = null,
        Func<ChatOpening, Task<string>>? openSession = null)
    {
        var api = ctx.Services.GetRequiredService<AgentApi>();
        return ctx.Render<ChatShell>(parameters =>
        {
            parameters.Add(shell => shell.OpenSession,
                openSession ?? (asked => api.CreateSessionAsync(["acme"], asked.Role)));
            parameters.Add(shell => shell.VoicePrefix, "site/voice-");
            parameters.Add(shell => shell.DefaultLanguage, "xx");
            more?.Invoke(parameters);
        });
    }
}
