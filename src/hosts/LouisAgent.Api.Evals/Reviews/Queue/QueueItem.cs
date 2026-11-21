using LouisAgent.Api.Evals.Reviews.Store;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reviews.Queue;

/// <summary>
/// One answer waiting for a grade, as the store holds it: its run, the conversation, the people's latest grades of
/// it, and its turns in order (empty past the limit, where only the count is wanted).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record QueueItem(
    Run Run,
    Conversation Conversation,
    IReadOnlyList<HumanGrade> Human,
    IReadOnlyList<Turn> Turns);
