using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Contracts.Admin;

namespace LouisAgent.Api.Admin;

/// <summary>
/// Builds the admin replies from the API's own types: each contract record gets its <c>Of</c> here, so a handler reads
/// <c>CheckRow.Of(check, hasManual)</c> and never spells out a reply's fields itself.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), written as C# 14 static extension members. The records live in
/// louis-agent.api.contracts, which references nothing, so they can't see a check, a trend or a category; the factory
/// sits here beside those types and still reads as <c>Record.Of</c> where it's called (the unattended run's finding 1).
/// Without it, either the contract would drag the server into every client or every call site would build the record
/// by hand.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class AdminReplies
{
    /// <summary>A check's row says whether it has a manual, which the check can't tell by itself.</summary>
    extension(CheckRow)
    {
        /// <summary>The row for a check, saying whether it has a manual.</summary>
        public static CheckRow Of(BenchmarkCheck check, bool hasManual) => new(check.Id, check.Title, hasManual);
    }

    /// <summary>
    /// The store's banner works out "configured" from the store being there, so the two can't disagree.
    /// </summary>
    extension(StoreRow)
    {
        /// <summary>
        /// The banner for a store that may be missing, with the reason it didn't answer if it didn't.
        /// </summary>
        public static StoreRow Of(bool configured, string? error) => new(configured, error);
    }

    /// <summary>The page's first read is gathered here, so its endpoint stays a few lines.</summary>
    extension(BenchmarkListReply)
    {
        /// <summary>
        /// Every check with its manual flag, the store's state, and the runs as the page shows them.
        /// </summary>
        public static BenchmarkListReply Of(BenchmarkCatalog catalog, string workspaceRoot, StoreRow store,
            IReadOnlyList<StoredRunRow> runs) =>
            new([.. catalog.Checks.Select(c => CheckRow.Of(c, BenchmarkManuals.Has(workspaceRoot, c)))], store, runs);
    }

    /// <summary>A manual's title comes from its check.</summary>
    extension(ManualReply)
    {
        /// <summary>The reply for a check and its manual's text.</summary>
        public static ManualReply Of(BenchmarkCheck check, string markdown) => new(check.Id, check.Title, markdown);
    }

    /// <summary>A trend point copies the trend's own point across, in one place.</summary>
    extension(TrendPointRow)
    {
        /// <summary>The row for one point of the trend.</summary>
        public static TrendPointRow Of(BenchmarkTrends.Point p) => new(p.Run, p.At, p.Cost, p.Score,
            p.Conversations, p.Tokens, p.SecondsPerTurn, p.CacheShare, p.Sourced, p.ToolsFailed, p.CostPerAnswer,
            p.Sites, p.PassAtK, p.PassHatK, p.K);
    }

    /// <summary>A trend line is a series and its points.</summary>
    extension(TrendLineRow)
    {
        /// <summary>The row for one line of the trend.</summary>
        public static TrendLineRow Of(BenchmarkTrends.Series line) =>
            new(line.Name, [.. line.Points.Select(TrendPointRow.Of)]);
    }

    /// <summary>
    /// The trend reply is the one place that knows how the overall lines and the categories fit together.
    /// </summary>
    extension(TrendReply)
    {
        /// <summary>The reply for a check's overall trend and its categories' charts.</summary>
        public static TrendReply Of(BenchmarkTrends.Trend trend, IReadOnlyList<CategoryTrendRow> categories) =>
            new(trend.Check, categories, [.. trend.Modes.Select(TrendLineRow.Of)]);
    }

    /// <summary>
    /// A category's chart has the shape of the overall trend's lines, so the portal draws both the same way.
    /// </summary>
    extension(CategoryTrendRow)
    {
        /// <summary>The charts for the categories; a slice with too few trials was never made a point.</summary>
        public static IReadOnlyList<CategoryTrendRow> Of(IReadOnlyList<CategoryTrends.Category> categories) =>
        [
            .. categories.Select(category => new CategoryTrendRow(category.Name,
            [
                .. category.Lines.Select(line => new CategoryLineRow(line.Name, line.Model,
                [
                    .. line.Points.Select(p =>
                        new CategoryPointRow(p.Run, p.At, p.PassRate, p.PassAtK, p.PassHatK, p.N))
                ]))
            ]))
        ];
    }
}
