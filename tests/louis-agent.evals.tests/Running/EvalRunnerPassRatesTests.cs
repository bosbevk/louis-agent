using LouisAgent.Testing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>
/// The runner writes a mode's pass@k and pass^k on its mode's end (the one copy of the formula is the harness's; the
/// API only reads them), counts the trials left out for the infrastructure, and runs a trial once more when its turn
/// failed with a transport error and with nothing else (a timeout, an API error or a replay miss would not change on a
/// second try).
/// </summary>
public class EvalRunnerPassRatesTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>A stream event that says the text.</summary>
    private static SessionEvent Said(string text) =>
        SessionEvent.From("content_block_delta",
            JsonDocument.Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"""
                               + JsonSerializer.Serialize(text) + "}}").RootElement.Clone());

    /// <summary>What one send does: answers, or throws.</summary>
    private abstract record Send
    {
        /// <summary>The send answers with this text.</summary>
        public sealed record Answers(string Text) : Send;

        /// <summary>The send throws this error.</summary>
        public sealed record Throws(Exception Error) : Send;
    }

    /// <summary>
    /// Runs the one-scenario eval over the scripted sends, and hands back its events, client and log.
    /// </summary>
    private static async Task<(IReadOnlyList<EvalEvent> Events, ScriptedClient Client, CapturingLogger Log)> Run(
        int repeats, params Send[] sends)
    {
        var sink = new StringWriter();
        var client = new ScriptedClient(sends);
        var log = new CapturingLogger();
        var runner = new EvalRunner(client, new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At),
            new TypedLogger(log));

        await runner.RunAsync(new OneScenarioEval(), new EvalRun("run_1", repeats), CancellationToken.None);

        return (Written.Events(sink), client, log);
    }

    /// <summary>A send that answers right.</summary>
    private static Send Right => new Send.Answers("Hallo");

    /// <summary>A send that answers wrong.</summary>
    private static Send Wrong => new Send.Answers("Nee");

    /// <summary>Each mode's end carries pass@k and pass^k over the repeats so far, and its k.</summary>
    [Test]
    public async Task ModeDone_CarriesThePassRates_OverTheRepeatsSoFar()
    {
        var (events, _, _) = await Run(2, Right, Wrong);

        var done = events.OfType<ModeDoneEvent>().Select(d => d.Rates).ToList();
        Assert.That(done[0], Is.EqualTo(new RatesInfo(1, 1, 1, 0, 0)));
        Assert.That(done[1], Is.EqualTo(new RatesInfo(1, 0, 2, 0, 0)),
            "it passed once in two trials, but not every time");
    }

    /// <summary>A trial left out for the infrastructure is counted, and the rates are left out, never 0.</summary>
    [Test]
    public async Task ATrialLeftOutForTheInfrastructure_IsCounted_AndTheRatesAreLeftOutNotZero()
    {
        var (events, _, _) = await Run(1, new Send.Throws(new TimeoutException("too slow")));

        Assert.That(events.OfType<ConversationEvent>().Single().Outcome, Is.EqualTo(TrialOutcome.Infrastructure));
        Assert.That(events.OfType<ModeDoneEvent>().Single().Rates, Is.EqualTo(new RatesInfo(null, null, 1, 1, 0)),
            "no countable trial: no rate, never 0");
    }

    /// <summary>A transport failure is run once more from a new session, and the second attempt is the trial.</summary>
    [Test]
    public async Task ATransportFailure_IsRunOnceMore_FromANewSession_AndTheSecondAttemptIsTheTrial()
    {
        var (events, client, log) = await Run(1, new Send.Throws(new HttpRequestException("reset")), Right);

        Assert.That(client.Opened, Is.EqualTo(new[] { "sess_1", "sess_2" }), "a new session for the second attempt");
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_1", "sess_2" }), "both are closed");
        Assert.That(events.OfType<ConversationEvent>().Single().Outcome, Is.EqualTo(TrialOutcome.Pass),
            "one trial, one conversation");
        Assert.That(events.OfType<ModeDoneEvent>().Single().Rates, Is.EqualTo(new RatesInfo(1, 1, 1, 0, 1)));
        Assert.That(events.OfType<TurnEvent>().Select(t => t.Failure),
            Is.EqualTo(new TurnFailure?[] { TurnFailure.Transport, null }),
            "the failed attempt stays in the protocol; the same turn key written again replaces it in the store");
        Assert.That(log.Warnings.Any(w => w.Contains("Retrying") && w.Contains("pannekoek")), Is.True,
            "the retry is logged");
    }

    /// <summary>A second transport failure is not retried again, and the trial is an infrastructure one.</summary>
    [Test]
    public async Task ATransportFailureTwice_IsRetriedOnlyOnce_AndIsAnInfrastructureTrial()
    {
        var (events, client, _) = await Run(1, new Send.Throws(new HttpRequestException("reset")),
            new Send.Throws(new IOException("broken pipe")));

        Assert.That(client.Opened.Count, Is.EqualTo(2), "one retry, not a loop");
        Assert.That(events.OfType<ConversationEvent>().Single().Outcome, Is.EqualTo(TrialOutcome.Infrastructure));
        var rates = events.OfType<ModeDoneEvent>().Single().Rates!;
        Assert.That((rates.Retried, rates.Infrastructure), Is.EqualTo((1, 1)));
    }

    /// <summary>Only a transport failure is run again: a timeout or an API error would not change.</summary>
    [Test]
    public async Task OnlyTransportIsRetried_ATimeoutAndAnApiErrorWouldNotChange()
    {
        var (_, timedOut, _) = await Run(1, new Send.Throws(new TimeoutException("too slow")), Right);
        var (_, refused, _) = await Run(1, new Send.Throws(new EvalApiException(500, "The model is down.")), Right);

        Assert.That(timedOut.Opened.Count, Is.EqualTo(1));
        Assert.That(refused.Opened.Count, Is.EqualTo(1));
    }

    /// <summary>A client that plays its sends in order, opening a new session each time.</summary>
    private sealed class ScriptedClient(IEnumerable<Send> sends) : ISessionClient
    {
        /// <summary>The sends still to play.</summary>
        private readonly Queue<Send> _sends = new(sends);

        /// <summary>The sessions opened, in order.</summary>
        public List<string> Opened { get; } = [];

        /// <summary>The sessions closed, in order.</summary>
        public List<string> Closed { get; } = [];

        /// <summary>No toolsets.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        /// <summary>Opens the next numbered session.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
        {
            Opened.Add($"sess_{Opened.Count + 1}");
            return Task.FromResult(Opened[^1]);
        }

        /// <summary>Plays the next send, answering "Hallo" once the script is done.</summary>
        public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var send = _sends.Count > 0 ? _sends.Dequeue() : new Send.Answers("Hallo");
            if (send is Send.Throws throws) throw throws.Error;
            yield return Said(((Send.Answers)send).Text);
        }

        /// <summary>Notes the close.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            return Task.CompletedTask;
        }
    }

    /// <summary>The trial passes when the answer is "Hallo".</summary>
    private sealed class AnswerGrader : IGrader
    {
        /// <inheritdoc />
        public string Name => "says-hallo";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public GraderKind Kind => GraderKind.FixedRule;

        /// <inheritdoc />
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <inheritdoc />
        public bool Required => true;

        /// <summary>Passes when the last turn's answer is "Hallo".</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(EngineEvents.AnswerOf(trial.Transcript.Turns[^1]) == "Hallo"
                ? new Grade(Name, 1, 1, GradeOutcome.Pass, "said hallo")
                : new Grade(Name, 1, 0, GradeOutcome.Fail, "did not"));
    }

    /// <summary>An eval of one scenario of one message, graded by its answer.</summary>
    private sealed class OneScenarioEval : IEval
    {
        /// <inheritdoc />
        public string Name => "stub-rates";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "pass@k and pass^k.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("pannekoek", new EvalInput(null, "Africa/Johannesburg", ["Vraag"]), default, [],
                DatasetSplit.Dev)
        ];

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders { get; } = [new AnswerGrader()];

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    /// <summary>The capturing logger, as the runner's typed logger.</summary>
    private sealed class TypedLogger(ILogger inner) : ILogger<EvalRunner>
    {
        /// <summary>The inner logger's scope.</summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

        /// <summary>Whatever the inner logger takes.</summary>
        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        /// <summary>Passes the entry on.</summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
