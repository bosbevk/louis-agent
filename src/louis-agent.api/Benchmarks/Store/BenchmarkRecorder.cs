using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Core.Usage.Records;
using System.Text.Json;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// Keeps a benchmark run in the store as it happens (F31): it listens to the run's lines as the page does
/// (<see cref="BenchmarkRun.Listen"/>: the lines so far, then each new one), parses each with
/// <see cref="BenchmarkProtocol"/> and writes as it goes: the run and its modes when they start, a conversation when its
/// first question comes (its scores when it ends), a turn with its stream events when it ends. So a half-done run is
/// already readable. When the run ends it closes whatever is still open, a turn with what it had, joins the run's rows
/// of the usage ledger in <paramref name="ledgerDirectory"/> to its turns (<see cref="BenchmarkLedger"/>), and writes
/// the run's end, its total and its plain lines. A store that fails stops the writes, never the run: the result and
/// the run say so.
/// </summary>
public sealed class BenchmarkRecorder(IBenchmarkStore store, ILogger? logger = null, string? ledgerDirectory = null)
{
    /// <summary>Whether everything was kept; <see cref="StoreError"/> says why not.</summary>
    public sealed record Result(bool Stored, string? StoreError);

    /// <summary>How many runs' recordings are kept for <see cref="RecordingOf"/>: the last few are all a page asks about.</summary>
    private const int KeptRecordings = 8;

    private readonly Lock _lock = new();
    private readonly List<(string Run, Task<Result> Recording)> _recordings = [];

    /// <summary>Starts recording <paramref name="run"/> in the background (the endpoint that started it doesn't wait).</summary>
    public void Attach(BenchmarkRun run, IReadOnlyDictionary<string, string>? settings = null)
    {
        var recording = RecordAsync(run, settings);
        lock (_lock)
        {
            _recordings.Add((run.Id, recording));
            if (_recordings.Count > KeptRecordings) _recordings.RemoveAt(0);
        }
    }

    /// <summary>The recording of a run <see cref="Attach"/> started; null for one it didn't (or no longer keeps).</summary>
    public Task<Result>? RecordingOf(string id)
    {
        lock (_lock) return _recordings.LastOrDefault(r => r.Run == id).Recording;
    }

    private static readonly JsonElement NoScores = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>Records <paramref name="run"/> until it ends; <paramref name="settings"/> are what it ran with.</summary>
    public async Task<Result> RecordAsync(BenchmarkRun run, IReadOnlyDictionary<string, string>? settings = null,
        CancellationToken cancellationToken = default)
    {
        // Listen before the first await, so no line printed meanwhile is missed.
        var (soFar, live) = run.Listen();
        var recording = new Recording(run, settings, store, logger, ledgerDirectory, cancellationToken);
        await recording.SaveRunAsync();
        foreach (string line in soFar) await recording.FoldAsync(Parse(line));
        await foreach (string line in live.ReadAllAsync(cancellationToken)) await recording.FoldAsync(Parse(line));
        return await recording.CloseAsync();
    }

    /// <summary>One run's state while it is recorded: what is open, and whether the store still takes writes.</summary>
    private sealed class Recording(BenchmarkRun run, IReadOnlyDictionary<string, string>? settings, IBenchmarkStore store,
        ILogger? logger, string? ledgerDirectory, CancellationToken cancellationToken)
    {
        private readonly List<Mode> _modes = [];
        private readonly List<string> _log = [];
        private readonly Dictionary<ConversationKey, int> _order = [];
        private readonly Dictionary<TurnKey, OpenTurn> _open = [];
        private readonly Dictionary<string, ConversationKey> _sessions = [];
        private Total? _total;
        private IReadOnlyDictionary<string, string>? _settings = settings;
        private ProtocolStarted? _protocol;
        private string? _storeError;

        private sealed class OpenTurn(string session, string question)
        {
            public string Session { get; set; } = session;
            public string Question { get; set; } = question;
            public List<StreamEvent> Events { get; } = [];
        }

        public Task SaveRunAsync() => WriteAsync(() => store.SaveRunAsync(Document(), cancellationToken));

        public async Task FoldAsync(Event e)
        {
            switch (e)
            {
                case LogLine line:
                    _log.Add(line.Text);
                    break;
                case ProtocolStarted started when started.Version != BenchmarkProtocol.Version:
                    _log.Add(MessageText.Format(ApiUserText.BenchmarkProtocolUnknown, new { started.Version, Supported = BenchmarkProtocol.Version }));
                    break;
                case ProtocolStarted started:
                    // F18 step 2a: what the run ran (its eval, graders and models), kept on the run once the line says so.
                    if (started.Eval is not null)
                    {
                        _protocol = started;
                        await SaveRunAsync();
                    }
                    break;
                case ModeStarted mode:
                    _modes.Add(new Mode(mode.Mode, mode.Repeat, mode.Toolsets, mode.Role, mode.Llm));
                    await SaveRunAsync();
                    break;
                case QuestionAsked question:
                    _open[question.Key] = new OpenTurn(question.Session, question.Text);
                    _sessions.TryAdd(question.Session, question.Key.Conversation);
                    if (!_order.ContainsKey(question.Key.Conversation))
                        await WriteAsync(() => store.SaveConversationAsync(run.Id,
                            new Conversation(question.Key.Conversation, OrderOf(question.Key.Conversation), null, NoScores, 0, 0), cancellationToken));
                    break;
                case SseEvent sse:
                    OpenOf(sse.Key).Events.Add(new StreamEvent(sse.Type, sse.Data, sse.At));
                    break;
                case TurnEnded turn:
                    _sessions.TryAdd(turn.Session, turn.Key.Conversation);
                    var events = _open.Remove(turn.Key, out var open) ? open.Events : [];
                    await WriteAsync(() => store.SaveTurnAsync(run.Id, new Turn(turn.Key, turn.Session, turn.Question, turn.Answer, turn.Role,
                        turn.Continued, turn.PulsCalls, turn.SpecialistCalls, turn.Hosts, turn.Seconds, turn.Error, BenchmarkTools.Of(events)),
                        events, cancellationToken));
                    break;
                case ConversationEnded conversation:
                    await WriteAsync(() => store.SaveConversationAsync(run.Id, new Conversation(conversation.Key, OrderOf(conversation.Key),
                        conversation.Role, conversation.Scores, conversation.Errors, conversation.Seconds,
                        conversation.Grades?.Select(g => new Grade(g.Grader, g.Version, g.Score, g.Outcome, g.Reason)).ToArray(),
                        conversation.Outcome), cancellationToken));
                    break;
            }
        }

        /// <summary>Closes what the run left open, then writes its end; the run is kept with its store error when it can be.</summary>
        public async Task<Result> CloseAsync()
        {
            string cutOff = MessageText.Format(ApiUserText.BenchmarkTurnCutOff);
            foreach (var (key, open) in _open)
                await WriteAsync(() => store.SaveTurnAsync(run.Id,
                    new Turn(key, open.Session, open.Question, null, null, null, 0, 0, [], 0, cutOff, BenchmarkTools.Of(open.Events)), open.Events, cancellationToken));
            _open.Clear();
            await JoinLedgerAsync();

            bool stored = _storeError is null;
            try
            {
                // Even after a failure: the run should say what wasn't kept, if the store takes that much.
                await store.SaveRunAsync(Document(), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Failed(ex);
                stored = false;
            }
            return new Result(stored, _storeError);
        }

        /// <summary>
        /// The run's requests and total from the ledger, read once the script has ended. A request's row is written when
        /// it finishes, so the last turn's rows should be there by then; the first real run (step 11b) checks that the
        /// run's request count matches the turns' (the old scripts waited two seconds to be sure).
        /// </summary>
        private async Task JoinLedgerAsync()
        {
            if (ledgerDirectory is null) return;
            IReadOnlyList<UsageRecord> ofRun;
            try
            {
                ofRun = BenchmarkLedger.OfRun(run.Id, BenchmarkLedger.Read(ledgerDirectory, run.StartedAt, run.EndedAt ?? DateTimeOffset.UtcNow));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                _log.Add(MessageText.Format(ApiUserText.BenchmarkLedgerUnreadable, new { Reason = ex.Message }));
                return;
            }
            _total = BenchmarkLedger.TotalOf(ofRun);
            _settings = BenchmarkSettings.WithPromptHashes(_settings, BenchmarkLedger.PromptHashesOf(ofRun));
            var requests = BenchmarkLedger.Join(run.Id, ofRun, _sessions);
            // F18 step 2a: a row on a model the run didn't declare is named in the run's log, so a trend can't mix it in silently.
            var unlisted = RunModels.Unlisted(_protocol?.Models, requests.Select(r => r.Model));
            if (unlisted.Count > 0) _log.Add(MessageText.Format(ApiUserText.BenchmarkModelUnlisted, new { Model = string.Join(", ", unlisted) }));
            if (requests.Count > 0) await WriteAsync(() => store.AddRequestsAsync(run.Id, requests, cancellationToken));
        }

        private int OrderOf(ConversationKey key)
        {
            if (!_order.TryGetValue(key, out int order)) _order[key] = order = _order.Count + 1;
            return order;
        }

        /// <summary>The turn an event belongs to; one the script never asked (an event before its question) opens with what's known.</summary>
        private OpenTurn OpenOf(TurnKey key)
        {
            if (!_open.TryGetValue(key, out var open)) _open[key] = open = new OpenTurn("", "");
            return open;
        }

        private Run Document() =>
            new(run.Id, run.Check, run.Repeat, run.Status, run.StartedAt, run.EndedAt, run.ExitCode, _settings, _modes.ToArray(),
                _total, Log: run.Status == BenchmarkRunStatus.Running ? null : _log.ToArray(), StoreError: _storeError,
                Eval: _protocol?.Eval, EvalVersion: _protocol?.EvalVersion, Commit: _protocol?.Commit,
                Graders: _protocol?.Graders?.Select(g => new Grader(g.Name, g.Version, g.Kind)).ToArray(),
                Models: _protocol?.Models?.Select(m => new ModelUse(m.Mode, m.Level, m.Role, m.Model)).ToArray());

        /// <summary>One write; after the first failure none are tried, so a store that is down doesn't slow the run.</summary>
        private async Task WriteAsync(Func<Task> write)
        {
            if (_storeError is not null) return;
            try
            {
                await write();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Failed(ex);
            }
        }

        private void Failed(Exception ex)
        {
            if (_storeError is not null) return;
            _storeError = MessageText.Format(ApiUserText.BenchmarkStoreFailed, new { Reason = ex.Message });
            logger?.Log(LogLevel.Warning, ApiLogMessage.BenchmarkStoreFailed, new { Run = run.Id }, exception: ex, tag: "API");
        }
    }
}
