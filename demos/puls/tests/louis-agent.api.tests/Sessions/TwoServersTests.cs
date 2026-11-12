using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Agents;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// An agent asks an agent on another server (F127.3.2, Claude's pick Q6): the partner is a real API host with its own
/// key, its identities and a model, reached through its whole HTTP pipeline; the asker is core's <c>AskAgent</c>, as
/// a host's engine builds it, with the partner's address and key. No model costs anything here.
/// </summary>
public class TwoServersTests
{
    /// <summary>The partner's own key.</summary>
    private const string PartnerKey = "partner-key";

    /// <summary>The partner's shop: an identity in its own voice.</summary>
    private static readonly FakeIdentity Shop = new("shop", voice: "puls/voice-en");

    /// <summary>The asker on this side, who may ask the partner's shop and a bakery it doesn't have.</summary>
    private static readonly FakeIdentity Guide = new("guide", calls: ["partner:shop", "partner:bakery"]);

    /// <summary>The partner's server: its key, its shop, and a model that says when the shop is open.</summary>
    private static WebApplication Partner() =>
        ApiTestHost.Build(PartnerKey, moreIdentities: [Shop], evals: false,
            model: new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Open until five."))));

    /// <summary>Asks the partner as the guide would, with this key for the partner.</summary>
    private static async Task<string> AskAsync(WebApplication partner, string address, string key)
    {
        var tools = new AskAgentTools(new Dictionary<string, Uri> { ["partner"] = new("http://partner.test/") },
            [Guide], "own-key", new HttpClient(partner.GetTestServer().CreateHandler()),
            peerKeys: new Dictionary<string, string> { ["partner"] = key });
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Guide));
        return await tools.AskAgent(address, "When are you open today?");
    }

    /// <summary>With the partner's key the shop is found in its identities and answers in its own session.</summary>
    [Test]
    public async Task WithThePartnersKey_TheShopAnswers()
    {
        await using var partner = Partner();
        await partner.StartAsync();

        string answer = await AskAsync(partner, "partner:shop", PartnerKey);

        Assert.That(answer, Does.StartWith("Open until five.").And.Contain("Answered by partner:shop"));
    }

    /// <summary>A key the partner doesn't take says so, and nothing is asked.</summary>
    [Test]
    public async Task AWrongKey_SaysTheKeyWasRefused()
    {
        await using var partner = Partner();
        await partner.StartAsync();

        Assert.That(await AskAsync(partner, "partner:shop", "not-the-key"),
            Does.StartWith("Error").And.Contain("refused this server's key"));
    }

    /// <summary>An agent the partner doesn't have is refused, naming the ones it does.</summary>
    [Test]
    public async Task AnAgentThePartnerDoesntHave_IsRefused()
    {
        await using var partner = Partner();
        await partner.StartAsync();

        Assert.That(await AskAsync(partner, "partner:bakery", PartnerKey),
            Does.StartWith("Error").And.Contain("bakery").And.Contain("shop"));
    }
}
