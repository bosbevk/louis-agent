using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.SessionStream;

namespace LouisAgent.Identities.Puls.Judges.Tests;

/// <summary>
/// The cook's trials as the judge's tests need them: a scenario's expected keys and its answers, built from canned
/// events. A cut of the Puls checks' own helper, which lives in the identity's tests project.
/// </summary>
internal static class JudgeTrials
{
    /// <summary>The cook's mode: Puls hands a meal to its cook.</summary>
    public static readonly EvalMode CookEvalMode =
        new(CookMode.Cook.Wire(), "puls/voice-af", [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]);

    /// <summary>A trial of a scenario with the given <c>expected</c> JSON and place.</summary>
    public static Trial Trial(EvalMode mode, string task, string expected, string? place,
        params TurnTranscript[] turns) =>
        new("run_1", mode, 1,
            new EvalScenario(task, new EvalInput(place, "Africa/Johannesburg", ["x"]),
                JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev),
            new Transcript(turns));

    /// <summary>One turn of the conversation, made of these events.</summary>
    public static TurnTranscript Turn(int number, IReadOnlyList<SessionEvent> events) =>
        new(number, "sess_1", "q", events, TimeSpan.FromSeconds(1), null);

    /// <summary>A piece of the answer's text, as the stream carries it.</summary>
    public static SessionEvent Text(string text) =>
        SessionEvent.From("content_block_delta", JsonDocument.Parse(
                """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":TEXT}}"""
                    .Replace("TEXT", JsonSerializer.Serialize(text)))
            .RootElement.Clone());
}
