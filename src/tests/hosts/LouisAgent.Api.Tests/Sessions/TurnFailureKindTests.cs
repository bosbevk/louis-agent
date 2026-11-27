using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Composition.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Llm.Failures;
using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Prompting.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A failed turn's error event says what kind of failure it was (FX26): a replay with no recorded answer streams
/// <c>replay_miss_error</c>, so a reader can mark the turn a replay miss; any other failure still streams
/// <c>api_error</c>, as before.
/// </summary>
public class TurnFailureKindTests
{
    /// <summary>Streams one turn whose model request throws this, and hands back the stream's data.</summary>
    private static async Task<string> FailedTurnAsync(Exception thrown)
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(_ => throw thrown),
            TestPaths.Agent(), toolsets: [], usageSink: new ListUsageSink());
        var session = new AgentSession("sess_fail", engine, [TestToolset.Web.Wire()], 0, UsageTags.None);
        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);

        var written = new List<string>();
        await foreach (var item in TurnStream.StreamTurnAsync(engine, session, "Wat kos bobotie?", turn))
            written.Add(item.Data ?? "");
        return string.Join("\n", written);
    }

    /// <summary>A replay miss streams its own kind, with its message.</summary>
    [Test]
    public async Task AReplayMiss_StreamsTheReplayMissKind()
    {
        string stream = await FailedTurnAsync(new ModelReplayMissException("No recorded model answer here."));

        Assert.That(stream, Does.Contain($"\"type\":\"{StreamErrorKind.ReplayMiss.Wire()}\"")
            .And.Contain("No recorded model answer here."));
    }

    /// <summary>Any other failure streams the API's own kind, as it always did.</summary>
    [Test]
    public async Task AnyOtherFailure_StreamsTheApisKind()
    {
        string stream = await FailedTurnAsync(new InvalidOperationException("Something else broke."));

        Assert.That(stream, Does.Contain($"\"type\":\"{StreamErrorKind.Api.Wire()}\"")
            .And.Not.Contain(StreamErrorKind.ReplayMiss.Wire()));
    }

    /// <summary>
    /// The API's own failure streams a plain sentence a visitor can read, never the exception's name or text (FX239:
    /// a rejected model key showed a visitor <c>AnthropicUnauthorizedException</c> and the provider's answer).
    /// </summary>
    /// <summary>
    /// An account limit streams its own kind (F8), in our words with when the agent answers again, never the
    /// provider's sentence, which names its workspace and its limits (FX239).
    /// </summary>
    [Test]
    public async Task AnAccountLimit_StreamsItsKind_InOurWords_WithWhenItLifts()
    {
        var limit = ModelFailure.AccountLimit(
            "You have reached your specified workspace API usage limits. You will regain access on 2026-12-01 at " +
            "00:00 UTC.", new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

        string stream = await FailedTurnAsync(new AccountLimitException(limit));

        Assert.Multiple(() =>
        {
            Assert.That(stream, Does.Contain($"\"type\":\"{StreamErrorKind.AccountLimit.Wire()}\""));
            Assert.That(stream, Does.Contain("2026-12-01 00:00 UTC"));
            Assert.That(stream, Does.Not.Contain("workspace API usage limits"));
        });
    }

    /// <summary>An account limit with no time it lifts says the agent's owner must raise it.</summary>
    [Test]
    public async Task AnAccountLimitWithNoTime_SaysItsOwnerMustRaiseIt()
    {
        string stream = await FailedTurnAsync(
            new AccountLimitException(ModelFailure.AccountLimit("Your credit balance is too low.", null)));

        Assert.Multiple(() =>
        {
            Assert.That(stream, Does.Contain($"\"type\":\"{StreamErrorKind.AccountLimit.Wire()}\""));
            Assert.That(stream, Does.Contain(MessageText.Format(ApiUserText.AccountLimitUntilRaised)));
            Assert.That(stream, Does.Not.Contain("credit balance"));
        });
    }

    [Test]
    public async Task TheApisFailure_StreamsAPlainSentence_NotTheException()
    {
        string stream = await FailedTurnAsync(new InvalidOperationException("invalid x-api-key"));

        Assert.That(stream, Does.Contain(MessageText.Format(ApiUserText.TurnFailed))
            .And.Not.Contain(nameof(InvalidOperationException))
            .And.Not.Contain("invalid x-api-key"));
    }
}
