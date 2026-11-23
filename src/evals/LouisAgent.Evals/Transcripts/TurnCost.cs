namespace LouisAgent.Evals.Transcripts;

/// <summary>
/// What a turn cost, in the currency it was charged in. A cost nobody could price is null, never 0, so a total
/// never claims to know what it doesn't.
/// </summary>
/// <param name="Amount">The cost, or null when nobody could price it.</param>
/// <param name="Currency">The currency it was charged in, or null with no cost.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record TurnCost(decimal? Amount, string? Currency)
{
    /// <summary>No cost known: the turn reported no priced usage.</summary>
    public static readonly TurnCost None = new(null, null);
}
