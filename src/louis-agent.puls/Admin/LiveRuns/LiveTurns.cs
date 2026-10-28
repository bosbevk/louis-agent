using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Prices;
using LouisAgent.Puls.Admin.Scoring;
using LouisAgent.SessionStream;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.LiveRuns;

/// <summary>
/// The table of the run in progress: a row per turn as the script's events come in, the turns the store already
/// holds when the page picks a run up again, each conversation's score chips, each turn's cost once the run has
/// ended, and the turn that is open (the newest question, until you open another).
/// </summary>
/// <remarks>
/// The page's live state as a plain class, so what an event does to the table is tested without a browser; the panel
/// that shows it only reads the events and the store and hands them in.
/// </remarks>
public sealed class LiveTurns
{
    private readonly List<LiveTurn> _rows = [];
    private readonly Dictionary<(string Mode, int Repeat, string Task), IReadOnlyList<PricedChip>> _chips = [];
    private readonly Dictionary<TurnKey, decimal?> _costs = [];
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
        _chips.Clear();
        _costs.Clear();
        Open = null;
        _followOpen = true;
    }

    /// <summary>The turns and scores the store already holds of the run, as finished rows.</summary>
    public void FromStore(StoredRunReply run)
    {
        _rows.Clear();
        _chips.Clear();
        foreach (var stored in run.Turns)
        {
            var row = RowOf(new TurnKey(stored.Mode, stored.Repeat, stored.Task, stored.Turn));
            row.Turn = new Turn { UserText = stored.Question ?? "" };
            row.Finish(stored);
        }

        // Only a conversation the store has actually scored gets chips.
        foreach (var conversation in run.Conversations)
            if (conversation.Scores.ValueKind == JsonValueKind.Object && conversation.Scores.EnumerateObject().Any())
                _chips[(conversation.Mode, conversation.Repeat, conversation.Task)] =
                    ScoreChips.Of(conversation.Role, conversation.Scores);
    }

    /// <summary>
    /// One of the script's events into the table: a question opens its row, a piece of stream feeds the turn the
    /// chat's way, a turn's end fills in its figures, and a conversation's scores become its chips.
    /// </summary>
    public void Apply(RunEvent e)
    {
        switch (e.Kind)
        {
            case RunEventKind.Question:
                Ask(RowOf(e.Key), e.Text);
                break;
            case RunEventKind.Sse:
                Stream(RowOf(e.Key), e);
                break;
            case RunEventKind.Turn:
                RowOf(e.Key).Finish(e);
                break;
            case RunEventKind.Conversation when e.Scores is { ValueKind: JsonValueKind.Object } scores:
                _chips[(e.Key.Mode, e.Key.Repeat, e.Key.Task)] = ScoreChips.Of(e.Role, scores);
                break;
        }
    }

    /// <summary>
    /// Each turn's cost once the run has ended: the store's requests, priced from the ledger, summed per turn.
    /// </summary>
    public void CostsFrom(StoredRunReply run)
    {
        foreach (var request in run.Requests)
        {
            // A request no turn made (a judge's) belongs to no row.
            if (request.Turn is not { } turn) continue;
            var key = new TurnKey(turn.Mode, turn.Repeat, turn.Task, turn.Turn);
            _costs[key] = UsageFormat.KnownSum([_costs.GetValueOrDefault(key), request.Cost]);
        }
    }

    /// <summary>What a turn cost; none while the run goes on, or when no price was known.</summary>
    public decimal? CostOf(TurnKey key) => _costs.GetValueOrDefault(key);

    /// <summary>A conversation's score chips, once it has been scored.</summary>
    public IReadOnlyList<PricedChip>? ChipsOf(string mode, int repeat, string task) =>
        _chips.GetValueOrDefault((mode, repeat, task));

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
    private static void Stream(LiveTurn row, RunEvent e)
    {
        if (e.Data is { } data && e.Type is { } type) TurnStream.Apply(row.Turn, SessionEvent.From(type, data));
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
