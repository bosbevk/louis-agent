using System.Net;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Llm;

namespace LouisAgent.Core.Tests.Llm.AnthropicProvider;

// F4 step 1, the spike: what the Anthropic adapter (Microsoft.Extensions.AI over Anthropic 12.53) puts on the wire for
// cache markers, checked on the captured request body; no model call. Kept as a regression test: if an SDK update stops
// sending a marker, caching silently stops (the API raises no error), and this fails instead.
public class AnthropicCachingWireTests
{
    // A Messages API reply, with cache writes split by lifetime as the API reports them.
    private const string Reply = """
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn","stop_sequence":null,
         "usage":{"input_tokens":10,"output_tokens":2,"cache_creation_input_tokens":248,"cache_read_input_tokens":0,
                  "cache_creation":{"ephemeral_5m_input_tokens":148,"ephemeral_1h_input_tokens":100}}}
        """;

    private static (IChatClient Client, StubHandler Handler) Client()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Reply, System.Text.Encoding.UTF8, "application/json"),
        });
        var anthropic = new AnthropicClient(new ClientOptions { ApiKey = "test", HttpClient = new HttpClient(handler) });
        return (anthropic.AsIChatClient("claude-haiku-5-5", 1024), handler);
    }

    private static JsonNode Body(StubHandler handler) => JsonNode.Parse(handler.Requests.Single().Body!)!;

    /// <summary>The request's top-level <c>cache_control</c> (automatic caching), set the way F4's strategy will.</summary>
    private static ChatOptions AutomaticCaching(Ttl ttl) => new()
    {
        RawRepresentationFactory = _ => new MessageCreateParams
        {
            Model = "claude-haiku-5-5", MaxTokens = 1024, Messages = [],
            CacheControl = new CacheControlEphemeral { Ttl = ttl },
        },
    };

    private static List<ChatMessage> Messages(Ttl systemTtl) =>
    [
        new(ChatRole.System, [new TextContent("You are louis-agent.").WithCacheControl(systemTtl)]),
        new(ChatRole.User, "hi"),
    ];

    [Test]
    public async Task SystemMarker_ReachesTheWire_WithItsLifetime()
    {
        var (client, handler) = Client();

        await client.GetResponseAsync(Messages(Ttl.Ttl1h));

        JsonNode system = Body(handler)["system"]!.AsArray().Single()!;
        Assert.That(system["text"]!.GetValue<string>(), Is.EqualTo("You are louis-agent."));
        Assert.That(system["cache_control"]!.ToJsonString(), Is.EqualTo("""{"type":"ephemeral","ttl":"1h"}"""));
    }

    [Test]
    public async Task TopLevelCacheControl_ReachesTheWire_BesideTheSystemMarker()
    {
        // Diagram 5: marker 1 on the prefix (1 hour), marker 2 automatic on the tail (5 minutes), longer first.
        var (client, handler) = Client();

        await client.GetResponseAsync(Messages(Ttl.Ttl1h), AutomaticCaching(Ttl.Ttl5m));

        JsonNode body = Body(handler);
        Assert.That(body["cache_control"]!.ToJsonString(), Is.EqualTo("""{"type":"ephemeral","ttl":"5m"}"""));
        Assert.That(body["system"]!.AsArray().Single()!["cache_control"]!["ttl"]!.GetValue<string>(), Is.EqualTo("1h"));
        Assert.That(body["messages"]!.ToJsonString(), Does.Not.Contain("cache_control"), "no marker on the messages themselves");
    }

    [Test]
    public async Task TopLevelCacheControl_KeepsThinkingAndTheRest()
    {
        // The caching hook and thinking both go through the request options: one must not drop the other.
        var (client, handler) = Client();
        ChatOptions options = AutomaticCaching(Ttl.Ttl5m);
        options.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Medium };
        options.Tools = [AIFunctionFactory.Create(() => "x", "Probe")];

        await client.GetResponseAsync(Messages(Ttl.Ttl5m), options);

        JsonNode body = Body(handler);
        Assert.That(body["cache_control"], Is.Not.Null);
        Assert.That(body["thinking"], Is.Not.Null, "thinking is still sent");
        Assert.That(body["tools"]!.AsArray().Single()!["name"]!.GetValue<string>(), Is.EqualTo("Probe"));
    }

    [Test]
    public async Task TopLevelCacheControl_SurvivesTheThinkingBudgetWrapper()
    {
        // Pre-4.6 models get a thinking budget through the same hook; it chains to the previous factory.
        var (client, handler) = Client();
        ChatOptions options = AutomaticCaching(Ttl.Ttl5m);
        options.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Medium };

        await client.GetResponseAsync(Messages(Ttl.Ttl5m), LlmClientFactory.WithThinkingBudget(options, "claude-haiku-4-5"));

        JsonNode body = Body(handler);
        Assert.That(body["cache_control"]!["ttl"]!.GetValue<string>(), Is.EqualTo("5m"));
        Assert.That(body["thinking"]!["budget_tokens"]!.GetValue<long>(), Is.EqualTo(4_096));
    }

    [Test]
    public async Task Usage_WritesByLifetime_OnlyInTheRawResponse()
    {
        // F4 step 6 prices each write at its lifetime. UsageDetails carries only the total; the split (ephemeral_5m /
        // ephemeral_1h) is on the SDK's own Message, which the adapter hands back as RawRepresentation.
        var (client, _) = Client();

        ChatResponse response = await client.GetResponseAsync(Messages(Ttl.Ttl1h), AutomaticCaching(Ttl.Ttl5m));

        Assert.That(response.Usage!.AdditionalCounts, Is.EqualTo(new Dictionary<string, long> { ["CacheCreationInputTokens"] = 248 }),
            "the total only, as F1 found");
        var raw = (Message)response.RawRepresentation!;
        Assert.That((raw.Usage.CacheCreation!.Ephemeral5mInputTokens, raw.Usage.CacheCreation.Ephemeral1hInputTokens), Is.EqualTo((148L, 100L)));
    }
}