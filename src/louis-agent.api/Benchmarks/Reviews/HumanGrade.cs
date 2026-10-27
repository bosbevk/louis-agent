using LouisAgent.Choices;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reviews;

/// <summary>What a person says of an answer (F18 step 6a.2): pass, fail, or unsure, which is kept and shown but is never a label.</summary>
public enum HumanVerdict
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("Pass")] Pass,
    [WireName("Fail")] Fail,
    [WireName("Unsure")] Unsure,
}

/// <summary>
/// A person's grade of one conversation of a stored run (F18 step 6a.2): <see cref="Grader"/> is the model grader it answers (<c>meal-for-people</c>; the run's human review is named
/// <c>{grader}.human</c>), with the verdict, a note, who and when. The portal's gate is one shared key, so <see cref="Reviewer"/> is a name the person gave, not proof of who they are.
/// A grade is added and never replaced: the latest of a reviewer's grades counts, and the earlier ones are the history.
/// </summary>
public sealed record HumanGrade(string Run, ConversationKey Conversation, string Grader, HumanVerdict Verdict, string Note, string Reviewer, DateTimeOffset At);

public static class HumanVerdicts
{
    /// <summary>The verdict as the portal's API words it, in lower case; the store keeps <c>verdict.Wire()</c> (PascalCase, as stored reviews have it). Spelled out, never derived from the wire name.</summary>
    public static string Word(HumanVerdict verdict) => verdict switch
    {
        HumanVerdict.Pass => "pass",
        HumanVerdict.Fail => "fail",
        HumanVerdict.Unsure => "unsure",
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null),
    };
}
