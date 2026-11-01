using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Drafts;

/// <summary>
/// A new sample a person made from a stored run's conversation, waiting to be published: which eval and identity it is
/// for, the run and conversation it came from, the sample itself, who made it and when. It never changes the eval by
/// itself: F49.5's publish writes its scenario file and opens the pull request, so git stays the truth.
/// </summary>
/// <remarks>
/// A draft belongs to one eval (ADR 0033), and outlives its run: deleting the run leaves it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public sealed record SampleDraft(
    string Id,
    string Eval,
    string Identity,
    string Run,
    ConversationKey Conversation,
    SampleBody Sample,
    string Author,
    DateTimeOffset At);
