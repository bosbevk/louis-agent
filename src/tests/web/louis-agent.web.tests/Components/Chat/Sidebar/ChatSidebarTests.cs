using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Sidebar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat.Sidebar;

/// <summary>
/// The chat's sidebar. The chats sort themselves into favourites, recent and archived, and a site brings its brand, its
/// views and its profile through slots. Its words are English unless a site gives its own.
/// </summary>
public class ChatSidebarTests
{
    private static readonly List<ChatModel> Chats =
    [
        new() { SessionId = "fav", Title = "Liked", Favourite = true },
        new() { SessionId = "now", Title = "Recent one" },
        new() { SessionId = "old", Title = "Old one", Archived = true }
    ];

    /// <summary>With no words of its own, it's in English.</summary>
    [Test]
    public void ItIsInEnglish_ByDefault()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.Find(".new-chat").TextContent, Is.EqualTo("New chat"));
        Assert.That(cut.Find(".side-search").TextContent, Does.Contain("Search chats"));
        Assert.That(cut.FindAll(".sidebar-label").Select(label => label.TextContent.Trim()),
            Does.Contain("Favourites").And.Contain("Recent"));
        Assert.That(cut.Find(".sidebar-settings summary").TextContent, Is.EqualTo("Settings"));
        Assert.That(cut.Markup, Does.Contain("Use my location").And.Contain("API key"));
    }

    /// <summary>Each chat sits in its group, and links to where the site keeps its chats.</summary>
    [Test]
    public void EachChat_SitsInItsGroup_AndLinksToTheSitesAddress()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, parameters => parameters.Add(sidebar => sidebar.ChatPath, "talk"));

        Assert.That(cut.Find(".side-archive").TextContent, Does.Contain("Old one"));
        Assert.That(cut.FindAll(".chat-list > .chat-item a").Select(link => link.GetAttribute("href")),
            Is.EqualTo(new[] { "talk/fav", "talk/now" }));
    }

    /// <summary>A site's brand, views and profile go in their slots.</summary>
    [Test]
    public void ASitesBrandViewsAndProfile_GoInTheirSlots()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, parameters => parameters
            .Add(sidebar => sidebar.Brand, "<b class=\"logo\">L</b>")
            .Add(sidebar => sidebar.BrandTitle, "Home")
            .Add(sidebar => sidebar.Views, "<a class=\"my-view\" href=\"x\">X</a>")
            .Add(sidebar => sidebar.Profile, "<div class=\"who\">Me</div>"));

        Assert.That(cut.Find(".sidebar-top .brand").GetAttribute("title"), Is.EqualTo("Home"));
        Assert.That(cut.FindAll(".sidebar-top .brand .logo"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".side-nav .my-view"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".side-footer .who"), Has.Count.EqualTo(1));
    }

    /// <summary>A site with no views or profile has no empty box for them.</summary>
    [Test]
    public void EmptySlots_LeaveNoGap()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.FindAll(".side-nav"), Is.Empty);
    }

    /// <summary>The sidebar over a stub API, with whatever a test adds.</summary>
    private static IRenderedComponent<ChatSidebar> Render(BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<ChatSidebar>>? more = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var api = new AgentApi(new HttpClient(new StubHandler(FixtureApi.Answer)) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings());
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        return ctx.Render<ChatSidebar>(parameters =>
        {
            parameters.Add(sidebar => sidebar.Chats, Chats);
            more?.Invoke(parameters);
        });
    }
}
