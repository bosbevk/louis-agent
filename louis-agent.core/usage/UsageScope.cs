namespace louis_agent.core.usage;

/// <summary>
/// Ambient context for the requests of one turn, held in an <see cref="AsyncLocal{T}"/> so it flows across
/// <c>await</c> into the recording client without being passed through every call. A host opens one per turn;
/// disposing it restores the outer scope.
/// </summary>
/// <example>
/// <code>
/// using (UsageScope.Begin(UsagePurpose.Turn))
/// {
///     await foreach (var update in engine.StreamPromptAsync(...)) { ... }
/// }
/// </code>
/// </example>
public sealed class UsageScope : IDisposable
{
    private static readonly AsyncLocal<UsageScope?> CurrentScope = new();

    private readonly UsageScope? _outer;
    private int _round;

    private UsageScope(UsageScope? outer, string purpose)
    {
        _outer = outer;
        Purpose = purpose;
    }

    /// <summary>The innermost open scope, or null outside any scope.</summary>
    public static UsageScope? Current => CurrentScope.Value;

    /// <summary>Purpose of the requests made in this scope; <see cref="UsagePurpose.Continue"/> is set per request in F1-S2.</summary>
    public string Purpose { get; set; }

    // TODO F1-S2: Host, Session, Turn. TODO F1-S3: Task, Run, Service.

    /// <summary>Opens a scope inside the current one. Dispose it to restore the outer scope.</summary>
    public static UsageScope Begin(string purpose = UsagePurpose.Turn)
    {
        var scope = new UsageScope(Current, purpose);
        CurrentScope.Value = scope;
        return scope;
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