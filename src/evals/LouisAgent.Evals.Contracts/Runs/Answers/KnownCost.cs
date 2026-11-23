namespace LouisAgent.Evals.Contracts.Runs.Answers;

/// <summary>
/// Costs added the way the ledger adds them: the known ones, and null when none has a price, never 0
/// (ADR 0007). The web's own <c>UsageFormat.KnownSum</c> adds the same way; the contracts reference nothing, so the
/// rule is written once here for a run.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class KnownCost
{
    /// <summary>The known costs added; null when none is known.</summary>
    public static decimal? Sum(IEnumerable<decimal?> costs)
    {
        var known = costs.Where(cost => cost is not null).Select(cost => cost!.Value).ToList();
        return known.Count == 0 ? null : known.Sum();
    }
}
