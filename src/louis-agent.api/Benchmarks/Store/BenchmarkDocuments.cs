using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// What a benchmark run keeps (F31), one record per collection of the store: the run, its conversations, their turns
/// (with the turn's stream events apart, read only for a replay) and the ledger's requests. The same records in memory
/// and in MongoDB, so a contract test holds both to one behaviour.
/// </summary>
public static class BenchmarkDocuments
{
    /// <summary>A run of a check: what started it, how it ended, what it cost (step 4), and the plain lines (its log).</summary>
    public sealed record Run(
        string Id,
        string Check,
        int Repeat,
        BenchmarkRunStatus Status,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt = null,
        int? ExitCode = null,
        IReadOnlyDictionary<string, string>? Settings = null,
        IReadOnlyList<Mode>? Modes = null,
        Total? Total = null,
        IReadOnlyList<string>? Log = null,
        string? StoreError = null,
        string? Eval = null,
        int? EvalVersion = null,
        string? Commit = null,
        IReadOnlyList<Grader>? Graders = null,
        IReadOnlyList<ModelUse>? Models = null,
        BenchmarkRunSplit? Split = null,
        string? RescoreOf = null);

    /// <summary>One of the eval's graders, as the run started (F18 step 2a).</summary>
    public sealed record Grader(string Name, int Version, BenchmarkGraderKind Kind);

    /// <summary>The model one level of the run ran on (F18 step 2a): its mode, its level and the model ("unknown" when not known).</summary>
    public sealed record ModelUse(string Mode, BenchmarkModelLevel Level, string? Role, string? Model);

    /// <summary>
    /// A mode's repeat as it opened its sessions, and (F18 step 5a) the pass@k and pass^k the runner wrote when it was done, over the repeats up to
    /// this one; null for a run from before the rates, or a repeat still going.
    /// </summary>
    public sealed record Mode(string Name, int Repeat, IReadOnlyList<string> Toolsets, string? Role, string? Llm, ModeRates? Rates = null);

    /// <summary>
    /// What the runner wrote on a mode's <c>mode_done</c> line (F18 step 5a): its pass@k and pass^k (null when no trial counted: never 0), the k they are
    /// over, and how many trials were left out for the infrastructure or run a second time. The API only keeps them; the formula is the harness's.
    /// </summary>
    public sealed record ModeRates(double? PassAtK, double? PassHatK, int K, int Infrastructure, int Retried);

    /// <summary>The run's cost, from the ledger with <c>UsageReport.Total</c>; <see cref="Cost"/> null when a price was unknown.</summary>
    public sealed record Total(decimal? Cost, string Currency, long Tokens, int Requests);

    /// <summary>A conversation's result; <see cref="Order"/> is when it started in its run, so the check's order holds.</summary>
    public sealed record Conversation(ConversationKey Key, int Order, string? Role, JsonElement Scores, int Errors, double Seconds,
        IReadOnlyList<Grade>? Grades = null, BenchmarkOutcome? Outcome = null);

    /// <summary>One grader's verdict on a conversation (F18 step 2a).</summary>
    public sealed record Grade(string Grader, int Version, double Score, BenchmarkOutcome Outcome, string Reason);

    /// <summary>
    /// One turn, without its stream events (<see cref="IBenchmarkStore.TurnEventsAsync"/>). <see cref="Tools"/> is what
    /// its tools did, counted when it was recorded; null for a turn recorded before the counts were kept. <see cref="Failure"/> is
    /// the infrastructure's failure that ended it (F18 step 5.1), null when it ended on its own.
    /// </summary>
    public sealed record Turn(
        TurnKey Key,
        string Session,
        string Question,
        string? Answer,
        string? Role,
        string? Continued,
        int PulsCalls,
        int SpecialistCalls,
        IReadOnlyList<string> Hosts,
        double Seconds,
        string? Error,
        BenchmarkTools.Summary? Tools = null,
        BenchmarkTurnFailure? Failure = null);

    /// <summary>One stream event of a turn, kept for its replay.</summary>
    public sealed record StreamEvent(string Type, JsonElement Data, DateTimeOffset? At);

    /// <summary>
    /// One model request from the ledger (step 4), with the turn it belongs to when the join found one. Tokens and cost
    /// are null when the ledger doesn't know them (a cancelled request, FX9; a model without a price), never 0.
    /// </summary>
    public sealed record Request(
        string Run,
        TurnKey? Turn,
        DateTimeOffset At,
        string Purpose,
        string? Session,
        string? Parent,
        string? Model,
        long? InputTokens,
        long? OutputTokens,
        long? CacheReadTokens,
        long? CacheWriteTokens,
        decimal? Cost,
        long DurationMs);
}
