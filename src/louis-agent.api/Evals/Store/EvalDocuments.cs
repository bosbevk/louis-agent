using System.Text.Json;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Api.Evals.Store;

/// <summary>
/// What an eval run keeps, one record per collection of the store: the run, its conversations, their turns, and
/// the ledger's requests. A turn's stream events are kept apart because they are heavy and only a replay reads them.
/// Memory and MongoDB keep the same records, so one contract test holds both.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0022-benchmarks-recorded-from-the-protocol.md">ADR 0022: benchmark results in MongoDB</seealso>
public static class EvalDocuments
{
    /// <summary>
    /// One run of a check: what started it, how it ended, what it cost and the lines it printed. The protocol version
    /// it was recorded from is kept, so a store can tell a run of an older version and drop it, and so are its
    /// dataset and the time of its first line, so an export gives the first line back as it came.
    /// </summary>
    public sealed record Run(
        string Id,
        string Check,
        int Repeat,
        EvalRunStatus Status,
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
        RunSplit? Split = null,
        string? RescoreOf = null,
        IReadOnlyList<EvalSlice>? Slices = null,
        IReadOnlyList<AgentPrompt>? Prompts = null,
        int? ProtocolVersion = null,
        DatasetInfo? Dataset = null,
        DateTimeOffset? BegunAt = null);

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
    public sealed record Grader(string Name, int Version, CheckKind Kind, string? Question = null);

    /// <summary>Which model one level of a mode ran on, so runs on different models can be told apart.</summary>
    public sealed record ModelUse(string Mode, ModelLevel Level, string? Role, string? Model);

    /// <summary>
    /// One repeat of a mode: the toolsets and role it opened its sessions with, how many conversations it planned and
    /// when it started, and, once the repeat is done, when that was and its pass rates.
    /// </summary>
    public sealed record Mode(
        string Name,
        int Repeat,
        IReadOnlyList<string> Toolsets,
        string? Role,
        string? Llm,
        ModeRates? Rates = null,
        int? Conversations = null,
        DateTimeOffset? At = null,
        DateTimeOffset? DoneAt = null);

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
    /// How one conversation went: what it measured, its errors, time and each grader's verdict, and when it ended.
    /// `Order` is when it started, so the eval's own order holds. Its metrics are empty until it ends.
    /// </summary>
    public sealed record Conversation(
        ConversationKey Key,
        int Order,
        string? Role,
        Metrics? Metrics,
        int Errors,
        double Seconds,
        IReadOnlyList<Grade>? Grades = null,
        TrialOutcome? Outcome = null,
        DateTimeOffset? At = null);

    /// <summary>
    /// One grader's verdict on a conversation, with its reason and, for a model judge, which model and prompt gave it.
    /// </summary>
    public sealed record Grade(
        string Grader,
        int Version,
        double Score,
        GradeOutcome Outcome,
        string Reason,
        string? JudgeModel = null,
        string? JudgePromptHash = null);

    /// <summary>
    /// One turn of a conversation, without its stream events: what was asked and answered, what the tools did, how
    /// long it took, how it failed if it did, and when its question was asked and its answer ended.
    /// </summary>
    public sealed record Turn(
        TurnKey Key,
        string Session,
        string Question,
        string? Answer,
        string? Role,
        string? Continued,
        int AgentCalls,
        int SpecialistCalls,
        IReadOnlyList<string> Hosts,
        double Seconds,
        string? Error,
        EvalTools.Summary? Tools = null,
        TurnFailure? Failure = null,
        DateTimeOffset? AskedAt = null,
        DateTimeOffset? EndedAt = null);

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
