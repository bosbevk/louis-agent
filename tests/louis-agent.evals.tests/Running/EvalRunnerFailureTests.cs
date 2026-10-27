using LouisAgent.Testing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Running;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Evals.Tests.Running;

// F18 step 5.1: a turn that fails for the infrastructure is recorded with its TrialFailure, not thrown. The run goes on, and the session
// is still closed. A cancelled run still throws.
public class EvalRunnerFailureTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static readonly SessionEvent Hallo =
        SessionEvent.From("content_block_delta", JsonDocument.Parse("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""").RootElement.Clone());

    private static async Task<(List<JsonElement> Lines, FailingClient Client)> Run(FailingClient client, int messages = 1, CancellationToken token = default)
    {
        var sink = new StringWriter();
        var runner = new EvalRunner(client, new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At), NullLogger<EvalRunner>.Instance);
        try
        {
            await runner.RunAsync(new TwoTurnEval(messages), new EvalRun("run_1", 1), token);
        }
        finally
        {
            Lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => JsonDocument.Parse(l.TrimStart('@')).RootElement.Clone()).ToList();
        }
        return (Lines, client);
    }

    private static List<JsonElement> Lines = [];

    private static IEnumerable<JsonElement> Of(string kind) => Lines.Where(l => l.GetProperty("event").GetString() == kind);

    [Test]
    public async Task AStreamThatBreaksPartway_KeepsItsFirstEvents_AndGetsTransport()
    {
        var (lines, client) = await Run(new FailingClient { Events = [Hallo], SendFails = new HttpRequestException("the connection was reset") });

        // A transport failure is run once more (step 5a): this client breaks every time, so the trial has two attempts, each recorded and each closed.
        Assert.That(Of("sse").Count(), Is.EqualTo(2), "the event read before the break is kept, in each attempt");
        var turn = Of("turn").Last();
        Assert.That(turn.GetProperty("failure").GetString(), Is.EqualTo("transport"));
        Assert.That(turn.GetProperty("answer").GetString(), Is.EqualTo("Hallo"));
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_f", "sess_f" }), "each attempt's session is closed");
        Assert.That(Of("conversation").Count(), Is.EqualTo(1), "the run goes on to the conversation's end");
        // The mode's roll-up follows its last mode_done (F18 step 5c).
        Assert.That(lines.Select(l => l.GetProperty("event").GetString()).TakeLast(2), Is.EqualTo(new[] { "mode_done", "summary" }));
    }

    [TestCase(typeof(TimeoutException), "timeout")]
    [TestCase(typeof(TaskCanceledException), "timeout")]
    public async Task ATimeoutGetsTimeout(Type error, string expected)
    {
        await Run(new FailingClient { SendFails = (Exception)Activator.CreateInstance(error, "too slow")! });

        Assert.That(Of("turn").Single().GetProperty("failure").GetString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task AnApiRefusalGetsApiError()
    {
        await Run(new FailingClient { SendFails = new EvalApiException(500, "The model is down.") });

        Assert.That(Of("turn").Single().GetProperty("failure").GetString(), Is.EqualTo("api_error"));
    }

    [Test]
    public async Task ATurnThatSucceedsHasNoFailureField()
    {
        await Run(new FailingClient { Events = [Hallo] });

        Assert.That(Of("turn").Single().TryGetProperty("failure", out _), Is.False);
    }

    [Test]
    public async Task AFailedOpenFailsEveryTurnTheSameWay_AndClosesNothing()
    {
        var (_, client) = await Run(new FailingClient { OpenFails = new EvalApiException(400, "Unknown role.") }, messages: 2);

        Assert.That(Of("turn").Select(t => t.GetProperty("failure").GetString()), Is.EqualTo(new[] { "api_error", "api_error" }));
        Assert.That(Of("question").Count(), Is.EqualTo(2));
        Assert.That(client.Closed, Is.Empty);
        Assert.That(Of("conversation").Count(), Is.EqualTo(1));
    }

    [Test]
    public void ACancelledRunStillThrows_AndStillClosesTheSession()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var client = new FailingClient { SendFails = new OperationCanceledException(source.Token) };

        Assert.That(async () => await Run(client, token: source.Token), Throws.InstanceOf<OperationCanceledException>());
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_f" }));
    }

    [Test]
    public async Task TheFailureRoundTripsThroughTheProtocolsTranscript()
    {
        await Run(new FailingClient { Events = [Hallo], SendFails = new HttpRequestException("reset") });

        var read = ProtocolTranscripts.ReadConversation(Lines.Select(l => "@" + l.GetRawText()));

        Assert.That(read.Turns.Single().Failure, Is.EqualTo(TrialFailure.Transport));
        Assert.That(read.Turns.Single().Events.Count, Is.EqualTo(1), "the retried turn reads as its last attempt, not both attempts' events together");
    }

    private sealed class FailingClient : ISessionClient
    {
        public IReadOnlyList<SessionEvent> Events { get; init; } = [];
        public Exception? SendFails { get; init; }
        public Exception? OpenFails { get; init; }
        public List<string> Closed { get; } = [];

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            OpenFails is null ? Task.FromResult("sess_f") : Task.FromException<string>(OpenFails);

        public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var e in Events) yield return e;
            if (SendFails is not null) throw SendFails;
            await Task.CompletedTask;
        }

        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            return Task.CompletedTask;
        }
    }

    private sealed class TwoTurnEval(int messages) : IEval
    {
        public string Name => "stub-failures";
        public int Version => 1;
        public string Proves => "A turn that fails for the infrastructure.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } = [new EvalScenario("pannekoek",
            new EvalInput(null, "Africa/Johannesburg", Enumerable.Range(1, messages).Select(n => $"Vraag {n}").ToList()), default, [], DatasetSplit.Dev)];

        public IReadOnlyList<IGrader> Graders => [];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new NoScores();

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed record NoScores : EvalScores;
}
