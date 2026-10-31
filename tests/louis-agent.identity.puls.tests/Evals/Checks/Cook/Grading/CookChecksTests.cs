using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.SessionStream;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading;

/// <summary>
/// The cook's no-error check, the general <c>no-error</c> under the name its grades carry, reads the errors from every
/// turn of a trial. Its route cases are in
/// <c>PulsRouteTests</c>.
/// </summary>
public class CookChecksTests
{
    /// <summary>The cook's mode: Puls hands the meal to its cook.</summary>
    private static readonly EvalMode Cook = new(CookMode.Cook.Wire(), PulsEvalRole.Cook.Wire(),
        [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]);

    /// <summary>The cook's no-error check, as its eval file builds it.</summary>
    private static readonly ICheck NoError = CookFile.Check("cook-no-error");

    /// <summary>A turn that errored fails the check.</summary>
    [Test]
    public async Task NoError_FailsOnAnErroredTurn()
    {
        var trial = Trial(Cook, PulsCookScenarioIds.CookAf, [Start(), Error("overloaded")]);

        var grade = await NoError.GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>With no turn failed, it passes.</summary>
    [Test]
    public async Task NoError_PassesWhenNoTurnFailed()
    {
        var trial = Trial(Cook, PulsCookScenarioIds.CookAf, [Start(), Text("Resep.")]);

        Assert.That((await NoError.GradeAsync(trial, CancellationToken.None)).Outcome,
            Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>
    /// Any turn the infrastructure broke, not just the first, leaves the check Unknown rather than a pass.
    /// </summary>
    [Test]
    public async Task NoError_IsUnknown_WhenAnyTurnFailedForTheInfrastructure_NotOnlyTheFirst()
    {
        // The follow-up timed out: whether it would have errored is unknown, so the conversation is not a pass. A
        // check of the first turn only would call this one a pass, a mutation no test noticed before this one.
        var trial = Trial(Cook, PulsCookScenarioIds.CookAf, [Start(), Text("Hier is dit vir ses.")]);
        trial = trial with
        {
            Transcript = new Transcript([
                trial.Transcript.Turns[0], trial.Transcript.Turns[1] with { Failure = TurnFailure.Timeout }
            ])
        };

        Assert.That((await NoError.GradeAsync(trial, CancellationToken.None)).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>A trial of this task with one turn per list of events.</summary>
    private static Trial Trial(EvalMode mode, string task, IReadOnlyList<IReadOnlyList<SessionEvent>> turns) =>
        new("run_1", mode, 1,
            new EvalScenario(task, new EvalInput(null, "Africa/Johannesburg", ["x"]), default, [], DatasetSplit.Dev),
            new Transcript(turns
                .Select((events, i) => new TurnTranscript(i + 1, "sess", "q", events, TimeSpan.Zero, null)).ToList()));

    /// <summary>Puls handing the question to its cook.</summary>
    private static IReadOnlyList<SessionEvent> Start() =>
    [
        SessionEvent.From("content_block_start",
            Json(
                """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"puls-cook"}}}"""))
    ];

    /// <summary>A piece of the answer's text.</summary>
    private static IReadOnlyList<SessionEvent> Text(string text) =>
    [
        SessionEvent.From("content_block_delta",
            Json("{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"" + text +
                 "\"}}"))
    ];

    /// <summary>An error the stream reported.</summary>
    private static IReadOnlyList<SessionEvent> Error(string message) =>
    [
        SessionEvent.From("error",
            Json("{\"type\":\"error\",\"error\":{\"type\":\"api_error\",\"message\":\"" + message + "\"}}"))
    ];

    /// <summary>A JSON value from its text.</summary>
    private static System.Text.Json.JsonElement Json(string text) =>
        System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
