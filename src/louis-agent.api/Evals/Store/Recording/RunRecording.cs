using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Store.Recording;

/// <summary>
/// What one run has told us so far, and how it's written as it comes: the run and its modes when they start, a
/// conversation when its first question comes (its grades and metrics when it ends), a turn with its stream events
/// when it ends. When the run ends it closes what's still open, joins the usage ledger, puts each conversation's
/// tokens and cost in its metrics, and writes the run's end.
/// </summary>
/// <remarks>
/// A fold over the run's events: each line changes this state and writes what it settled, one method per kind of
/// event, picked by a switch expression on the contract's record. It was split off the recorder when that passed 200
/// lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
internal sealed class RunRecording(
    RunningEval run,
    IReadOnlyDictionary<string, string>? settings,
    IEvalStore store,
    ILogger? logger,
    string? ledgerDirectory,
    CancellationToken cancellationToken)
{
    /// <summary>The run's modes, a repeat each, in the order they started.</summary>
    private readonly List<Mode> _modes = [];

    /// <summary>The summary slices the runner wrote after each mode.</summary>
    private readonly List<EvalSlice> _slices = [];

    /// <summary>The lines for people, kept for the run's log.</summary>
    private readonly List<string> _log = [];

    /// <summary>Where each conversation sits: the order its first question came in.</summary>
    private readonly Dictionary<ConversationKey, int> _order = [];

    /// <summary>The conversations that ended, as they were written, so their tokens can be added at the end.</summary>
    private readonly Dictionary<ConversationKey, Conversation> _ended = [];

    /// <summary>The turns asked and not yet ended.</summary>
    private readonly Dictionary<TurnKey, OpenTurn> _open = [];

    /// <summary>Which conversation each chat session belongs to, for joining the ledger's rows.</summary>
    private readonly Dictionary<string, ConversationKey> _sessions = [];

    /// <summary>The store's writes, stopped after the first failure.</summary>
    private readonly GuardedWrites _writes = new(run.Id, logger);

    /// <summary>What the run cost, once the ledger is joined.</summary>
    private Total? _total;

    /// <summary>What the run ran with, and the prompt hashes its requests carried once the ledger is joined.</summary>
    private IReadOnlyDictionary<string, string>? _settings = settings;

    /// <summary>Each agent's prompts, once the ledger is joined.</summary>
    private IReadOnlyList<AgentPrompt>? _prompts;

    /// <summary>The run's first line, once it came.</summary>
    private ProtocolEvent? _protocol;

    /// <summary>A turn asked and not ended yet: its session, its question, when it was asked, its events.</summary>
    private sealed class OpenTurn(string session, string question, DateTimeOffset? askedAt)
    {
        /// <summary>The chat session the turn runs in.</summary>
        public string Session { get; } = session;

        /// <summary>What the turn asked.</summary>
        public string Question { get; } = question;

        /// <summary>When the question was asked; unknown for a turn whose events came before its question.</summary>
        public DateTimeOffset? AskedAt { get; } = askedAt;

        /// <summary>The stream events heard so far.</summary>
        public List<StreamEvent> Events { get; } = [];
    }

    /// <summary>Writes the run as it stands now, so a half-done run can already be read.</summary>
    public Task SaveRunAsync() => _writes.WriteAsync(() => store.SaveRunAsync(Document(), cancellationToken));

    /// <summary>
    /// Takes one line of the run in: an event of the protocol, or a line for people, which goes to the run's log.
    /// </summary>
    /// <remarks>A switch expression on the event's type, one arm per event, read like a table.</remarks>
    public Task FoldAsync(string line) => EvalProtocolJson.Read(line) switch
    {
        null => LoggedAsync(line),
        ProtocolEvent { Version: not ProtocolEvent.CurrentVersion } started => LoggedAsync(MessageText.Format(
            ApiUserText.BenchmarkProtocolUnknown,
            new
            {
                started.Version,
                Supported = ProtocolEvent.CurrentVersion
            })),
        ProtocolEvent started => StartedAsync(started),
        ModeEvent mode => ModeStartedAsync(mode),
        ModeDoneEvent done => ModeEndedAsync(done),
        SummaryEvent summary => SummarisedAsync(summary),
        QuestionEvent question => AskedAsync(question),
        SseEvent sse => StreamedAsync(sse),
        TurnEvent turn => TurnEndedAsync(turn),
        ConversationEvent conversation => ConversationEndedAsync(conversation),
        _ => Task.CompletedTask,
    };

    /// <summary>
    /// Closes what the run left open, then writes its end; the run is kept with its store error when it can be.
    /// </summary>
    public async Task<EvalRecorder.Result> CloseAsync()
    {
        // A turn still open was cut off: it's kept with what it had and says so.
        string cutOff = MessageText.Format(ApiUserText.BenchmarkTurnCutOff);
        foreach (var (key, open) in _open)
            await _writes.WriteAsync(() => store.SaveTurnAsync(run.Id,
                new Turn(key, open.Session, open.Question, null, null, null, 0, 0, [], 0, cutOff,
                    EvalTools.Of(open.Events)), open.Events, cancellationToken));
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

        return new EvalRecorder.Result(stored, _writes.StoreError);
    }

    /// <summary>Keeps a line for people in the run's log.</summary>
    private Task LoggedAsync(string text)
    {
        _log.Add(text);
        return Task.CompletedTask;
    }

    /// <summary>Keeps what the run ran (its eval, graders and models) once its first line says so.</summary>
    private Task StartedAsync(ProtocolEvent started)
    {
        _protocol = started;
        return SaveRunAsync();
    }

    /// <summary>Keeps a mode's repeat as it starts, with what its sessions open with and how many it plans.</summary>
    private Task ModeStartedAsync(ModeEvent mode)
    {
        _modes.Add(new Mode(mode.Mode, mode.Repeat, mode.Toolsets, mode.Role, mode.Llm, null, mode.Conversations,
            mode.T));
        return SaveRunAsync();
    }

    /// <summary>
    /// Keeps when a mode's repeat ended, and the pass rates the runner wrote for its repeats so far; the API keeps
    /// them, it computes none.
    /// </summary>
    private async Task ModeEndedAsync(ModeDoneEvent ended)
    {
        int at = _modes.FindLastIndex(m => m.Name == ended.Mode && m.Repeat == ended.Repeat);
        if (at < 0) return;

        var rates = ended.Rates is { } r
            ? new ModeRates(r.PassAtK, r.PassHatK, r.K, r.Infrastructure, r.Retried)
            : null;
        _modes[at] = _modes[at] with
        {
            Rates = rates,
            DoneAt = ended.T
        };
        await SaveRunAsync();
    }

    /// <summary>Keeps one slice of a mode's roll-up; the run is written with it at the end.</summary>
    private Task SummarisedAsync(SummaryEvent summary)
    {
        _slices.Add(EvalSlice.Of(summary));
        return Task.CompletedTask;
    }

    /// <summary>Opens the turn, and writes its conversation the first time one of its questions comes.</summary>
    private async Task AskedAsync(QuestionEvent question)
    {
        var conversation = question.Key.Conversation;
        _open[question.Key] = new OpenTurn(question.Session, question.Text, question.T);
        _sessions.TryAdd(question.Session, conversation);
        if (_order.ContainsKey(conversation)) return;

        var started = new Conversation(conversation, OrderOf(conversation), null, null, 0, 0);
        await _writes.WriteAsync(() => store.SaveConversationAsync(run.Id, started, cancellationToken));
    }

    /// <summary>Adds one event of a turn's stream to its turn; one before its question opens the turn blank.</summary>
    private Task StreamedAsync(SseEvent sse)
    {
        if (!_open.TryGetValue(sse.Key, out var open)) _open[sse.Key] = open = new OpenTurn("", "", null);
        open.Events.Add(new StreamEvent(sse.Type, sse.Data, sse.T));
        return Task.CompletedTask;
    }

    /// <summary>Writes the ended turn with the stream events heard for it, and what its tools did.</summary>
    private async Task TurnEndedAsync(TurnEvent turn)
    {
        _sessions.TryAdd(turn.Session, turn.Key.Conversation);
        _open.Remove(turn.Key, out var open);
        var events = open?.Events ?? [];

        var stored = new Turn(turn.Key, turn.Session, turn.Question, turn.Answer, turn.Role, turn.Continued,
            turn.AgentCalls, turn.SpecialistCalls, turn.Hosts, turn.Seconds, turn.Error, EvalTools.Of(events),
            turn.Failure, open?.AskedAt, turn.T);
        await _writes.WriteAsync(() => store.SaveTurnAsync(run.Id, stored, events, cancellationToken));
    }

    /// <summary>Writes the ended conversation again, now with its metrics, grades and outcome.</summary>
    private async Task ConversationEndedAsync(ConversationEvent ended)
    {
        var grades = ended.Grades.Select(g => new Grade(g.Grader, g.Version, g.Score, g.Outcome, g.Reason,
            g.JudgeModel, g.JudgePromptHash)).ToArray();
        var conversation = new Conversation(ended.Key, OrderOf(ended.Key), ended.Role, ended.Metrics, ended.Errors,
            ended.Seconds, grades, ended.Outcome, ended.T);
        _ended[ended.Key] = conversation;
        await _writes.WriteAsync(() => store.SaveConversationAsync(run.Id, conversation, cancellationToken));
    }

    /// <summary>
    /// Joins the run's rows of the usage ledger to its turns, for its requests, total and prompts, and writes each
    /// ended conversation again with its tokens and cost.
    /// </summary>
    private async Task JoinLedgerAsync()
    {
        var joined = LedgerJoin.Of(run, RescoreOf, ledgerDirectory, _settings, _protocol?.Models, _sessions);
        (_total, _settings, _prompts) = (joined.Total, joined.Settings, joined.Prompts);
        _log.AddRange(joined.Log);
        if (joined.Requests.Count == 0) return;

        await _writes.WriteAsync(() => store.AddRequestsAsync(run.Id, joined.Requests, cancellationToken));
        // The ledger knows each conversation's tokens and cost only now, through its turns' requests.
        foreach (var (key, conversation) in _ended)
        {
            if (conversation.Metrics is not { } metrics) continue;
            var measured = conversation with
            {
                Metrics = ConversationUsage.With(metrics, joined.Requests.Where(r => r.Turn?.Conversation == key))
            };
            await _writes.WriteAsync(() => store.SaveConversationAsync(run.Id, measured, cancellationToken));
        }
    }

    /// <summary>Where a conversation sits in the run: the order its first question came in.</summary>
    private int OrderOf(ConversationKey key)
    {
        if (!_order.TryGetValue(key, out int order)) _order[key] = order = _order.Count + 1;
        return order;
    }

    /// <summary>The run as the store keeps it, from everything heard so far, as protocol version 2.</summary>
    private Run Document() =>
        new(run.Id, run.Check, run.Repeat, run.Status, run.StartedAt, run.EndedAt, run.ExitCode, _settings,
            _modes.ToArray(),
            _total, Log: run.Status == EvalRunStatus.Running ? null : _log.ToArray(),
            StoreError: _writes.StoreError,
            Eval: _protocol?.Eval, EvalVersion: _protocol?.EvalVersion, Commit: _protocol?.Commit,
            Graders: _protocol?.Graders.Select(g => new Grader(g.Name, g.Version, g.Kind, g.Question)).ToArray(),
            Models: _protocol?.Models.Select(m => new ModelUse(m.Mode, m.Level, m.Role, m.Model)).ToArray(),
            Split: _protocol?.Split,
            RescoreOf: RescoreOf, Slices: _slices.Count == 0 ? null : _slices.ToArray(), Prompts: _prompts,
            ProtocolVersion: ProtocolEvent.CurrentVersion, Dataset: _protocol?.Dataset, BegunAt: _protocol?.T);

    /// <summary>The run this one re-scored: what it was started with, else what its first line says.</summary>
    private string? RescoreOf => run.RescoreOf ?? _protocol?.RescoreOf;
}
