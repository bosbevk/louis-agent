using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.Failures;
using LouisAgent.Core.Llm.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Failures;

/// <summary>
/// What a model's failure is (F8): worth asking again, an account limit, or neither, as its provider says. A provider
/// that says nothing calls every failure "other", so nothing is asked again; an account limit is its own exception,
/// carrying the reason and when access comes back.
/// </summary>
public class ModelFailuresTests
{
    /// <summary>A provider that leaves failures alone, as OpenAI's and Ollama's do.</summary>
    private sealed class PlainProvider : ILlmProvider
    {
        public string Name => "plain";

        public IChatClient Create(LlmOptions options) => new FakeChatClient();
    }

    /// <summary>
    /// A provider that names no failure calls each one "other", through the factory as the engine asks.
    /// </summary>
    [Test]
    public void AProviderThatNamesNothing_CallsEveryFailureOther()
    {
        var options = new LlmOptions
        {
            Provider = "plain",
            Model = "m"
        };
        var failures = new LlmClientFactory([new PlainProvider()]).CreateFailures(options);

        var failure = failures.Of(new HttpRequestException("busy"));

        Assert.Multiple(() =>
        {
            Assert.That(failures, Is.SameAs(NoModelFailures.Instance));
            Assert.That(failure.Kind, Is.EqualTo(ModelFailureKind.Other));
        });
    }

    /// <summary>An account limit says its reason, and keeps when access comes back for whoever stops on it.</summary>
    [Test]
    public void AnAccountLimit_SaysItsReason_AndKeepsWhenItLifts()
    {
        var resumes = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var limit = ModelFailure.AccountLimit("You have reached your specified workspace API usage limits.", resumes);

        var thrown = new AccountLimitException(limit);

        Assert.Multiple(() =>
        {
            Assert.That(limit.Kind, Is.EqualTo(ModelFailureKind.AccountLimit));
            Assert.That(thrown.ResumesAt, Is.EqualTo(resumes));
            Assert.That(thrown.Message, Does.Contain("usage limits"));
        });
    }
}
