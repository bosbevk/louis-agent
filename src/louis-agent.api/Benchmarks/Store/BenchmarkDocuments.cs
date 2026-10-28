using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// What a benchmark run keeps, one record per collection of the store: the run, its conversations, their turns, and
/// the ledger's requests. A turn's stream events are kept apart because they are heavy and only a replay reads them.
/// Memory and MongoDB keep the same records, so one contract test holds both.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public static class BenchmarkDocuments
{
    /// <summary>One run of a check: what started it, how it ended, what it cost and the lines it printed.</summary>
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
        string? RescoreOf = null,
        IReadOnlyList<BenchmarkSlice>? Slices = null,
        IReadOnlyList<AgentPrompt>? Prompts = null);

    /// <summary>
    /// Which prompts one agent used in one mode of a run, as their hashes. The trend compares these agent by agent, so
    /// an agent a run never called doesn't look like a changed prompt.
    /// </summary>
    public sealed record AgentPrompt(string? Mode, string Agent, IReadOnlyList<string> Hashes);

    /// <summary>
    /// A person's note on a run, or on one of its conversations. It's kept apart from the run itself, so the recorder
    /// rewriting the run can't wipe it, and it's never edited: a correction is a new note.
    /// </summary>
    public sealed record RunNote(string Text, DateTimeOffset At, ConversationKey? Conversation = null);

    /// <summary>One of the eval's graders, as the run started.</summary>
    public sealed record Grader(string Name, int Version, BenchmarkGraderKind Kind, string? Question = null);

    /// <summary>Which model one level of a mode ran on, so runs on different models can be told apart.</summary>
    public sealed record ModelUse(string Mode, BenchmarkModelLevel Level, string? Role, string? Model);

    /// <summary>
    /// One repeat of a mode: the toolsets and role it opened its sessions with and, once the repeat is done, its pass
    /// rates.
    /// </summary>
    public sealed record Mode(
        string Name,
        int Repeat,
        IReadOnlyList<string> Toolsets,
        string? Role,
        string? Llm,
        ModeRates? Rates = null);

    /// <summary>
    /// The pass rates the runner wrote when a repeat finished: pass@k and pass^k, over how many trials, and how many
    /// were left out or run again. We only keep them; the maths is the runner's.
    /// </summary>
    public sealed record ModeRates(double? PassAtK, double? PassHatK, int K, int Infrastructure, int Retried);

    /// <summary>
    /// What a run cost, worked out from the ledger. The cost is empty when a price was unknown, never zero.
    /// </summary>
    public sealed record Total(decimal? Cost, string Currency, long Tokens, int Requests);

    /// <summary>
    /// How one conversation went: its scores, errors, time and each grader's verdict. `Order` is when it started, so
    /// the eval's own order holds.
    /// </summary>
    public sealed record Conversation(
        ConversationKey Key,
        int Order,
        string? Role,
        JsonElement Scores,
        int Errors,
        double Seconds,
        IReadOnlyList<Grade>? Grades = null,
        BenchmarkOutcome? Outcome = null);

    /// <summary>
    /// One grader's verdict on a conversation, with its reason and, for a model judge, which model and prompt gave it.
    /// </summary>
    public sealed record Grade(
        string Grader,
        int Version,
        double Score,
        BenchmarkOutcome Outcome,
        string Reason,
        string? JudgeModel = null,
        string? JudgePromptHash = null);

    /// <summary>
    /// One turn of a conversation, without its stream events: what was asked and answered, what the tools did, how
    /// long it took, and how it failed if it did.
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

    /// <summary>One event from a turn's stream, kept so the turn can be replayed.</summary>
    public sealed record StreamEvent(string Type, JsonElement Data, DateTimeOffset? At);

    /// <summary>
    /// One model request from the ledger, tied to its turn when we could find it. Tokens and cost are empty when the
    /// ledger did not know them, never zero.
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
