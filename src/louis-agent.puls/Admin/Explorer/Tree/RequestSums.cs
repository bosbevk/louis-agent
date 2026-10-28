using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Explorer.Tree;

/// <summary>
/// Adds up some requests the ways the explorer's charts split them: Puls's own part against the specialists', and the
/// four kinds of token.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class RequestSums
{
    /// <summary>What Puls's own requests cost, or the specialists': nothing if none was priced, never zero.</summary>
    public static decimal? Cost(IEnumerable<RequestNode> rows, bool specialist) =>
        UsageFormat.KnownSum(rows.Where(r => r.Specialist == specialist).Select(r => r.Cost));

    /// <summary>The model's seconds on Puls's own requests, or on the specialists'.</summary>
    public static double Seconds(IEnumerable<RequestNode> rows, bool specialist) =>
        rows.Where(r => r.Specialist == specialist).Sum(r => r.DurationMs) / 1000.0;

    /// <summary>The four kinds of token added up: input, cache written, cache read and output.</summary>
    public static long[] Tokens(IEnumerable<RequestNode> rows)
    {
        var list = rows.ToList();
        return
        [
            list.Sum(r => r.Input ?? 0), list.Sum(r => r.CacheWrite ?? 0), list.Sum(r => r.CacheRead ?? 0),
            list.Sum(r => r.Output ?? 0)
        ];
    }
}
