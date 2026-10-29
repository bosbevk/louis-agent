namespace LouisAgent.Api.Contracts.Admin.Runs;

/// <summary>
/// One conversation of a stored run: which one, its errors and time, each grader's grade and the outcome, the people's
/// latest grades beside them, and what it measured.
/// </summary>
/// <remarks>
/// Expand and Contract (Fowler's parallel change), finished: <see cref="Metrics"/> was added while the eval's own
/// scores stayed, so the portal built unchanged until it read the grades and metrics; the scores went in F18 step 7c.5
/// once nothing read them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public sealed record ConversationRow(
    string Mode,
    int Repeat,
    string Task,
    int Order,
    string? Role,
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
