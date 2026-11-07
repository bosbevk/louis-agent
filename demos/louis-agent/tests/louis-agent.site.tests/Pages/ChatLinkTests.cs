using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The site is the coding agent's landing page, so it leads to the agent: the app at <c>/chat</c> unless the place it runs
/// says the chat lives somewhere else.
/// </summary>
public class ChatLinkTests
{
    /// <summary>The home page's words, from a site started with this setting.</summary>
    private static async Task<string> HomeWith(string? chatAddress)
    {
        await using var site = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                chatAddress is null ? [] : [new KeyValuePair<string, string?>("CHAT_ADDRESS", chatAddress)])));
        return await site.CreateClient().GetStringAsync("/");
    }

    /// <summary>By default it leads to the app on the same address, from the hero and from the header.</summary>
    [Test]
    public async Task ByDefault_ItLeadsToChat()
    {
        string home = await HomeWith(null);

        Assert.That(home.Split("href=\"/chat\"").Length - 1, Is.EqualTo(2));
    }

    /// <summary>Where the chat runs elsewhere, the setting says so.</summary>
    [Test]
    public async Task TheSetting_SaysWhereTheChatIs()
    {
        string home = await HomeWith("https://agent.example/chat");

        Assert.That(home, Does.Contain("href=\"https://agent.example/chat\""));
        Assert.That(home, Does.Not.Contain("href=\"/chat\""));
    }
}
