using System.Text.Json;

namespace LouisAgent.Api.Contracts.Admin;

/// <summary>
/// One conversation of a stored run: which one, the eval's scores, its errors and time, each grader's grade and the
/// outcome, and the people's latest grades beside them.
/// </summary>
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
    IReadOnlyList<HumanGradeRow> HumanGrades);

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
