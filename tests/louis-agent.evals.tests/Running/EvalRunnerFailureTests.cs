using LouisAgent.Testing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Tests.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>
/// A turn that fails for the infrastructure is recorded with its failure, not thrown: the run goes on, and the
/// session is still closed. A cancelled run still throws.
/// </summary>
public class EvalRunnerFailureTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>A stream event that says "Hallo".</summary>
    private static readonly SessionEvent Hallo =
        SessionEvent.From("content_block_delta",
            JsonDocument
                .Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")
                .RootElement.Clone());

    /// <summary>The lines the last run wrote, kept even when the run threw.</summary>
    private static string[] _lines = [];

    /// <summary>Runs the eval against the client, keeping the lines it wrote whatever happens.</summary>
    private static async Task<FailingClient> Run(FailingClient client, int messages = 1,
        CancellationToken token = default)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(client, new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At),
            NullLogger<EvalRunner>.Instance);

        // The lines are kept in a finally, so a run that throws still leaves what it wrote.
        try
        {
            await runner.RunAsync(new TwoTurnEval(messages), new EvalRun("run_1", 1), token);
        }
        finally
        {
            _lines = Written.Lines(sink);
        }

        return client;
    }

    /// <summary>The last run's events of one kind.</summary>
    private static IEnumerable<T> Of<T>() where T : EvalEvent => Written.Events(_lines).OfType<T>();

    /// <summary>
    /// A stream that breaks partway keeps what it read, is run once more, and ends in a transport failure.
    /// </summary>
    [Test]
    public async Task AStreamThatBreaksPartway_KeepsItsFirstEvents_AndGetsTransport()
    {
        var client = await Run(new FailingClient
        {
            Events = [Hallo],
            SendFails = new HttpRequestException("the connection was reset")
        });

        // A transport failure is run once more: this client breaks every time, so the trial has two attempts, each
        // recorded and each closed.
        Assert.That(Of<SseEvent>().Count(), Is.EqualTo(2), "the event read before the break is kept, in each attempt");
        var turn = Of<TurnEvent>().Last();
        Assert.That((turn.Failure, turn.Answer), Is.EqualTo(((TurnFailure?)TurnFailure.Transport, "Hallo")));
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_f", "sess_f" }), "each attempt's session is closed");
        Assert.That(Of<ConversationEvent>().Count(), Is.EqualTo(1), "the run goes on to the conversation's end");

        // The mode's roll-up follows its last mode-done.
        Assert.That(Written.Events(_lines).TakeLast(2).Select(e => e.GetType()),
            Is.EqualTo(new[] { typeof(ModeDoneEvent), typeof(SummaryEvent) }));
    }

    /// <summary>A turn that takes too long ends as a timeout.</summary>
    [TestCase(typeof(TimeoutException))]
    [TestCase(typeof(TaskCanceledException))]
    public async Task ATimeoutGetsTimeout(Type error)
    {
        await Run(new FailingClient { SendFails = (Exception)Activator.CreateInstance(error, "too slow")! });

        Assert.That(Of<TurnEvent>().Single().Failure, Is.EqualTo(TurnFailure.Timeout));
    }

    /// <summary>An error the API answers with ends the turn as an API error.</summary>
    [Test]
    public async Task AnApiRefusalGetsApiError()
    {
        await Run(new FailingClient { SendFails = new EvalApiException(500, "The model is down.") });

        Assert.That(Of<TurnEvent>().Single().Failure, Is.EqualTo(TurnFailure.ApiError));
    }

    /// <summary>
    /// A turn the API ends with a replay miss is the replay's failure, not the agent's; any other error the API streams
    /// stays the agent's turn, with no failure (FX26).
    /// </summary>
    [TestCase("replay_miss_error", TurnFailure.ReplayMiss)]
    [TestCase("api_error", null)]
    public async Task AStreamedReplayMissGetsReplayMiss(string kind, TurnFailure? expected)
    {
        var error = SessionEvent.From("error",
            JsonDocument.Parse($$$"""{"type":"error","error":{"type":"{{{kind}}}","message":"No recorded answer."}}""")
                .RootElement.Clone());

        await Run(new FailingClient { Events = [error] });

        Assert.That(Of<TurnEvent>().Single().Failure, Is.EqualTo(expected));
    }

    /// <summary>A turn that ends on its own has no failure.</summary>
    [Test]
    public async Task ATurnThatSucceedsHasNoFailure()
    {
        await Run(new FailingClient { Events = [Hallo] });

        Assert.That(Of<TurnEvent>().Single().Failure, Is.Null);
    }

    /// <summary>A session that can't open fails every turn the same way, and there is nothing to close.</summary>
    [Test]
    public async Task AFailedOpenFailsEveryTurnTheSameWay_AndClosesNothing()
    {
        var client = await Run(new FailingClient { OpenFails = new EvalApiException(400, "Unknown role.") },
            messages: 2);

        Assert.That(Of<TurnEvent>().Select(t => t.Failure),
            Is.EqualTo(new TurnFailure?[] { TurnFailure.ApiError, TurnFailure.ApiError }));
        Assert.That(Of<QuestionEvent>().Count(), Is.EqualTo(2));
        Assert.That(client.Closed, Is.Empty);
        Assert.That(Of<ConversationEvent>().Count(), Is.EqualTo(1));
    }

    /// <summary>A run that is cancelled still throws, and still closes its session.</summary>
    [Test]
    public void ACancelledRunStillThrows_AndStillClosesTheSession()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var client = new FailingClient { SendFails = new OperationCanceledException(source.Token) };

        Assert.That(async () => await Run(client, token: source.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_f" }));
    }

    /// <summary>
    /// The failure reads back from the run's own lines, and a retried turn reads as its last attempt.
    /// </summary>
    [Test]
    public async Task TheFailureRoundTripsThroughTheProtocolsTranscript()
    {
        await Run(new FailingClient
        {
            Events = [Hallo],
            SendFails = new HttpRequestException("reset")
        });

        var read = ProtocolTranscripts.ReadConversation(_lines);

        Assert.That(read.Turns.Single().Failure, Is.EqualTo(TurnFailure.Transport));
        Assert.That(read.Turns.Single().Events.Count, Is.EqualTo(1),
            "the retried turn reads as its last attempt, not both attempts' events together");
    }

    /// <summary>A client that sends its events and then fails, or fails to open, as a test sets it.</summary>
    private sealed class FailingClient : ISessionClient
    {
        /// <summary>The events every send gives before it fails, if it does.</summary>
        public IReadOnlyList<SessionEvent> Events { get; init; } = [];

        /// <summary>What a send throws after its events, if anything.</summary>
        public Exception? SendFails { get; init; }

        /// <summary>What opening a session throws, if anything.</summary>
        public Exception? OpenFails { get; init; }

        /// <summary>The sessions closed, in order.</summary>
        public List<string> Closed { get; } = [];

        /// <summary>No toolsets.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        /// <summary>Opens the one session, or fails as set.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            OpenFails is null ? Task.FromResult("sess_f") : Task.FromException<string>(OpenFails);

        /// <summary>Gives the events, then fails as set.</summary>
        public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var e in Events) yield return e;
            if (SendFails is not null) throw SendFails;
            await Task.CompletedTask;
        }

        /// <summary>Notes the close.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            return Task.CompletedTask;
        }
    }

    /// <summary>An eval of one scenario with as many messages as asked, and nothing to grade.</summary>
    private sealed class TwoTurnEval(int messages) : IEval
    {
        /// <inheritdoc />
        public string Name => "stub-failures";

        /// <inheritdoc />
        public int Version => 1;

        /// <inheritdoc />
        public string Proves => "A turn that fails for the infrastructure.";

        /// <summary>One mode.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([
                new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])
            ]);

        /// <inheritdoc />
        public IReadOnlyList<EvalScenario> Scenarios { get; } =
        [
            new EvalScenario("pannekoek",
                new EvalInput(null, "Africa/Johannesburg",
                    Enumerable.Range(1, messages).Select(n => $"Vraag {n}").ToList()), default, [], DatasetSplit.Dev)
        ];

        /// <inheritdoc />
        public IReadOnlyList<IGrader> Graders => [];

        /// <summary>No facts about a turn.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }
}
