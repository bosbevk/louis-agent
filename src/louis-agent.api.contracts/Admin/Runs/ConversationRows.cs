using System.Text.Json;

namespace LouisAgent.Api.Contracts.Admin.Runs;

/// <summary>
/// One conversation of a stored run: which one, its errors and time, each grader's grade and the outcome, the people's
/// latest grades beside them, and what it measured. <see cref="Scores"/> is always an empty object now: the eval's own
/// scores went with protocol version 2, and the field goes in F18 step 7c.5 with the portal code that reads it.
/// </summary>
/// <remarks>
/// Expand and Contract (Fowler's parallel change): <see cref="Metrics"/> is added while <see cref="Scores"/> stays, so
/// the portal builds unchanged until it reads the metrics.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ConversationRow(
    string Mode,
    int Repeat,
    string Task,
    int Order,
    string? Role,
    JsonElement Scores,
    int Errors,
    double Seconds,
    IReadOnlyList<GradeRow>? Grades,
    string? Outcome,
    IReadOnlyList<HumanGradeRow> HumanGrades,
    MetricsRow? Metrics = null);

/// <summary>
/// What a conversation measured: the agent's and the specialists' calls, the sites read, its turns, its tokens in and
/// out, its seconds and its cost. Each is null when it isn't known, never 0; the whole is null before it ended.
/// </summary>
public sealed record MetricsRow(
    int? AgentCalls,
    int? SpecialistCalls,
    IReadOnlyList<string>? Hosts,
    int? Turns,
    long? TokensIn,
    long? TokensOut,
    double? Seconds,
    decimal? Cost);

/// <summary>One grader's grade: its score, outcome and reason, and for a model judge which model and prompt.</summary>
public sealed record GradeRow(
    string Grader,
    int Version,
    double Score,
    string Outcome,
    string Reason,
    string? JudgeModel,
    string? JudgePromptHash);

/// <summary>
/// A person's grade of a conversation for a judge: the verdict, the note, who and when, and whether it is that
/// person's latest.
/// </summary>
public sealed record HumanGradeRow(
    string Mode,
    int Repeat,
    string Task,
    string Grader,
    string ModelGrader,
    string Verdict,
    string Note,
    string Reviewer,
    DateTimeOffset At,
    bool Latest);
