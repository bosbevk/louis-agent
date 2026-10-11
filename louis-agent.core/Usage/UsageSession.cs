namespace LouisAgent.Core.Usage;

using LouisAgent.Core.Pricing;

/// <summary>
/// A host session's usage so far: each finished turn's records, for the session total on the usage line and for
/// <c>/usage</c> (F3). The host keeps one per session (the CLI per run, the API and Rider per chat), because the engine
/// serves every session and can't tell them apart.
/// </summary>
/// <param name="display">The currency its lines show costs in (F14); null shows them as charged.</param>
public sealed class UsageSession(DisplayCurrency? display = null)
{
    private readonly List<UsageRecord> _records = [];

    /// <summary>Every record of the session's finished turns, in order.</summary>
    public IReadOnlyList<UsageRecord> Records
    {
        get { lock (_records) return [.. _records]; }
    }

    /// <summary>
    /// Adds a finished turn's records and returns its usage line (<see cref="UsageText.Line"/>, with the session total),
    /// or null when the turn made no model request (nothing to show).
    /// </summary>
    /// <remarks>Locked: an API session's turns could in principle overlap.</remarks>
    public string? EndTurn(IReadOnlyList<UsageRecord> turn)
    {
        if (turn.Count == 0) return null;
        IReadOnlyList<UsageRecord> session;
        lock (_records)
        {
            _records.AddRange(turn);
            session = [.. _records];
        }
        return UsageText.Line(turn, session, display);
    }
}