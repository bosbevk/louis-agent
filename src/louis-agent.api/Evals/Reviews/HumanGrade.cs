using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Reviews;

/// <summary>
/// What a person says of an answer: pass, fail, or unsure. An unsure is kept and shown, but it's never a label. The
/// store keeps the same lower-case word the portal's API sends.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0049-nothing-is-compatible-before-a-sale.md">ADR 0049: nothing is compatible before a sale</seealso>
public enum HumanVerdict
{
    [WireName("pass")] Pass,
    [WireName("fail")] Fail,
    [WireName("unsure")] Unsure,
}

/// <summary>
/// A person's grade of one conversation of a stored run: the verdict, a note, who gave it and when. It answers one
/// model grader (<c>meal-for-people</c>); the run's human review of it is named <c>{grader}.human</c>.
/// </summary>
/// <remarks>
/// The portal's gate is one shared key, so the reviewer is a name the person typed, not proof of who they are. A
/// grade is added and never replaced: a reviewer's latest grade counts, and the earlier ones are the history.
/// </remarks>
public sealed record HumanGrade(
    string Run,
    ConversationKey Conversation,
    string Grader,
    HumanVerdict Verdict,
    string Note,
    string Reviewer,
    DateTimeOffset At);
