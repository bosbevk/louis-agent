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
using Microsoft.Extensions.Logging;

namespace LouisAgent.Evals.Tests.Running;

// F18 step 5a: the runner writes a mode's pass@k and pass^k on its mode_done line (the one copy of the formula is the harness's, the API only reads
// them), counts the trials left out for the infrastructure, and runs a trial once more when its turn failed with a transport error and with nothing
// else (a timeout, an API error or a replay miss would not change on a second try). Written first: mode_done carried only mode and repeat.
public class EvalRunnerPassRatesTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static SessionEvent Said(string text) =>
        SessionEvent.From("content_block_delta", JsonDocument.Parse(JsonSerializer.Serialize(new { type = "content_block_delta", index = 0, delta = new { type = "text_delta", text } })).RootElement.Clone());

    /// <summary>What one send does: answers, or throws.</summary>
    private abstract record Send
    {
        public sealed record Answers(string Text) : Send;

        public sealed record Throws(Exception Error) : Send;
    }

    private static async Task<(List<JsonElement> Lines, ScriptedClient Client, CapturingLogger Log)> Run(int repeats, params Send[] sends)
    {
        var sink = new StringWriter();
        var client = new ScriptedClient(sends);
        var log = new CapturingLogger();
        var runner = new EvalRunner(client, new ProtocolWriter(sink, new FixedClock(At)), new FixedClock(At), new TypedLogger(log));
        await runner.RunAsync(new OneScenarioEval(), new EvalRun("run_1", repeats), CancellationToken.None);
        var lines = sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => JsonDocument.Parse(l.TrimStart('@')).RootElement.Clone()).ToList();
        return (lines, client, log);
    }

    private static IEnumerable<JsonElement> Of(List<JsonElement> lines, string kind) => lines.Where(l => l.GetProperty("event").GetString() == kind);

    private static Send Right => new Send.Answers("Hallo");

    private static Send Wrong => new Send.Answers("Nee");

    // --- the rates on mode_done

    [Test]
    public async Task ModeDone_CarriesThePassRates_OverTheRepeatsSoFar()
    {
        var (lines, _, _) = await Run(2, Right, Wrong);

        var done = Of(lines, "mode_done").ToList();
        Assert.That(done[0].GetProperty("pass_at_k").GetDouble(), Is.EqualTo(1));
        Assert.That(done[0].GetProperty("pass_hat_k").GetDouble(), Is.EqualTo(1));
        Assert.That(done[0].GetProperty("k").GetInt32(), Is.EqualTo(1));
        Assert.That(done[1].GetProperty("pass_at_k").GetDouble(), Is.EqualTo(1), "it passed once in two trials");
        Assert.That(done[1].GetProperty("pass_hat_k").GetDouble(), Is.EqualTo(0), "but not every time");
        Assert.That(done[1].GetProperty("k").GetInt32(), Is.EqualTo(2));
        Assert.That(done[1].GetProperty("infrastructure").GetInt32(), Is.EqualTo(0));
        Assert.That(done[1].GetProperty("retried").GetInt32(), Is.EqualTo(0));
    }

    [Test]
    public async Task ATrialLeftOutForTheInfrastructure_IsCounted_AndTheRatesAreLeftOutNotZero()
    {
        var (lines, _, _) = await Run(1, new Send.Throws(new TimeoutException("too slow")));

        var done = Of(lines, "mode_done").Single();
        Assert.That(Of(lines, "conversation").Single().GetProperty("outcome").GetString(), Is.EqualTo("Infrastructure"));
        Assert.That(done.TryGetProperty("pass_at_k", out _), Is.False, "no countable trial: no rate, never 0");
        Assert.That(done.TryGetProperty("pass_hat_k", out _), Is.False);
        Assert.That(done.GetProperty("infrastructure").GetInt32(), Is.EqualTo(1));
        Assert.That(done.GetProperty("k").GetInt32(), Is.EqualTo(1));
    }

    // --- the retry

    [Test]
    public async Task ATransportFailure_IsRunOnceMore_FromANewSession_AndTheSecondAttemptIsTheTrial()
    {
        var (lines, client, log) = await Run(1, new Send.Throws(new HttpRequestException("reset")), Right);

        Assert.That(client.Opened, Is.EqualTo(new[] { "sess_1", "sess_2" }), "a new session for the second attempt");
        Assert.That(client.Closed, Is.EqualTo(new[] { "sess_1", "sess_2" }), "both are closed");
        Assert.That(Of(lines, "conversation").Count(), Is.EqualTo(1), "one trial, one conversation");
        Assert.That(Of(lines, "conversation").Single().GetProperty("outcome").GetString(), Is.EqualTo("Pass"));
        Assert.That(Of(lines, "mode_done").Single().GetProperty("retried").GetInt32(), Is.EqualTo(1));
        Assert.That(Of(lines, "mode_done").Single().GetProperty("infrastructure").GetInt32(), Is.EqualTo(0));
        Assert.That(Of(lines, "turn").Select(t => t.TryGetProperty("failure", out var f) ? f.GetString() : null), Is.EqualTo(new string?[] { "transport", null }),
            "the failed attempt stays in the protocol; the same turn key written again replaces it in the store");
        Assert.That(log.Warnings.Any(w => w.Contains("Retrying") && w.Contains("pannekoek")), Is.True, "the retry is logged");
    }

    [Test]
    public async Task ATransportFailureTwice_IsRetriedOnlyOnce_AndIsAnInfrastructureTrial()
    {
        var (lines, client, _) = await Run(1, new Send.Throws(new HttpRequestException("reset")), new Send.Throws(new IOException("broken pipe")));

        Assert.That(client.Opened.Count, Is.EqualTo(2), "one retry, not a loop");
        Assert.That(Of(lines, "conversation").Single().GetProperty("outcome").GetString(), Is.EqualTo("Infrastructure"));
        var done = Of(lines, "mode_done").Single();
        Assert.That(done.GetProperty("retried").GetInt32(), Is.EqualTo(1));
        Assert.That(done.GetProperty("infrastructure").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task OnlyTransportIsRetried_ATimeoutAndAnApiErrorWouldNotChange()
    {
        var (_, timedOut, _) = await Run(1, new Send.Throws(new TimeoutException("too slow")), Right);
        var (_, refused, _) = await Run(1, new Send.Throws(new EvalApiException(500, "The model is down.")), Right);

        Assert.That(timedOut.Opened.Count, Is.EqualTo(1));
        Assert.That(refused.Opened.Count, Is.EqualTo(1));
    }

    // --- fakes

    private sealed class ScriptedClient(IEnumerable<Send> sends) : ISessionClient
    {
        private readonly Queue<Send> _sends = new(sends);
        public List<string> Opened { get; } = [];
        public List<string> Closed { get; } = [];

        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
        {
            Opened.Add($"sess_{Opened.Count + 1}");
            return Task.FromResult(Opened[^1]);
        }

        public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var send = _sends.Count > 0 ? _sends.Dequeue() : new Send.Answers("Hallo");
            if (send is Send.Throws throws) throw throws.Error;
            yield return Said(((Send.Answers)send).Text);
        }

        public Task CloseAsync(string session, CancellationToken cancellationToken)
        {
            Closed.Add(session);
            return Task.CompletedTask;
        }
    }

    /// <summary>The trial passes when the answer is "Hallo".</summary>
    private sealed class AnswerGrader : IGrader
    {
        public string Name => "says-hallo";
        public int Version => 1;
        public GraderKind Kind => GraderKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(EngineEvents.AnswerOf(trial.Transcript.Turns[^1]) == "Hallo"
                ? new Grade(Name, 1, 1, GradeOutcome.Pass, "said hallo")
                : new Grade(Name, 1, 0, GradeOutcome.Fail, "did not"));
    }

    private sealed class OneScenarioEval : IEval
    {
        public string Name => "stub-rates";
        public string? Identity => null;
        public int Version => 1;
        public string Proves => "pass@k and pass^k.";

        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("cook", SampleRole.Alpha, [SampleToolset.Alpha])]);

        public IReadOnlyList<EvalScenario> Scenarios { get; } =
            [new EvalScenario("pannekoek", new EvalInput(null, "Africa/Johannesburg", ["Vraag"]), default, [], DatasetSplit.Dev)];

        public IReadOnlyList<IGrader> Graders { get; } = [new AnswerGrader()];

        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => new NoScores();

        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => new(null, null, 0, 0, [], null);
    }

    private sealed record NoScores : EvalScores;

    private sealed class TypedLogger(ILogger inner) : ILogger<EvalRunner>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
