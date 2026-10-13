namespace LouisAgent.Core.Usage;

public sealed class UsageScope : IDisposable
{
    private static readonly AsyncLocal<UsageScope?> CurrentScope = new();

    private readonly UsageScope? _outer;
    private readonly List<UsageRecord> _records = [];
    private int _round;

    private UsageScope(UsageScope? outer, string purpose, string? host, string? session, int? turn, UsageTags tags)
    {
        _outer = outer;
        Purpose = purpose;
        Host = host;
        Session = session;
        Turn = turn;
        Tags = tags;
    }

    /// <summary>The innermost open scope, or null outside any scope.</summary>
    public static UsageScope? Current => CurrentScope.Value;

    /// <summary>
    /// Purpose of the requests made in this scope. <c>AgentEngine.StreamPromptAsync</c> switches it to
    /// <see cref="UsagePurpose.Continue"/> after a cut-off reply, for the rest of the turn.
    /// </summary>
    public string Purpose { get; set; }

    /// <summary>cli | acp | api | orchestrator.</summary>
    public string? Host { get; }

    /// <summary>The host's session id, if it has sessions.</summary>
    public string? Session { get; }

    /// <summary>User message number in the session, from 1.</summary>
    public int? Turn { get; }

    /// <summary>What the work is for (task, run, service), set by whoever started it, e.g. the orchestrator for a fix.</summary>
    public UsageTags Tags { get; }

    /// <summary>
    /// Opens a scope inside the current one. What isn't given is taken from the outer scope, so a summary's scope keeps
    /// the turn's host, session, turn and tags. Dispose it to restore the outer scope.
    /// </summary>
    public static UsageScope Begin(string purpose = UsagePurpose.Turn, string? host = null, string? session = null, int? turn = null,
        UsageTags? tags = null)
    {
        UsageScope? outer = Current;
        UsageTags outerTags = outer?.Tags ?? UsageTags.None;
        UsageTags merged = tags is null
            ? outerTags
            : new UsageTags(tags.Task ?? outerTags.Task, tags.Run ?? outerTags.Run, tags.Service ?? outerTags.Service,
                tags.Parent ?? outerTags.Parent);
        var scope = new UsageScope(outer, purpose, host ?? outer?.Host, session ?? outer?.Session, turn ?? outer?.Turn, merged);
        CurrentScope.Value = scope;
        return scope;
    }

    /// <summary>
    /// Makes this scope current until the result is disposed, then restores what was current before. For async iterators,
    /// which can't keep a scope current across a <c>yield</c>.
    /// </summary>
    public Activation Activate()
    {
        var activation = new Activation(this, CurrentScope.Value);
        CurrentScope.Value = this;
        return activation;
    }

    /// <summary>Undoes one <see cref="Activate"/>.</summary>
    public readonly struct Activation(UsageScope scope, UsageScope? previous) : IDisposable
    {
        public void Dispose()
        {
            if (CurrentScope.Value == scope) CurrentScope.Value = previous;
        }
    }

    /// <summary>Numbers the next request of this scope: 1, 2, 3… (F1-S1: a turn with 7 tool rounds has rounds 1–7).</summary>
    /// <remarks>Interlocked, because a summary request can run while another request of the same turn is in flight.</remarks>
    public int NextRound() => Interlocked.Increment(ref _round);

    /// <summary>
    /// Keeps a finished request's record (as the ledger stored it, with its cost) and passes it to the outer scope, so a
    /// summary's nested scope still counts toward its turn (F3-S1).
    /// </summary>
    /// <remarks>Locked, for the same reason as <see cref="NextRound"/>.</remarks>
    public void Add(UsageRecord record)
    {
        lock (_records) _records.Add(record);
        _outer?.Add(record);
    }

    /// <summary>The records of the requests made in this scope and in the scopes nested in it, in the order they finished.</summary>
    public IReadOnlyList<UsageRecord> Records
    {
        get { lock (_records) return [.. _records]; }
    }

    /// <summary>
    /// What this scope's requests used and cost so far, totalled the same way as a ledger (<see cref="UsageReport.Total"/>)
    /// so a turn's line and a ledger report can't disagree.
    /// </summary>
    public UsageTotal Total() => UsageReport.Total(TotalKey, Records);

    /// <summary>The <see cref="UsageTotal.Key"/> of <see cref="Total"/>: one scope has one total, so there's nothing to group by.</summary>
    internal const string TotalKey = "scope";

    /// <summary>Restores the outer scope. Does nothing if a newer scope is current, so a late or double dispose is harmless.</summary>
    public void Dispose()
    {
        if (CurrentScope.Value == this) CurrentScope.Value = _outer;
    }
}
