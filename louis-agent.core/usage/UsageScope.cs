namespace louis_agent.core.usage;

/// <summary>
/// Ambient context for the requests of one turn, held in an <see cref="AsyncLocal{T}"/> so it flows across
/// <c>await</c> into the recording client without being passed through every call. A host opens one per turn;
/// disposing it restores the outer scope.
/// </summary>
/// <example>
/// From an ordinary async method (the CLI, the ACP server, the orchestrator):
/// <code>
/// using (UsageScope.Begin(UsagePurpose.Turn, host: "acp", session: id, turn: 3))
/// {
///     await foreach (var update in engine.StreamPromptAsync(...)) { ... }
/// }
/// </code>
/// From an async iterator (the API's SSE stream), where a scope it opens is lost at its first <c>yield</c>, activate the
/// scope around each step of the engine's stream instead:
/// <code>
/// using var usage = UsageScope.Begin(host: "api", session: id, turn: 3);
/// using (usage.Activate()) more = await updates.MoveNextAsync();
/// </code>
/// </example>
/// <summary>
/// Who the work is for, beyond the session: the task (e.g. "fix:e8b0b572223d"), the orchestrator run it belongs to, and
/// the service. Lets a ledger total a fix or a whole run across both agents (F1-S3).
/// </summary>
public sealed record UsageTags(string? Task, string? Run, string? Service)
{
    public static readonly UsageTags None = new(null, null, null);
}

public sealed class UsageScope : IDisposable
{
    private static readonly AsyncLocal<UsageScope?> CurrentScope = new();

    private readonly UsageScope? _outer;
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
            : new UsageTags(tags.Task ?? outerTags.Task, tags.Run ?? outerTags.Run, tags.Service ?? outerTags.Service);
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

    /// <summary>Restores the outer scope. Does nothing if a newer scope is current, so a late or double dispose is harmless.</summary>
    public void Dispose()
    {
        if (CurrentScope.Value == this) CurrentScope.Value = _outer;
    }
}