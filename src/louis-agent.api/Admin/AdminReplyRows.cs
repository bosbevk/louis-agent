using System.Text.Json.Serialization;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Admin;

/// <summary>One check in the page's list: what it is called and whether it has a manual to open.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler). The row is built from a check plus one fact the check can't
/// tell by itself (whether its manual file exists), so <c>Of</c> names that and the endpoint is a call, not an object
/// built inside a lambda. Without it every endpoint would spell out the property names again.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record CheckRow(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("manual")] bool Manual)
{
    /// <summary>The row for a check, saying whether it has a manual.</summary>
    public static CheckRow Of(BenchmarkCheck check, bool hasManual) => new(check.Id, check.Title, hasManual);
}

/// <summary>Whether the store is there, and why not when it was asked and didn't answer: what the page's banner shows.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler). "Configured" is a fact about the store being present, so
/// <c>Of</c> takes the store and the error and works it out, and the two can't disagree.
/// </remarks>
internal sealed record StoreRow(
    [property: JsonPropertyName("configured")]
    bool Configured,
    [property: JsonPropertyName("error")] string? Error)
{
    /// <summary>The banner for a store that may be missing, with the reason it didn't answer if it didn't.</summary>
    public static StoreRow Of(bool configured, string? error) => new(configured, error);
}

/// <summary>What the admin page reads first: the checks, the store's state and the stored runs.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler). The reply is assembled from a catalogue, a folder and a
/// list of runs, so <c>Of</c> does that gathering in one place and the endpoint stays a few lines.
/// </remarks>
internal sealed record BenchmarkListReply(
    [property: JsonPropertyName("checks")] IReadOnlyList<CheckRow> Checks,
    [property: JsonPropertyName("store")] StoreRow Store,
    [property: JsonPropertyName("runs")] IReadOnlyList<object> Runs)
{
    /// <summary>The first read: every check with its manual flag, the store's state, and the runs as the page shows them.</summary>
    public static BenchmarkListReply Of(BenchmarkCatalog catalog, string workspaceRoot, StoreRow store,
        IReadOnlyList<object> runs) =>
        new([.. catalog.Checks.Select(c => CheckRow.Of(c, BenchmarkManuals.Has(workspaceRoot, c)))], store, runs);
}

/// <summary>A check's manual: which check, its title and the markdown beside its script.</summary>
/// <remarks>Replace Constructor with Factory Function (Fowler): the title comes from the check, so <c>Of</c> takes the check and the text.</remarks>
internal sealed record ManualReply(
    [property: JsonPropertyName("check")] string Check,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("markdown")]
    string Markdown)
{
    /// <summary>The reply for a check and its manual's text.</summary>
    public static ManualReply Of(BenchmarkCheck check, string markdown) => new(check.Id, check.Title, markdown);
}

/// <summary>One point of the overall trend: a run in a mode, with the nine rings' numbers.</summary>
/// <remarks>Replace Constructor with Factory Function (Fowler): built from the trend's own point, so <c>Of</c> carries the snake_case mapping once.</remarks>
internal sealed record TrendPointRow(
    [property: JsonPropertyName("run")] string Run,
    [property: JsonPropertyName("at")] DateTimeOffset At,
    [property: JsonPropertyName("cost")] decimal? Cost,
    [property: JsonPropertyName("score")] double? Score,
    [property: JsonPropertyName("conversations")]
    int Conversations,
    [property: JsonPropertyName("tokens")] long? Tokens,
    [property: JsonPropertyName("seconds_per_turn")]
    double? SecondsPerTurn,
    [property: JsonPropertyName("cache_share")]
    double? CacheShare,
    [property: JsonPropertyName("sourced")]
    double? Sourced,
    [property: JsonPropertyName("tools_failed")]
    double? ToolsFailed,
    [property: JsonPropertyName("cost_per_answer")]
    decimal? CostPerAnswer,
    [property: JsonPropertyName("sites")] int? Sites,
    [property: JsonPropertyName("pass_at_k")]
    double? PassAtK,
    [property: JsonPropertyName("pass_hat_k")]
    double? PassHatK,
    [property: JsonPropertyName("k")] int? K)
{
    /// <summary>The row for one point of the trend.</summary>
    public static TrendPointRow Of(BenchmarkTrends.Point p) => new(p.Run, p.At, p.Cost, p.Score, p.Conversations,
        p.Tokens, p.SecondsPerTurn, p.CacheShare, p.Sourced, p.ToolsFailed, p.CostPerAnswer, p.Sites, p.PassAtK,
        p.PassHatK, p.K);
}

/// <summary>One line of the overall trend: a mode and its points.</summary>
/// <remarks>Replace Constructor with Factory Function (Fowler): built from the trend's series.</remarks>
internal sealed record TrendLineRow(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("points")] IReadOnlyList<TrendPointRow> Points)
{
    /// <summary>The row for one line of the trend.</summary>
    public static TrendLineRow Of(BenchmarkTrends.Series line) =>
        new(line.Name, [.. line.Points.Select(TrendPointRow.Of)]);
}

/// <summary>A check's trend as the endpoint sends it: its name, a chart per category, and the overall lines.</summary>
/// <remarks>Replace Constructor with Factory Function (Fowler): built from the trend and the categories' rows, so <c>Of</c> is the one place that knows how they fit together.</remarks>
internal sealed record TrendReply(
    [property: JsonPropertyName("check")] string Check,
    [property: JsonPropertyName("categories")]
    IReadOnlyList<CategoryTrendRow> Categories,
    [property: JsonPropertyName("modes")] IReadOnlyList<TrendLineRow> Modes)
{
    /// <summary>The reply for a check's overall trend and its categories' charts.</summary>
    public static TrendReply Of(BenchmarkTrends.Trend trend, IReadOnlyList<CategoryTrendRow> categories) =>
        new(trend.Check, categories, [.. trend.Modes.Select(TrendLineRow.Of)]);
}
