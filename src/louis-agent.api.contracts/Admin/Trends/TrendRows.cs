namespace LouisAgent.Api.Contracts.Admin.Trends;

/// <summary>One point of the overall trend: a run in a mode, with the nine rings' numbers.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record TrendPointRow(
    string Run,
    DateTimeOffset At,
    decimal? Cost,
    double? Score,
    int Conversations,
    long? Tokens,
    double? SecondsPerTurn,
    double? CacheShare,
    double? Sourced,
    double? ToolsFailed,
    decimal? CostPerAnswer,
    int? Sites,
    double? PassAtK,
    double? PassHatK,
    int? K);

/// <summary>One line of the overall trend: a mode and its points.</summary>
public sealed record TrendLineRow(string Name, IReadOnlyList<TrendPointRow> Points);

/// <summary>A check's trend as the endpoint sends it: its name, a chart per category, and the overall lines.</summary>
public sealed record TrendReply(
    string Check,
    IReadOnlyList<CategoryTrendRow> Categories,
    IReadOnlyList<TrendLineRow> Modes);
