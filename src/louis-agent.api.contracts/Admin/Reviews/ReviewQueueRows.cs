using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Api.Contracts.Admin.Reviews;

/// <summary>
/// The answers waiting for a person's grade across the stored runs (F53.3): the first so many as items, newest run
/// first, and how many wait in all.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ReviewQueueReply(IReadOnlyList<ReviewQueueItem> Items, int Total);

/// <summary>
/// One answer waiting for a grade: which run and eval it is from, the run's graders, the conversation's row with
/// its grades and the people's, the questions asked and the last answer; what the review panel needs, as the run
/// page already reads it.
/// </summary>
public sealed record ReviewQueueItem(
    string Run,
    string Eval,
    DateTimeOffset StartedAt,
    IReadOnlyList<RunGraderRow> Graders,
    ConversationRow Conversation,
    IReadOnlyList<string> Asked,
    string? Answer);
