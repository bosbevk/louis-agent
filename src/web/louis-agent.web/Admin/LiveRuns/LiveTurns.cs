using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.SessionStream;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Admin.LiveRuns;

/// <summary>
/// The table of the run in progress: a row per turn as the runner's events come in (the protocol's own records), the
/// turns the store already holds when the page picks a run up again, each turn's cost from its requests (as each turn
/// ends, and again when the run has), and the turn that is open (the newest question, until you open another).
/// </summary>
/// <remarks>
/// The page's live state as a plain class, so what an event does to the table is tested without a browser; the panel
/// that shows it only reads the events and the store and hands them in.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed class LiveTurns
{
    /// <summary>The turns, in the order they first came.</summary>
    private readonly List<LiveTurn> _rows = [];

    /// <summary>Each turn's known cost, from the requests the page was last given.</summary>
    private readonly Dictionary<TurnKey, decimal?> _costs = [];

    /// <summary>Whether the newest question opens as it comes.</summary>
    private bool _followOpen = true;

    /// <summary>The turns, in the order they first came.</summary>
    public IReadOnlyList<LiveTurn> Rows => _rows;

    /// <summary>The turn shown under the table.</summary>
    public LiveTurn? Open { get; private set; }

    /// <summary>
    /// Gets ready to follow a run: an empty table with no costs and no open turn, where the newest question opens.
    /// </summary>
    public void Start()
    {
        _rows.Clear();
        _costs.Clear();
        Open = null;
        _followOpen = true;
    }

    /// <summary>The turns the store already holds of the run, as finished rows.</summary>
    public void FromStore(StoredRunReply run)
    {
        _rows.Clear();
        foreach (var stored in run.Turns)
        {
            var row = RowOf(new TurnKey(stored.Mode, stored.Repeat, stored.Task, stored.Turn));
            row.Turn = new Turn { UserText = stored.Question ?? "" };
            row.Finish(stored);
        }
    }

    /// <summary>
    /// One of the runner's events into the table: a question opens its row, a piece of stream feeds the turn the
    /// chat's way, and a turn's end fills in its figures. Any other event (a mode, a conversation's grades, a
    /// summary) changes nothing here: the run's page reads those from the store.
    /// </summary>
    /// <remarks>A switch on the event's type, one arm per kind the table shows.</remarks>
    public void Apply(EvalEvent e)
    {
        switch (e)
        {
            case QuestionEvent q:
                Ask(RowOf(new TurnKey(q.Mode, q.Repeat, q.Task, q.Turn)), q.Text);
                break;
            case SseEvent s:
                Stream(RowOf(new TurnKey(s.Mode, s.Repeat, s.Task, s.Turn)), s);
                break;
            case TurnEvent t:
                RowOf(new TurnKey(t.Mode, t.Repeat, t.Task, t.Turn)).Finish(t);
                break;
        }
    }

    /// <summary>
    /// Each turn's cost from the run's requests, priced from the ledger, summed per turn: the ones so far while the
    /// run goes on (FX34), the store's when it has ended. Each call starts again from these requests, so asking after
    /// every turn never counts one twice.
    /// </summary>
    public void CostsFrom(IEnumerable<RequestRow> requests)
    {
        _costs.Clear();
        foreach (var request in requests)
        {
            // A request no turn made (a judge's) belongs to no row.
            if (request.Turn is not { } turn) continue;
            var key = new TurnKey(turn.Mode, turn.Repeat, turn.Task, turn.Turn);
            _costs[key] = UsageFormat.KnownSum([_costs.GetValueOrDefault(key), request.Cost]);
        }
    }

    /// <summary>What a turn cost; none before its requests came, or when no price was known.</summary>
    public decimal? CostOf(TurnKey key) => _costs.GetValueOrDefault(key);

    /// <summary>
    /// Opens a turn from the table. Opening the newest one follows new questions again; an older one stays open.
    /// </summary>
    public LiveTurn? Choose(TurnKey key)
    {
        if (_rows.FirstOrDefault(r => r.Key == key) is not { } row) return null;
        Open = row;
        _followOpen = row == _rows[^1];
        return row;
    }

    /// <summary>The turns per conversation, each in the order its first turn came.</summary>
    public IEnumerable<IGrouping<(string Mode, int Repeat, string Task), LiveTurn>> Conversations() =>
        _rows.GroupBy(r => (r.Mode, r.Repeat, r.Task));

    /// <summary>
    /// A question for a turn: its text, unless this page already streamed the turn; it opens when we follow.
    /// </summary>
    private void Ask(LiveTurn row, string? text)
    {
        if (!row.Streamed) row.Turn = new Turn { UserText = text ?? "" };
        if (_followOpen) Open = row;
    }

    /// <summary>A piece of the turn's stream, fed to the turn as the chat feeds its own.</summary>
    private static void Stream(LiveTurn row, SseEvent e)
    {
        TurnStream.Apply(row.Turn, SessionEvent.From(e.Type, e.Data));
        row.Streamed = true;
    }

    /// <summary>The turn's row, added at the end the first time it comes.</summary>
    private LiveTurn RowOf(TurnKey key)
    {
        if (_rows.FirstOrDefault(r => r.Key == key) is { } row) return row;
        _rows.Add(row = new LiveTurn(key));
        return row;
    }
}
