using Bunit.TestDoubles;
using LouisAgent.Web.Components.Chat.Search;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat.Search;

/// <summary>
/// Searching the chats saved in this browser. Its words are English unless a site gives its own, and it opens a chat at
/// the address the site keeps them at.
/// </summary>
public class ChatSearchTests
{
    private static readonly List<ChatModel> Chats = [new() { SessionId = "sess_1", Title = "Pancakes" }];

    /// <summary>With no words of its own, the box is in English.</summary>
    [Test]
    public void TheBox_IsInEnglish_ByDefault()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.Find("#search-input").GetAttribute("placeholder"), Is.EqualTo("Search chats…"));
        Assert.That(cut.Find(".search-group").TextContent, Is.EqualTo("Recent"));
        Assert.That(cut.Find(".search-foot").TextContent, Does.Contain("choose").And.Contain("open"));
    }

    /// <summary>A query nothing matches says so, naming the query.</summary>
    [Test]
    public void NoMatch_SaysSo()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.Find("#search-input").Input("zebra");

        Assert.That(cut.Find(".search-none").TextContent, Is.EqualTo("No chats for \"zebra\"."));
    }

    /// <summary>Opening a result goes to where the site keeps its chats.</summary>
    [Test]
    public void AResult_OpensAtTheSitesAddress()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx);

        cut.Find(".search-result").Click();

        var nav = ctx.Services.GetRequiredService<NavigationManager>();
        Assert.That(nav.Uri, Does.EndWith("/talk/sess_1"));
    }

    /// <summary>Opens the search over one chat, for a site that keeps its chats at <c>talk/</c>.</summary>
    private static IRenderedComponent<ChatSearch> Render(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        return ctx.Render<ChatSearch>(parameters => parameters
            .Add(search => search.Open, true)
            .Add(search => search.Chats, Chats)
            .Add(search => search.ChatPath, "talk"));
    }
}
