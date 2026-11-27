using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Core;
using LouisAgent.Core.Agent.EngineTools.ToolSearch;
using LouisAgent.Core.Anthropic.ToolSearch;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.Tests.ToolSearch;

/// <summary>
/// Anthropic's tool search on the wire (F97.3): a tool that waits goes with <c>defer_loading</c>, the ones kept loaded
/// without it, and what FindTool found goes back as <c>tool_reference</c> blocks, while the chat's own messages and
/// options are never changed.
/// </summary>
public class AnthropicToolSearchTests
{
    /// <summary>A Messages API reply with nothing in it but a word.</summary>
    private const string Reply = """
                                 {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
                                  "content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn","stop_sequence":null,
                                  "usage":{"input_tokens":10,"output_tokens":2}}
                                 """;

    /// <summary>The SDK's own adapter over a stub, wrapped in the strategy with ShowCommit waiting.</summary>
    private static (IChatClient Client, StubHandler Handler) Client()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Reply, System.Text.Encoding.UTF8, "application/json")
        });
        var anthropic = new AnthropicClient(new ClientOptions
        {
            ApiKey = "test",
            HttpClient = new HttpClient(handler)
        });
        IReadOnlySet<string> deferred = new HashSet<string> { "ShowCommit" };
        var client =
            new AnthropicToolSearch().Around(anthropic.AsIChatClient("claude-haiku-5-5", 1024), () => deferred);
        return (client, handler);
    }

    /// <summary>The request body the API would have been sent.</summary>
    private static JsonNode Body(StubHandler handler) => JsonNode.Parse(handler.Requests.Single().Body!)!;

    /// <summary>The chat's two tools: GetStatus kept loaded, ShowCommit waiting.</summary>
    private static ChatOptions Options() => new()
    {
        Tools =
        [
            AIFunctionFactory.Create(() => "clean", "GetStatus", "Show what changed."),
            AIFunctionFactory.Create(() => "a commit", "ShowCommit", "Show the last commit.")
        ]
    };

    /// <summary>
    /// A chat in which the model searched and FindTool found ShowCommit, as the history keeps it: JSON.
    /// </summary>
    private static List<ChatMessage> Searched() =>
    [
        new(ChatRole.User, "what does the last commit say?"),
        new(ChatRole.Assistant, [new FunctionCallContent("toolu_1", "FindTool")]),
        new(ChatRole.Tool, [
            new FunctionResultContent("toolu_1",
                JsonSerializer.SerializeToElement(new FoundTools(["ShowCommit"]), AIJsonUtilities.DefaultOptions))
        ])
    ];

    /// <summary>The waiting tool goes with defer_loading, the kept one without it.</summary>
    [Test]
    public async Task AToolThatWaits_GoesDeferred_TheKeptOneDoesNot()
    {
        var (client, handler) = Client();

        await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], Options());

        var tools = Body(handler)["tools"]!.AsArray();
        var byName = tools.ToDictionary(t => t!["name"]!.GetValue<string>(), t => t!);
        Assert.Multiple(() =>
        {
            Assert.That(byName["ShowCommit"]["defer_loading"]?.GetValue<bool>(), Is.True);
            Assert.That(byName["GetStatus"]["defer_loading"], Is.Null);
        });
    }

    /// <summary>What FindTool found goes back as tool references, one per name, inside its tool result.</summary>
    [Test]
    public async Task WhatFindToolFound_GoesBack_AsToolReferences()
    {
        var (client, handler) = Client();

        await client.GetResponseAsync(Searched(), Options());

        var result = Body(handler)["messages"]!.AsArray()
            .SelectMany(m => m!["content"] is JsonArray content ? content : [])
            .Single(block => block!["type"]!.GetValue<string>() == "tool_result")!;
        Assert.That(result["content"]!.ToJsonString(),
            Is.EqualTo("""[{"type":"tool_reference","tool_name":"ShowCommit"}]"""));
    }

    /// <summary>The chat's own history and options are left as they were: only the request sent is shaped.</summary>
    [Test]
    public async Task TheChatsOwnMessagesAndOptions_AreNeverChanged()
    {
        var (client, _) = Client();
        var messages = Searched();
        var options = Options();

        await client.GetResponseAsync(messages, options);

        var kept = messages[2].Contents.OfType<FunctionResultContent>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(kept.Result, Is.InstanceOf<JsonElement>());
            Assert.That(options.Tools!.All(t => t.AdditionalProperties.Count == 0), Is.True);
        });
    }
}
