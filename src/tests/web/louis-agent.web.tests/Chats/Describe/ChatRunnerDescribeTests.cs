using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Tests.Chats.Describe;

/// <summary>A refused turn asks for a key on a site without accounts, and for a sign-in on one with them.</summary>
public class ChatRunnerDescribeTests
{
    private static ChatRunner Runner(bool accounts)
    {
        var settings = new ChatSettings();
        var api = new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("http://localhost/")
            }, settings);
        return new ChatRunner(api, new ChatStore(new NoJs(), settings), new ChatText(), () => Task.FromResult("s"))
        {
            AccountsEnabled = accounts
        };
    }

    [TestCase(false, "The API needs a key")]
    [TestCase(true, "Sign in to chat.")]
    public void A401_IsSaidForTheSite(bool accounts, string expected) =>
        Assert.That(Runner(accounts).Describe(new AgentApiException(HttpStatusCode.Unauthorized, "no")),
            Does.StartWith(expected));

    /// <summary>A JS runtime that answers nothing; the store isn't read here.</summary>
    private sealed class NoJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => default;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) => default;
    }
}
