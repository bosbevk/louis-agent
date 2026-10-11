using System.Net;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Core;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests;

// F4 step 4: the caching strategy and its client. Anthropic's markers are checked on the captured request body, through
// the real adapter (as in AnthropicCachingWireTests), so the test sees what the API would see.
public class PromptCachingTests
{
    private const string Reply = """
        {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
         "content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn","stop_sequence":null,
         "usage":{"input_tokens":10,"output_tokens":2}}
        """;

    private static (IChatClient Client, StubHandler Handler) Anthropic(CacheLifetime prefix)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Reply, System.Text.Encoding.UTF8, "application/json"),
        });
        IChatClient adapter = new AnthropicClient(new ClientOptions { ApiKey = "test", HttpClient = new HttpClient(handler) })
            .AsIChatClient("claude-haiku-5-5", 1024);
        return (new PromptCachingChatClient(adapter, new AnthropicPromptCaching(prefix, "claude-haiku-5-5", 1024)), handler);
    }

    private static List<ChatMessage> Conversation() =>
    [
        new(ChatRole.System, "You are louis-agent."),
        new(ChatRole.User, "hi"),
        new(ChatRole.Assistant, "hello"),
        new(ChatRole.User, "fix the bug"),
    ];

    private static JsonNode Body(StubHandler handler, int request = 0) => JsonNode.Parse(handler.Requests[request].Body!)!;

    private static string? SystemTtl(JsonNode body) =>
        body["system"]?.AsArray().Last()!["cache_control"]?["ttl"]?.GetValue<string>();

    [TestCase(CacheLifetime.FiveMinutes, "5m")]
    [TestCase(CacheLifetime.OneHour, "1h")]
    public async Task Marker1_OnTheSystemPrompt_CarriesLlmCacheTtl(CacheLifetime prefix, string ttl)
    {
        var (client, handler) = Anthropic(prefix);

        await client.GetResponseAsync(Conversation());

        Assert.That(SystemTtl(Body(handler)), Is.EqualTo(ttl));
    }

    [TestCase(CacheLifetime.FiveMinutes)]
    [TestCase(CacheLifetime.OneHour)]
    public async Task Marker2_IsAutomaticAndFiveMinutes_NeverLongerThanMarker1(CacheLifetime prefix)
    {
        // The API rejects a 1-hour entry after a 5-minute one: marker 1 is 5m or 1h, marker 2 always 5m.
        var (client, handler) = Anthropic(prefix);

        await client.GetResponseAsync(Conversation());

        JsonNode body = Body(handler);
        Assert.That(body["cache_control"]!.ToJsonString(), Is.EqualTo("""{"type":"ephemeral","ttl":"5m"}"""));
        Assert.That(Minutes(SystemTtl(body)!), Is.GreaterThanOrEqualTo(Minutes("5m")));
    }

    private static int Minutes(string ttl) => ttl == "1h" ? 60 : 5;

    [Test]
    public async Task NoMarker_OnAnyMessageAfterTheSystemPrompt()
    {
        // Marker 2 is placed by the API, not by us; nothing in the messages carries one.
        var (client, handler) = Anthropic(CacheLifetime.OneHour);

        await client.GetResponseAsync(Conversation());

        Assert.That(Body(handler)["messages"]!.ToJsonString(), Does.Not.Contain("cache_control"));
    }

    [Test]
    public async Task TheCallersHistory_IsUnchanged()
    {
        // WithCacheControl changes the block it is called on; the history's blocks must never be marked.
        var (client, _) = Anthropic(CacheLifetime.OneHour);
        List<ChatMessage> history = Conversation();
        List<ChatMessage> before = [.. history];
        List<AIContent> systemBlocks = [.. history[0].Contents];

        await client.GetResponseAsync(history);

        Assert.That(history, Is.EqualTo(before).AsCollection, "same list, same message objects");
        Assert.That(history[0].Contents, Is.EqualTo(systemBlocks).AsCollection, "same system blocks");

        // Sent again without the strategy, the history carries no marker at all.
        IChatClient plain = PlainAdapter(out StubHandler plainHandler);
        await plain.GetResponseAsync(history);
        Assert.That(plainHandler.Requests.Single().Body, Does.Not.Contain("cache_control"));
    }

    private static IChatClient PlainAdapter(out StubHandler handler)
    {
        handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Reply, System.Text.Encoding.UTF8, "application/json"),
        });
        return new AnthropicClient(new ClientOptions { ApiKey = "test", HttpClient = new HttpClient(handler) })
            .AsIChatClient("claude-haiku-5-5", 1024);
    }

    [Test]
    public async Task TheCallersOptions_AreUnchanged()
    {
        var (client, _) = Anthropic(CacheLifetime.FiveMinutes);
        var options = new ChatOptions { Temperature = 0.5f };

        await client.GetResponseAsync(Conversation(), options);

        Assert.That(options.RawRepresentationFactory, Is.Null, "the hook went on a copy");
        Assert.That(options.Temperature, Is.EqualTo(0.5f));
    }

    [Test]
    public async Task ARequestWithoutASystemMessage_GetsOnlyTheAutomaticMarker()
    {
        var (client, handler) = Anthropic(CacheLifetime.OneHour);

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        JsonNode body = Body(handler);
        Assert.That(body["system"], Is.Null);
        Assert.That(body["cache_control"]!["ttl"]!.GetValue<string>(), Is.EqualTo("5m"));
    }

    [Test]
    public async Task ASummaryRequest_IsNotMarked()
    {
        // A summary is sent once; a marker would only pay the 1.25× write.
        var (client, handler) = Anthropic(CacheLifetime.OneHour);

        using (UsageScope.Begin(UsagePurpose.Summary))
            await client.GetResponseAsync(Conversation());

        JsonNode body = Body(handler);
        Assert.That(body["cache_control"], Is.Null);
        Assert.That(body.ToJsonString(), Does.Not.Contain("cache_control"));
    }

    [Test]
    public async Task ATurnRequest_IsMarked()
    {
        var (client, handler) = Anthropic(CacheLifetime.FiveMinutes);

        using (UsageScope.Begin(UsagePurpose.Turn))
            await client.GetResponseAsync(Conversation());

        Assert.That(Body(handler)["cache_control"], Is.Not.Null);
    }

    [Test]
    public async Task Streaming_IsPreparedTheSameWay()
    {
        // Hosts stream; the client prepares the streaming path exactly as the other.
        var inner = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var client = new PromptCachingChatClient(inner, new AnthropicPromptCaching(CacheLifetime.OneHour, "claude-haiku-5-5", 1024));
        List<ChatMessage> history = Conversation();

        await foreach (var _ in client.GetStreamingResponseAsync(history)) { }

        var (sent, options) = inner.Requests.Single();
        Assert.That(sent[0], Is.Not.SameAs(history[0]), "the system message is a marked copy");
        Assert.That(sent.Skip(1), Is.EqualTo(history.Skip(1)).AsCollection, "the other messages are the history's own");
        Assert.That(options?.RawRepresentationFactory, Is.Not.Null, "the top-level marker's hook is set");
    }

    [Test]
    public async Task NoPromptCaching_PassesTheRequestThroughUnchanged()
    {
        var inner = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var client = new PromptCachingChatClient(inner, NoPromptCaching.Instance);
        List<ChatMessage> history = Conversation();
        var options = new ChatOptions();

        await client.GetResponseAsync(history, options);

        var (sent, sentOptions) = inner.Requests.Single();
        Assert.That(sent, Is.EqualTo(history).AsCollection);
        Assert.That(sentOptions, Is.SameAs(options));
    }
}
