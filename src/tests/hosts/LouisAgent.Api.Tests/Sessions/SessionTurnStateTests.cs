using LouisAgent.Api.Endpoints.Sessions;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Prompting.Providers;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// What a session knows of its running turn, for the line the API logs when it refuses a message as still answering
/// (FX252): when the turn began and whether it was already cancelled, so the next time a chat is stuck the log says
/// whether its turn was still working or only finishing a tool after the page let go.
/// </summary>
[NonParallelizable]
public class SessionTurnStateTests
{
    /// <summary>A session on an engine that never needs its model here.</summary>
    private static AgentSession Session(TimeProvider clock) =>
        new("sess_busy", new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(), TestPaths.Agent(),
            toolsets: []), [], 0, UsageTags.None, clock: clock);

    /// <summary>A turn records when it began, and whether it was cancelled; ending it forgets both.</summary>
    [Test]
    public void ATurn_KnowsWhenItBegan_AndWhetherItWasCancelled()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero));
        var session = Session(clock);

        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);
        var began = session.TurnStartedAt;
        session.Cancel();
        bool cancelled = session.TurnCancelled;
        session.EndTurn(turn);

        Assert.Multiple(() =>
        {
            Assert.That(began, Is.EqualTo(clock.GetUtcNow()));
            Assert.That(cancelled, Is.True);
            Assert.That(session.TurnStartedAt, Is.Null);
            Assert.That(session.TurnCancelled, Is.False);
        });
    }

    /// <summary>The refusal logs the session, how long its turn has run, and whether it was cancelled.</summary>
    [Test]
    public void TheRefusal_LogsHowLongTheTurnHasRun_AndWhetherItWasCancelled()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero));
        var session = Session(clock);
        Assert.That(session.TryStartTurn(CancellationToken.None, out _), Is.True);
        session.Cancel();
        clock.Advance(TimeSpan.FromSeconds(587));

        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            AgentLogging.Configure(null);
            SessionHandlers.RefuseBusy(session);
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(stderr.ToString(), Does.Contain("sess_busy").And.Contain("587 s").And.Contain("cancelled: yes"));
    }
}
