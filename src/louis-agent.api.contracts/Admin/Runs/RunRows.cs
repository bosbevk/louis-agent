namespace LouisAgent.Api.Contracts.Admin.Runs;

/// <summary>
/// A stored run as the admin pages list it: what ran, how it ended, what it cost, which models it ran on, and the
/// eval and graders it was run with, the run it re-scored or replayed, if any, and whether it recorded its answers,
/// so it can be replayed (FX28). A run from before a field was kept sends null for it, never <c>[]</c> or 0.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record StoredRunRow(
    string Id,
    string Check,
    int Repeat,
    string Status,
    int? ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    RunTotalRow? Total,
    string? StoreError,
    IReadOnlyList<RunModelRow>? Models,
    string? Eval,
    int? EvalVersion,
    string? Commit,
    string? RescoreOf,
    IReadOnlyList<RunGraderRow>? Graders,
    string? ReplayOf = null,
    bool Recorded = false);

/// <summary>What a run cost, from the ledger; the cost is null when a price was unknown, never 0.</summary>
public sealed record RunTotalRow(decimal? Cost, string Currency, long Tokens, int Requests);

/// <summary>Which model one level of a mode ran on.</summary>
public sealed record RunModelRow(string Mode, string Level, string? Role, string? Model);

/// <summary>One grader of the run's eval: its name, version and kind, and a human review's question.</summary>
public sealed record RunGraderRow(string Name, int Version, string Kind, string? Question);

/// <summary>
/// A run the runner holds now: how far it is, and once it is done whether its recording kept everything. A judge
/// check says which run and judge it checks, and its last line, since its lines are not streamed to a page.
/// </summary>
public sealed record LiveRunRow(
    string Id,
    string Check,
    int Repeat,
    string Status,
    int? ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int Lines,
    bool? Stored,
    string? StoreError,
    string? CalibrationOf,
    string? Grader,
    string? LastLine);
