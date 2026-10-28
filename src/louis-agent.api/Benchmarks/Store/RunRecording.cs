using System.Text.Json;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// What one run has told us so far, and how it's written as it comes: the run and its modes when they start, a
/// conversation when its first question comes (its scores when it ends), a turn with its stream events when it ends.
/// When the run ends it closes what's still open, joins the usage ledger and writes the run's end.
/// </summary>
/// <remarks>
/// A fold over the run's events: each line changes this state and writes what it settled, one method per kind of
/// line, picked by a switch on the event's type. It was split off the recorder when that passed 200 lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal sealed class RunRecording(
    BenchmarkRun run,
    IReadOnlyDictionary<string, string>? settings,
    IBenchmarkStore store,
    ILogger? logger,
    string? ledgerDirectory,
    CancellationToken cancellationToken)
{
    /// <summary>A conversation's scores before it has any.</summary>
    private static readonly JsonElement NoScores = JsonDocument.Parse("{}").RootElement.Clone();

    private readonly List<Mode> _modes = [];
    private readonly List<BenchmarkSlice> _slices = [];
    private readonly List<string> _log = [];
    private readonly Dictionary<ConversationKey, int> _order = [];
    private readonly Dictionary<TurnKey, OpenTurn> _open = [];
    private readonly Dictionary<string, ConversationKey> _sessions = [];
    private readonly GuardedWrites _writes = new(run.Id, logger);
    private Total? _total;
    private IReadOnlyDictionary<string, string>? _settings = settings;
    private IReadOnlyList<AgentPrompt>? _prompts;
    private ProtocolStarted? _protocol;

    /// <summary>A turn that was asked and hasn't ended yet: its session, question and events so far.</summary>
    private sealed class OpenTurn(string session, string question)
    {
        /// <summary>The chat session the turn runs in.</summary>
        public string Session { get; } = session;

        /// <summary>What the turn asked.</summary>
        public string Question { get; } = question;

        /// <summary>The stream events heard so far.</summary>
        public List<StreamEvent> Events { get; } = [];
    }

    /// <summary>Writes the run as it stands now, so a half-done run can already be read.</summary>
    public Task SaveRunAsync() => _writes.WriteAsync(() => store.SaveRunAsync(Document(), cancellationToken));

    /// <summary>Takes one line of the run in: a mode, a question, a stream event, a turn, a verdict.</summary>
    public async Task FoldAsync(Event e)
    {
        switch (e)
        {
            case LogLine line:
                _log.Add(line.Text);
                break;
            case ProtocolStarted { Version: not BenchmarkProtocol.Version } started:
                _log.Add(MessageText.Format(ApiUserText.BenchmarkProtocolUnknown,
                    new
                    {
                        started.Version,
                        Supported = BenchmarkProtocol.Version
                    }));
                break;
            case ProtocolStarted started:
                await StartedAsync(started);
                break;
            case ModeStarted mode:
                _modes.Add(new Mode(mode.Mode, mode.Repeat, mode.Toolsets, mode.Role, mode.Llm));
                await SaveRunAsync();
                break;
            case ModeEnded ended:
                await ModeEndedAsync(ended);
                break;
            case SummaryReported summary:
                _slices.Add(summary.Slice);
                break;
            case QuestionAsked question:
                await AskedAsync(question);
                break;
            case SseEvent sse:
                OpenOf(sse.Key).Events.Add(new StreamEvent(sse.Type, sse.Data, sse.At));
                break;
            case TurnEnded turn:
                await TurnEndedAsync(turn);
                break;
            case ConversationEnded conversation:
                await ConversationEndedAsync(conversation);
                break;
        }
    }

    /// <summary>
    /// Closes what the run left open, then writes its end; the run is kept with its store error when it can be.
    /// </summary>
    public async Task<BenchmarkRecorder.Result> CloseAsync()
    {
        // A turn still open was cut off: it's kept with what it had and says so.
        string cutOff = MessageText.Format(ApiUserText.BenchmarkTurnCutOff);
        foreach (var (key, open) in _open)
            await _writes.WriteAsync(() => store.SaveTurnAsync(run.Id,
                new Turn(key, open.Session, open.Question, null, null, null, 0, 0, [], 0, cutOff,
                    BenchmarkTools.Of(open.Events)), open.Events, cancellationToken));
        _open.Clear();
        await JoinLedgerAsync();

        bool stored = _writes.StoreError is null;
        try
        {
            // Even after a failure: the run should say what wasn't kept, if the store takes that much.
            await store.SaveRunAsync(Document(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _writes.Failed(ex);
            stored = false;
        }

        return new BenchmarkRecorder.Result(stored, _writes.StoreError);
    }

    /// <summary>Keeps what the run ran (its eval, graders and models) once its first line says so.</summary>
    private async Task StartedAsync(ProtocolStarted started)
    {
        if (started.Eval is null) return;
        _protocol = started;
        await SaveRunAsync();
    }

    /// <summary>
    /// Keeps the pass rates the runner wrote for a mode's repeats so far; the API keeps them, it computes none.
    /// </summary>
    private async Task ModeEndedAsync(ModeEnded ended)
    {
        if (ended.Rates is not { } rates) return;
        int at = _modes.FindLastIndex(m => m.Name == ended.Mode && m.Repeat == ended.Repeat);
        if (at < 0) return;

        _modes[at] = _modes[at] with
        {
            Rates = new ModeRates(rates.PassAtK, rates.PassHatK, rates.K, rates.Infrastructure, rates.Retried)
        };
        await SaveRunAsync();
    }

    /// <summary>Opens the turn, and writes its conversation the first time one of its questions comes.</summary>
    private async Task AskedAsync(QuestionAsked question)
    {
        _open[question.Key] = new OpenTurn(question.Session, question.Text);
        _sessions.TryAdd(question.Session, question.Key.Conversation);
        if (_order.ContainsKey(question.Key.Conversation)) return;

        var conversation = new Conversation(question.Key.Conversation, OrderOf(question.Key.Conversation), null,
            NoScores, 0, 0);
        await _writes.WriteAsync(() => store.SaveConversationAsync(run.Id, conversation, cancellationToken));
    }

    /// <summary>Writes the ended turn with the stream events heard for it, and what its tools did.</summary>
    private async Task TurnEndedAsync(TurnEnded turn)
    {
        _sessions.TryAdd(turn.Session, turn.Key.Conversation);
        var events = _open.Remove(turn.Key, out var open) ? open.Events : [];

        var stored = new Turn(turn.Key, turn.Session, turn.Question, turn.Answer, turn.Role, turn.Continued,
            turn.PulsCalls, turn.SpecialistCalls, turn.Hosts, turn.Seconds, turn.Error, BenchmarkTools.Of(events),
            turn.Failure);
        await _writes.WriteAsync(() => store.SaveTurnAsync(run.Id, stored, events, cancellationToken));
    }

    /// <summary>Writes the ended conversation again, now with its scores, grades and outcome.</summary>
    private async Task ConversationEndedAsync(ConversationEnded ended)
    {
        var grades = ended.Grades?.Select(g => new Grade(g.Grader, g.Version, g.Score, g.Outcome, g.Reason,
            g.JudgeModel, g.JudgePromptHash)).ToArray();
        var conversation = new Conversation(ended.Key, OrderOf(ended.Key), ended.Role, ended.Scores, ended.Errors,
            ended.Seconds, grades, ended.Outcome);
        await _writes.WriteAsync(() => store.SaveConversationAsync(run.Id, conversation, cancellationToken));
    }

    /// <summary>Joins the run's rows of the usage ledger to its turns, for its requests, total and prompts.</summary>
    private async Task JoinLedgerAsync()
    {
        var joined = LedgerJoin.Of(run, RescoreOf, ledgerDirectory, _settings, _protocol?.Models, _sessions);
        (_total, _settings, _prompts) = (joined.Total, joined.Settings, joined.Prompts);
        _log.AddRange(joined.Log);
        if (joined.Requests.Count > 0)
            await _writes.WriteAsync(() => store.AddRequestsAsync(run.Id, joined.Requests, cancellationToken));
    }

    /// <summary>Where a conversation sits in the run: the order its first question came in.</summary>
    private int OrderOf(ConversationKey key)
    {
        if (!_order.TryGetValue(key, out int order)) _order[key] = order = _order.Count + 1;
        return order;
    }

    /// <summary>The turn an event belongs to; an event that comes before its question opens the turn blank.</summary>
    private OpenTurn OpenOf(TurnKey key)
    {
        if (!_open.TryGetValue(key, out var open)) _open[key] = open = new OpenTurn("", "");
        return open;
    }

    /// <summary>The run as the store keeps it, from everything heard so far.</summary>
    private Run Document() =>
        new(run.Id, run.Check, run.Repeat, run.Status, run.StartedAt, run.EndedAt, run.ExitCode, _settings,
            _modes.ToArray(),
            _total, Log: run.Status == BenchmarkRunStatus.Running ? null : _log.ToArray(),
            StoreError: _writes.StoreError,
            Eval: _protocol?.Eval, EvalVersion: _protocol?.EvalVersion, Commit: _protocol?.Commit,
            Graders: _protocol?.Graders?.Select(g => new Grader(g.Name, g.Version, g.Kind, g.Question)).ToArray(),
            Models: _protocol?.Models?.Select(m => new ModelUse(m.Mode, m.Level, m.Role, m.Model)).ToArray(),
            Split: _protocol?.Split,
            RescoreOf: RescoreOf, Slices: _slices.Count == 0 ? null : _slices.ToArray(), Prompts: _prompts);

    /// <summary>The run this one re-scored: what it was started with, else what its first line says.</summary>
    private string? RescoreOf => run.RescoreOf ?? _protocol?.RescoreOf;
}
