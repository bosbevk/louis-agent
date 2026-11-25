using Anthropic.Models.Messages;
using LouisAgent.Core.Anthropic.Thinking;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm.Thinking;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.Tests.Thinking;

/// <summary>
/// Anthropic's way with a resumed chat whose tools moved: its saved thinking is kept, and every request asks the API
/// to drop the blocks that fail the prefix check, with the beta header that setting needs, keeping the turn's effort
/// and display.
/// </summary>
public class AnthropicDropBlockTests
{
    /// <summary>A turn's options with thinking on at the effort given.</summary>
    private static ChatOptions Thinking(ReasoningEffort effort) => new()
    {
        MaxOutputTokens = 16_000,
        Reasoning = new ReasoningOptions
        {
            Effort = effort,
            Output = ReasoningOutput.Full
        }
    };

    /// <summary>The request the prepared options make.</summary>
    private static MessageCreateParams Raw(ChatOptions prepared) =>
        (MessageCreateParams)prepared.RawRepresentationFactory!(null!)!;

    /// <summary>
    /// The request carries the beta header and drop_block, and keeps the effort and display the adapter would have
    /// sent, since it stops adding them once the request names its own thinking.
    /// </summary>
    [Test]
    public void Prepare_AsksToDropFailingBlocks_KeepingEffortAndDisplay()
    {
        var raw = Raw(new AnthropicDropBlock("claude-haiku-5-5").Prepare(Thinking(ReasoningEffort.High)));

        var thinking = ((ThinkingConfigAdaptive)raw.Thinking!.Value).RawData;
        Assert.Multiple(() =>
        {
            Assert.That(raw.RawHeaderData["anthropic-beta"].GetString(),
                Is.EqualTo("thinking-binding-controls-2026-08-01"));
            Assert.That(thinking["block_binding"].GetProperty("prefix_mismatch_behavior").GetString(),
                Is.EqualTo("drop_block"));
            Assert.That(thinking["display"].GetString(), Is.EqualTo("summarized"));
            Assert.That(raw.RawBodyData["output_config"].GetProperty("effort").GetString(), Is.EqualTo("high"));
            Assert.That(raw.Model.ToString(), Does.Contain("claude-haiku-5-5"));
        });
    }

    /// <summary>
    /// Through the real adapter to a stub of the API, the request carries the beta header, drop_block, and the turn's
    /// effort and display: what a paid call on Haiku 5.5 showed the API answers instead of a 400.
    /// </summary>
    [Test]
    public async Task Prepare_ReachesTheWire()
    {
        string? beta = null;
        var handler = new StubHandler(request =>
        {
            beta = request.Headers.TryGetValues("anthropic-beta", out var values) ? string.Join(",", values) : null;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(Reply, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var chat = new global::Anthropic.AnthropicClient(new global::Anthropic.Core.ClientOptions
        {
            ApiKey = "test",
            HttpClient = new HttpClient(handler)
        }).AsIChatClient("claude-haiku-5-5", 16_000);
        var prepared = new AnthropicDropBlock("claude-haiku-5-5").Prepare(Thinking(ReasoningEffort.High));

        await chat.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], prepared);

        var body = System.Text.Json.Nodes.JsonNode.Parse(handler.Requests.Single().Body!)!;
        Assert.Multiple(() =>
        {
            Assert.That(beta, Does.Contain("thinking-binding-controls-2026-08-01"));
            Assert.That(body["thinking"]!["block_binding"]!["prefix_mismatch_behavior"]!.GetValue<string>(),
                Is.EqualTo("drop_block"));
            Assert.That(body["thinking"]!["display"]!.GetValue<string>(), Is.EqualTo("summarized"));
            Assert.That(body["output_config"]!["effort"]!.GetValue<string>(), Is.EqualTo("high"));
        });
    }

    /// <summary>A Messages API reply with one text block, for the stub to answer with.</summary>
    private const string Reply = """
                                 {"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5",
                                  "content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn","stop_sequence":null,
                                  "usage":{"input_tokens":10,"output_tokens":2}}
                                 """;

    /// <summary>With thinking off the setting is refused by the API, so the request is left as it was.</summary>
    [Test]
    public void Prepare_WithThinkingOff_LeavesTheRequestAlone()
    {
        var options = new ChatOptions { MaxOutputTokens = 16_000 };

        Assert.That(new AnthropicDropBlock("claude-haiku-5-5").Prepare(options), Is.SameAs(options));
    }

    /// <summary>The saved turns are sent whole: the API, not us, drops what no longer matches.</summary>
    [Test]
    public void Restore_KeepsTheThinking()
    {
        ChatMessage[] saved =
            [new(ChatRole.Assistant, [new TextReasoningContent("A thought."), new TextContent("Hi.")])];

        var restored = new AnthropicDropBlock("claude-haiku-5-5").Restore(saved);

        Assert.That(restored.Single().Contents.OfType<TextReasoningContent>().Single().Text, Is.EqualTo("A thought."));
    }

    /// <summary>
    /// The provider asks for drop_block only on the models Anthropic's page lists; any other strips the thinking.
    /// </summary>
    [TestCase("claude-haiku-5-5", true)]
    [TestCase("claude-opus-5-5", true)]
    [TestCase("claude-fable-5-1", true)]
    [TestCase("claude-haiku-4-5", false)]
    [TestCase("claude-opus-5", false)]
    public void TheProvider_PicksDropBlock_OnlyForModelsThatTakeIt(string model, bool dropsBlocks)
    {
        var options = new LlmOptions
        {
            Provider = "anthropic",
            Model = model
        };

        IStaleThinking chosen = AnthropicProvider.Instance.CreateStaleThinking(options);

        Assert.That(chosen is AnthropicDropBlock, Is.EqualTo(dropsBlocks));
    }
}
