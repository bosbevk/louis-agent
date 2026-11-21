using LouisAgent.Web.Components.Chat.Sidebar;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat.Sidebar;

/// <summary>
/// One chat in a sidebar, and its menu. The framework's words are English, a site can give its own, and the link goes to
/// the address the site keeps its chats at.
/// </summary>
public class ChatListItemTests
{
    private static readonly ChatModel Chat = new() { SessionId = "sess_1", Title = "Pancakes" };

    /// <summary>With no words of its own, the menu is in English.</summary>
    [Test]
    public void TheMenu_IsInEnglish_ByDefault()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<ChatListItem>(parameters => parameters.Add(item => item.Chat, Chat));

        string[] items = [.. cut.FindAll(".dropdown-item").Select(item => item.TextContent.Trim())];
        Assert.That(items, Is.EqualTo(new[] { "Add to favourites", "Share", "Rename", "Archive", "Delete" }));
        Assert.That(cut.Find(".item-menu").GetAttribute("aria-label"), Is.EqualTo("More for Pancakes"));
    }

    /// <summary>A favourite or archived chat offers the way back.</summary>
    [Test]
    public void AFavouriteAndArchivedChat_OffersTheWayBack()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var chat = new ChatModel { SessionId = "sess_1", Title = "Pancakes", Favourite = true, Archived = true };

        var cut = ctx.Render<ChatListItem>(parameters => parameters.Add(item => item.Chat, chat));

        string[] items = [.. cut.FindAll(".dropdown-item").Select(item => item.TextContent.Trim())];
        Assert.That(items, Does.Contain("Remove from favourites").And.Contain("Take out of archive"));
    }

    /// <summary>A site's words replace them.</summary>
    [Test]
    public void ASitesWords_ReplaceThem()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText { Rename = "Mine" });

        var cut = ctx.Render<ChatListItem>(parameters => parameters.Add(item => item.Chat, Chat));

        Assert.That(cut.FindAll(".dropdown-item").Select(item => item.TextContent.Trim()), Does.Contain("Mine"));
    }

    /// <summary>The link goes to where the site keeps its chats.</summary>
    [Test]
    public void TheLink_GoesToTheSitesAddress()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<ChatListItem>(parameters => parameters
            .Add(item => item.Chat, Chat)
            .Add(item => item.ChatPath, "talk"));

        Assert.That(cut.Find(".chat-item a").GetAttribute("href"), Is.EqualTo("talk/sess_1"));
    }
}
