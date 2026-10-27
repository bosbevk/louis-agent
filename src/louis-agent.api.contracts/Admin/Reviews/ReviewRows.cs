using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Api.Contracts.Admin.Reviews;

/// <summary>A run's people's grades, history included, each marked with whether it is its reviewer's latest.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ReviewsReply(IReadOnlyList<HumanGradeRow> Reviews);

/// <summary>
/// What the portal sends to grade a conversation: the verdict as a word, a note, and the reviewer's name.
/// </summary>
public sealed record ReviewRequest(string? Verdict, string? Note, string? Reviewer);
