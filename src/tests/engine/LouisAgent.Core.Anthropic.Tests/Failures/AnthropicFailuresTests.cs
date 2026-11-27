using System.Net;
using Anthropic;
using Anthropic.Core;
using LouisAgent.Core.Anthropic.Failures;
using LouisAgent.Core.Llm.Failures;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Anthropic.Tests.Failures;

/// <summary>
/// Claude's failures named (F8), on the exceptions the SDK really throws, caught from its own client over a stub with
/// its retries off: an overload or a server error inside a stream is worth asking again; the three account limits
/// are account limits, with the time access comes back read from Anthropic's own sentence; anything else is neither.
/// </summary>
public class AnthropicFailuresTests
{
    /// <summary>The sentence Anthropic ends an account limit with, as it answered us on 2026-11-27.</summary>
    private const string Regain = "You will regain access on 2026-12-01 at 00:00 UTC.";

    /// <summary>When that sentence says access comes back.</summary>
    private static readonly DateTimeOffset Resumes = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The exception the SDK's own client throws for this answer, retries off.</summary>
    private static async Task<Exception> Thrown(HttpResponseMessage answer)
    {
        var client = new AnthropicClient(new ClientOptions
        {
            ApiKey = "test",
            HttpClient = new HttpClient(new StubHandler(_ => answer)),
            MaxRetries = 0
        }).AsIChatClient("claude-haiku-5-5", 64);
        try
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
            {
            }
        }
        catch (Exception thrown)
        {
            return thrown;
        }

        throw new AssertionException("the SDK threw nothing");
    }

    /// <summary>An error answer, as JSON with the status given.</summary>
    private static HttpResponseMessage Error(int status, string type, string message, string? code = null)
    {
        string details = code is null ? "" : ",\"details\":{\"error_code\":\"" + code + "\"}";
        string body = "{\"type\":\"error\",\"error\":{\"type\":\"" + type + "\",\"message\":\"" + message + "\"" +
                      details + "}}";
        return new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
    }

    /// <summary>An error that arrives as an event inside a stream that has begun.</summary>
    private static HttpResponseMessage InStream(string type) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "event: error\ndata: {\"type\":\"error\",\"error\":{\"type\":\"" + type + "\",\"message\":\"x\"}}\n\n",
                System.Text.Encoding.UTF8, "text/event-stream")
        };

    /// <summary>An overload or a server error inside a stream is a busy moment, worth asking again.</summary>
    [TestCase("overloaded_error")]
    [TestCase("api_error")]
    public async Task AnErrorInsideAStream_IsWorthAskingAgain(string type)
    {
        var failure = new AnthropicFailures().Of(await Thrown(InStream(type)));

        Assert.That(failure.Kind, Is.EqualTo(ModelFailureKind.Transient));
    }

    /// <summary>
    /// The three account limits are account limits, with Anthropic's reason and when access comes back.
    /// </summary>
    [TestCase(400, "invalid_request_error", "You have reached your specified workspace API usage limits. " + Regain,
        null)]
    [TestCase(400, "invalid_request_error", "You have reached your specified API usage limits. " + Regain, null)]
    [TestCase(429, "rate_limit_error", "You have reached your API usage limits: your organization has crossed its " +
                                       "monthly API usage threshold. " + Regain, "enforced_spend_limit_reached")]
    public async Task AnAccountLimit_IsOne_WithWhenItLifts(int status, string type, string message, string? code)
    {
        var failure = new AnthropicFailures().Of(await Thrown(Error(status, type, message, code)));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Kind, Is.EqualTo(ModelFailureKind.AccountLimit));
            Assert.That(failure.Reason, Is.EqualTo(message));
            Assert.That(failure.ResumesAt, Is.EqualTo(Resumes));
        });
    }

    /// <summary>A billing problem is an account limit too, with no time it lifts.</summary>
    [Test]
    public async Task ABillingError_IsAnAccountLimit()
    {
        var failure =
            new AnthropicFailures().Of(await Thrown(Error(402, "billing_error", "Your credit balance is too low.")));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Kind, Is.EqualTo(ModelFailureKind.AccountLimit));
            Assert.That(failure.ResumesAt, Is.Null);
        });
    }

    /// <summary>
    /// Anything else is neither: a bad request, an ordinary rate limit (the SDK already waited and asked again), and a
    /// failure that isn't the SDK's.
    /// </summary>
    [Test]
    public async Task AnythingElse_IsNeither()
    {
        var failures = new AnthropicFailures();
        var badRequest = await Thrown(Error(400, "invalid_request_error", "messages: field required"));
        var rateLimit = await Thrown(Error(429, "rate_limit_error",
            "Number of request tokens has exceeded your rate limit."));

        Assert.Multiple(() =>
        {
            Assert.That(failures.Of(badRequest).Kind, Is.EqualTo(ModelFailureKind.Other));
            Assert.That(failures.Of(rateLimit).Kind, Is.EqualTo(ModelFailureKind.Other));
            Assert.That(failures.Of(new InvalidOperationException("x")).Kind, Is.EqualTo(ModelFailureKind.Other));
        });
    }
}
