using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Routing;
using LouisAgent.Evals.Puls.Evals.Routing.Scoring;
using LouisAgent.Evals.Puls.Shared.Turns;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests.Evals.Routing.Scoring;

// The Python's test_routing.py (F31 step 8): does Puls hand each kind of question to the right place? Only cooking has a role
// that's on (F29), so every other question is right when Puls answers it itself. Each expected value is the Python's.
public class RoutingScorerTests
{
    private static readonly ToolCall Start = new("StartSubSession", Json("{}"));

    [Test]
    public void TheScoresDocumentKeys_AreThePythonsKeys()
    {
        // A renamed property would rename the key in the scores document: this holds the Python's 9 names.
        var element = System.Text.Json.JsonSerializer.SerializeToElement<object>(RoutingScorer.Score(Dev.Route("cook"), Turn(), delegating: true), EvalScores.Json);
        Assert.That(element.EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "role", "expected_role", "routed_right", "answered_itself", "delegations", "puls_calls", "specialist_calls", "hosts", "errors" }));
    }

    [Test]
    public void TheExpectedRole_IsTheScenariosRoute_TheCookForAMealAndNobodyOtherwise()
    {
        // F18 step 5.4: from the dataset's routes_to, no longer from the question's name.
        Assert.That(Dev.Route("cook-2"), Is.EqualTo(PulsEvalRole.Cook.Wire()));
        foreach (string task in new[] { "web", "files-2", "git", "numbers", "chat-2" })
            Assert.That(Dev.Route(task), Is.Null, task);
    }

    [Test]
    public void AMealHandedToTheCook_IsRight()
    {
        var scores = RoutingScorer.Score(Dev.Route("cook"), Turn(role: PulsEvalRole.Cook.Wire(), calls: [Start]), delegating: true);

        Assert.That(scores.RoutedRight, Is.True);
        Assert.That(scores.Delegations, Is.EqualTo(1));
    }

    [Test]
    public void AMealPulsCookedItself_IsWrong() =>
        Assert.That(RoutingScorer.Score(Dev.Route("cook"), Turn(), delegating: true).RoutedRight, Is.False);

    [Test]
    public void AWebQuestionPulsAnsweredItself_IsRight() =>
        Assert.That(RoutingScorer.Score(Dev.Route("web"), Turn(), delegating: true).RoutedRight, Is.True);

    [Test]
    public void AWebQuestionHandedAway_IsWrong() =>
        Assert.That(RoutingScorer.Score(Dev.Route("web-2"), Turn(role: PulsEvalRole.Cook.Wire()), delegating: true).RoutedRight, Is.False);

    [Test]
    public void WithoutDelegation_AnsweringItselfIsAlwaysRight() =>
        Assert.That(RoutingScorer.Score(Dev.Route("cook"), Turn(), delegating: false).RoutedRight, Is.True);

    [Test]
    public void TheBaseline_IsEveryAvailableToolsetButTheDelegatingOnes() =>
        Assert.That(RoutingScorer.Baseline([PulsToolset.Files.Wire(), PulsToolset.Git.Wire(), PulsToolset.Web.Wire(), PulsToolset.Specialists.Wire(), PulsToolset.Minions.Wire(), PulsToolset.Skills.Wire()]),
            Is.EqualTo(new[] { PulsToolset.Files.Wire(), PulsToolset.Git.Wire(), PulsToolset.Web.Wire(), PulsToolset.Skills.Wire() }));

    [Test]
    public void AnError_IsNeverRight()
    {
        var scores = RoutingScorer.Score(Dev.Route("chat"), Turn(error: "overloaded"), delegating: false);

        Assert.That(scores.RoutedRight, Is.False);
        Assert.That(scores.Errors, Is.EqualTo(1));
    }

    [Test]
    public void Delegating_IsTheSpecialistsToolset_NotMinions()
    {
        // The Python's rule is "specialists" in the mode's toolsets (routing/run_check.py); a minions-only mode can't hand on.
        Assert.That(PulsModes.CanDelegate(new EvalMode("minions-only", PulsEvalRole.Cook.Wire(), [PulsToolset.Minions.Wire(), PulsToolset.Web.Wire()])), Is.False);
        Assert.That(PulsModes.CanDelegate(new EvalMode("puls-as-built", PulsEvalRole.Cook.Wire(), [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()])), Is.True);
    }

    [Test]
    public void TheDelegatedMode_IsPulsAsBuilt() =>
        Assert.That(RoutingSetup.Delegated, Is.EqualTo(new[] { PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire() }));

    private static TurnView Turn(string? role = null, ToolCall[]? calls = null, string? error = null) =>
        new("Antwoord.", calls ?? [], [], role, null, error, [], []);

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
