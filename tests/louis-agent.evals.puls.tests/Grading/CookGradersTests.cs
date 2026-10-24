using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Tests.Grading;

// The cook's fixed-rule graders (F31): the route and the errors, read from a trial's turns. Written first: the graders
// threw NotImplementedException on the red run.
public class CookGradersTests
{
    private static readonly EvalMode Cook = new("puls-cook", "puls-cook", ["specialists", "web"]);
    private static readonly EvalMode Tools = new("puls-tools", "puls-tools", ["web", "shops"]);

    [Test]
    public async Task RoutedRight_PassesWhenTheCookAnswered()
    {
        var trial = Trial(Cook, "cook-af", [Start(), Text("Resep.")]);

        var grade = await new CookRoutedRight().GradeAsync(trial, CancellationToken.None);

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    [Test]
    public async Task RoutedRight_FollowsTheSpecialistsToolset_NotTheModesName()
    {
        // A cook mode with another name (a puls-cook-sonnet run) is on the cook all the same.
        var trial = Trial(new EvalMode("puls-cook-sonnet", "puls-cook", ["specialists", "web"]), "cook-af", [Start(), Text("Resep.")]);

        Assert.That((await new CookRoutedRight().GradeAsync(trial, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedRight_FailsWhenPulsAnsweredItself()
    {
        var trial = Trial(Cook, "cook-af", [Text("Resep.")]);

        var grade = await new CookRoutedRight().GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    [Test]
    public async Task RoutedRight_IsNotCookedForWhereTheCookIsNotOn()
    {
        // Puls with its own tools answers itself: that's right in its mode.
        var trial = Trial(Tools, "control-web", [Text("Die koers is 10.5%.")]);

        var grade = await new CookRoutedRight().GradeAsync(trial, CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

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

    private static Trial Trial(EvalMode mode, string task, IReadOnlyList<IReadOnlyList<SessionEvent>> turns) =>
        new("run_1", mode, 1, new EvalScenario(task, new EvalInput(null, "Africa/Johannesburg", ["x"]), default),
            new Transcript(turns.Select((events, i) => new TurnTranscript(i + 1, "sess", "q", events, TimeSpan.Zero, null)).ToList()));

    private static IReadOnlyList<SessionEvent> Start() =>
        [SessionEvent.From("content_block_start", Json("""{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"t1","name":"StartSubSession","input":{"role":"puls-cook"}}}"""))];

    private static IReadOnlyList<SessionEvent> Text(string text) =>
        [SessionEvent.From("content_block_delta", Json("{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"" + text + "\"}}"))];

    private static IReadOnlyList<SessionEvent> Error(string message) =>
        [SessionEvent.From("error", Json("{\"type\":\"error\",\"error\":{\"type\":\"api_error\",\"message\":\"" + message + "\"}}"))];

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
