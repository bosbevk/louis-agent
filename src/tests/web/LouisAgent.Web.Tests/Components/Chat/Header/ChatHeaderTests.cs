using LouisAgent.Web.Components.Chat.Header;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat.Header;

/// <summary>
/// The chat's top bar. It shows a new chat, a chat, or the title of a view a site brings, and its words are English
/// unless a site gives its own.
/// </summary>
public class ChatHeaderTests
{
    /// <summary>With no chat, it says it's a new one, in English.</summary>
    [Test]
    public void NoChat_IsANewOne_InEnglish()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.Find(".chat-title").TextContent, Is.EqualTo("New chat"));
        Assert.That(cut.Find(".header-sidebar").GetAttribute("title"), Is.EqualTo("Show the sidebar"));
        Assert.That(cut.Find(".usage-meter").TextContent, Does.Contain("Credits"));
        Assert.That(cut.Find(".profile-who strong").TextContent, Is.EqualTo("Guest"));
    }

    /// <summary>With a chat, its title shows, with its menu.</summary>
    [Test]
    public void AChat_ShowsItsTitle_AndItsMenu()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, parameters => parameters.Add(header => header.Chat, new ChatModel
        {
            SessionId = "s",
            Title = "Pancakes"
        }));

        Assert.That(cut.Find(".chat-title").TextContent, Is.EqualTo("Pancakes"));
        Assert.That(cut.Find(".chat-heading .item-menu").GetAttribute("aria-label"), Is.EqualTo("More for this chat"));
    }

    /// <summary>A view's title replaces the chat's.</summary>
    [Test]
    public void AViewsTitle_ReplacesTheChats()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, parameters => parameters
            .Add(header => header.ViewTitle, "Explore")
            .Add(header => header.Chat, new ChatModel { SessionId = "s", Title = "Pancakes" }));

        Assert.That(cut.Find(".chat-title").TextContent, Is.EqualTo("Explore"));
    }

    /// <summary>One chat is said in the singular and several in the plural.</summary>
    [TestCase(1, "1 chat")]
    [TestCase(3, "3 chats")]
    public void TheMonthsChats_AreCounted(int chats, string expected)
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, parameters => parameters.Add(header => header.MonthChats, chats));

        Assert.That(cut.Find(".usage-menu").TextContent, Does.Contain(expected));
    }

    /// <summary>The language menu lists what the site offers, and marks the one in use.</summary>
    [Test]
    public void TheLanguages_AreListed_WithTheOneInUseMarked()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, parameters => parameters
            .Add(header => header.Language, "en")
            .Add(header => header.Languages, (IReadOnlyList<string>)["en", "af"]));

        Assert.That(cut.FindAll(".header-actions .dropdown-item").Any(item => item.ClassList.Contains("active")
                                                                              && item.TextContent.Contains("English")));
        Assert.That(cut.Markup, Does.Contain("Afrikaans"));
    }

    /// <summary>The top bar in the framework's words, with whatever a test adds.</summary>
    private static IRenderedComponent<ChatHeader> Render(BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<ChatHeader>>? more = null)
    {
        ctx.Services.AddSingleton(new ChatText());
        return ctx.Render<ChatHeader>(parameters => more?.Invoke(parameters));
    }
}
