using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using LouisAgent.Evals.Puls.Tests.Shared.Grading;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// The cook's no-error grader (F31): the errors, read from a trial's turns. Its route cases are PulsRouteTests now (F18 step 5.4).
public class CookGradersTests
{
    private static readonly EvalMode Cook = new(CookMode.Cook.Wire(), "puls-cook", [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]);

    [Test]
    public async Task NoError_FailsOnAnErroredTurn()
    {
        var trial = Trial(Cook, "cook-af", [Start(), Error("overloaded")]);

        var grade = await new CookNoError().GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    [Test]
    public async Task NoError_PassesWhenNoTurnFailed()
    {
        var trial = Trial(Cook, "cook-af", [Start(), Text("Resep.")]);

        Assert.That((await new CookNoError().GradeAsync(trial, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task NoError_IsUnknown_WhenAnyTurnFailedForTheInfrastructure_NotOnlyTheFirst()
    {
        // The follow-up timed out: whether it would have errored is unknown, so the conversation is not a pass. A check of the first turn only
        // would call this one a pass (a mutation of step 5.3 that no test noticed).
        var trial = Trial(Cook, "cook-af", [Start(), Text("Hier is dit vir ses.")]);
        trial = trial with { Transcript = new Transcript([trial.Transcript.Turns[0], trial.Transcript.Turns[1] with { Failure = TrialFailure.Timeout }]) };

        Assert.That((await new CookNoError().GradeAsync(trial, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    private static Trial Trial(EvalMode mode, string task, IReadOnlyList<IReadOnlyList<SessionEvent>> turns) =>
        new("run_1", mode, 1, new EvalScenario(task, new EvalInput(null, "Africa/Johannesburg", ["x"]), default, [], DatasetSplit.Dev),
            new Transcript(turns.Select((events, i) => new TurnTranscript(i + 1, "sess", "q", events, TimeSpan.Zero, null)).ToList()));

    private static IReadOnlyList<SessionEvent> Start() =>
        [SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"puls-cook"}}}"""))];

    private static IReadOnlyList<SessionEvent> Text(string text) =>
        [SessionEvent.From("content_block_delta", Json("{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"" + text + "\"}}"))];

    private static IReadOnlyList<SessionEvent> Error(string message) =>
        [SessionEvent.From("error", Json("{\"type\":\"error\",\"error\":{\"type\":\"api_error\",\"message\":\"" + message + "\"}}"))];

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
