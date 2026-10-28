using LouisAgent.Choices;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>
/// What a person says of an answer: pass, fail, or unsure. An unsure is kept and shown, but it's never a label.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum HumanVerdict
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one
    // lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Pass")] Pass,
    [WireName("Fail")] Fail,
    [WireName("Unsure")] Unsure,
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

/// <summary>How the portal's API words a verdict.</summary>
public static class HumanVerdicts
{
    /// <summary>
    /// The verdict in lower case, as the portal's API says it. The store keeps the wire name instead (PascalCase, as
    /// stored reviews have it), so this is spelled out and never derived from it.
    /// </summary>
    public static string Word(HumanVerdict verdict) => verdict switch
    {
        HumanVerdict.Pass => "pass",
        HumanVerdict.Fail => "fail",
        HumanVerdict.Unsure => "unsure",
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null),
    };
}
