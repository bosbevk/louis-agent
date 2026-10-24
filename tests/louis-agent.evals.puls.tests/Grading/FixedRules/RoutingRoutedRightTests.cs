using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Puls.Routing;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Puls.Tests.Grading.FixedRules;

// The routing's fixed-rule grader (F29, F31): the route of each kind of question, read from the first turn. Split out of
// RoutingScorerTests, which holds the score.
public class RoutingRoutedRightTests
{
    [Test]
    public async Task TheRoutedRightGrader_PassesAMealToTheCookInTheDelegatedMode()
    {
        var mode = new EvalMode("puls-delegated", "puls/voice-af", RoutingSetup.Delegated);
        var events = new[]
        {
            SessionEvent.From("content_block_start", Json("{\"type\":\"content_block_start\",\"index\":0,\"content_block\":{\"type\":\"tool_use\",\"id\":\"t1\",\"name\":\"StartSubSession\",\"input\":{\"role\":\"puls-cook\"}}}")),
        };
        var trial = Trial(mode, "cook", [events]);

        Assert.That((await new RoutingRoutedRight().GradeAsync(trial, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    [Test]
    public async Task RoutedRight_SaysItFailedWhenTheTurnErrored()
    {
        var mode = new EvalMode("puls-as-built", "puls/voice-af", RoutingSetup.Delegated);
        var events = new[] { SessionEvent.From("error", Json("{\"type\":\"error\",\"error\":{\"type\":\"api_error\",\"message\":\"overloaded\"}}")) };

        var grade = await new RoutingRoutedRight().GradeAsync(Trial(mode, "web", [events]), CancellationToken.None);

        Assert.That(grade.Reason, Does.Contain("failed"));
    }

    private static Trial Trial(EvalMode mode, string task, IReadOnlyList<IReadOnlyList<SessionEvent>> turns) =>
        new("run_1", mode, 1, new EvalScenario(task, new EvalInput(null, "Africa/Johannesburg", ["x"]), default, [], DatasetSplit.Dev),
            new Transcript(turns.Select((events, i) => new TurnTranscript(i + 1, "sess", "q", events, TimeSpan.Zero, null)).ToList()));

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
