using System.Net;
using Anthropic;
using Anthropic.Core;
using LouisAgent.Core.Anthropic.Failures;
using LouisAgent.Core.Llm.Failures;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.Tests.Failures;

/// <summary>
/// F8's proof on the SDK's own client: an overload that arrives inside a stream, before any words, is asked again and
/// answered; a spend limit stops at once as an account limit, asked only once.
/// </summary>
public class RetryingOnTheSdkTests
{
    /// <summary>A whole answer, "ok", as Anthropic streams it.</summary>
    private const string Answer = """
                                  event: message_start
                                  data: {"type":"message_start","message":{"id":"msg_1","type":"message","role":"assistant","model":"claude-haiku-5-5","content":[],"stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":3,"output_tokens":1}}}

                                  event: content_block_start
                                  data: {"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}

                                  event: content_block_delta
                                  data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"ok"}}

                                  event: content_block_stop
                                  data: {"type":"content_block_stop","index":0}

                                  event: message_delta
                                  data: {"type":"message_delta","delta":{"stop_reason":"end_turn","stop_sequence":null},"usage":{"output_tokens":1}}

                                  event: message_stop
                                  data: {"type":"message_stop"}


                                  """;

    /// <summary>
    /// A stream that opens, then is overloaded before any words, as Anthropic sends it at a busy moment.
    /// </summary>
    private const string Overloaded = """
                                      event: message_start
                                      data: {"type":"message_start","message":{"id":"msg_0","type":"message","role":"assistant","model":"claude-haiku-5-5","content":[],"stop_reason":null,"stop_sequence":null,"usage":{"input_tokens":3,"output_tokens":1}}}

                                      event: error
                                      data: {"type":"error","error":{"type":"overloaded_error","message":"Overloaded"}}


                                      """;

    /// <summary>
    /// The SDK's client over a stub that answers with these, in turn, wrapped in the retrying client.
    /// </summary>
    private static (IChatClient Client, Func<int> Requests) Client(params Func<HttpResponseMessage>[] answers)
    {
        int requests = 0;
        var handler = new StubHandler(_ => answers[Math.Min(requests++, answers.Length - 1)]());
        var sdk = new AnthropicClient(new ClientOptions
        {
            ApiKey = "test",
            HttpClient = new HttpClient(handler),
            MaxRetries = 0
        }).AsIChatClient("claude-haiku-5-5", 64);
        var retrying = new RetryingChatClient(sdk, new AnthropicFailures(), maxRetries: 2,
            wait: (_, _) => Task.CompletedTask);
        return (retrying, () => requests);
    }

    /// <summary>A stream's body.</summary>
    private static HttpResponseMessage Stream(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body.ReplaceLineEndings("\n"), System.Text.Encoding.UTF8,
                "text/event-stream")
        };

    /// <summary>An overload inside the stream, before any words, is asked again, and the answer comes.</summary>
    [Test]
    public async Task AnOverloadInsideAStream_IsAskedAgain_AndAnswered()
    {
        var (client, requests) = Client(() => Stream(Overloaded), () => Stream(Answer));

        string text = "";
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            text += update.Text;

        Assert.Multiple(() =>
        {
            Assert.That(text, Is.EqualTo("ok"));
            Assert.That(requests(), Is.EqualTo(2));
        });
    }

    /// <summary>A spend limit is an account limit at once, asked only once, with when access comes back.</summary>
    [Test]
    public void ASpendLimit_StopsAtOnce_AsAnAccountLimit()
    {
        const string body = "{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\",\"message\":" +
                            "\"You have reached your specified workspace API usage limits. " +
                            "You will regain access on 2026-12-01 at 00:00 UTC.\"}}";
        var (client, requests) = Client(() => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        });

        var limit = Assert.ThrowsAsync<AccountLimitException>(async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(limit!.ResumesAt, Is.EqualTo(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(requests(), Is.EqualTo(1));
        });
    }
}
