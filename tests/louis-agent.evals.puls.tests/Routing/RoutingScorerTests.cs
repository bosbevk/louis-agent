using LouisAgent.Evals;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Grading.FixedRules;
using LouisAgent.Evals.Puls.Routing;
using LouisAgent.Evals.Puls.Transcripts;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Puls.Tests.Routing;

// The Python's test_routing.py (F31 step 8): does Puls hand each kind of question to the right place? Only cooking has a role
// that's on (F29), so every other question is right when Puls answers it itself. Each expected value is the Python's.
public class RoutingScorerTests
{
    private static readonly ToolCall Start = new("StartSubSession", Json("{}"));

    [Test]
    public void TheScoresDocumentKeys_AreThePythonsKeys()
    {
        // A renamed property would rename the key in the scores document: this holds the Python's 9 names.
        var element = System.Text.Json.JsonSerializer.SerializeToElement<object>(RoutingScorer.Score("cook", Turn(), delegating: true), EvalScores.Json);
        Assert.That(element.EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "role", "expected_role", "routed_right", "answered_itself", "delegations", "puls_calls", "specialist_calls", "hosts", "errors" }));
    }

    [Test]
    public void TheExpectedRole_IsTheCookForAMealAndNobodyOtherwise()
    {
        Assert.That(RoutingScorer.ExpectedRole("cook-2"), Is.EqualTo("puls-cook"));
        foreach (string task in new[] { "web", "files-2", "git", "numbers", "chat-2" })
            Assert.That(RoutingScorer.ExpectedRole(task), Is.Null, task);
    }

    [Test]
    public void EveryQuestion_HasAKindTheCheckKnows()
    {
        Assert.That(RoutingSetup.Tasks, Has.Count.EqualTo(12));
        foreach (string task in RoutingSetup.Tasks)
            Assert.That(RoutingSetup.Kinds.ContainsKey(task.Split('-')[0]), Is.True, task);
    }

    [Test]
    public void AMealHandedToTheCook_IsRight()
    {
        var scores = RoutingScorer.Score("cook", Turn(role: "puls-cook", calls: [Start]), delegating: true);

        Assert.That(scores.RoutedRight, Is.True);
        Assert.That(scores.Delegations, Is.EqualTo(1));
    }

    [Test]
    public void AMealPulsCookedItself_IsWrong() =>
        Assert.That(RoutingScorer.Score("cook", Turn(), delegating: true).RoutedRight, Is.False);

    [Test]
    public void AWebQuestionPulsAnsweredItself_IsRight() =>
        Assert.That(RoutingScorer.Score("web", Turn(), delegating: true).RoutedRight, Is.True);

    [Test]
    public void AWebQuestionHandedAway_IsWrong() =>
        Assert.That(RoutingScorer.Score("web-2", Turn(role: "puls-cook"), delegating: true).RoutedRight, Is.False);

    [Test]
    public void WithoutDelegation_AnsweringItselfIsAlwaysRight() =>
        Assert.That(RoutingScorer.Score("cook", Turn(), delegating: false).RoutedRight, Is.True);

    [Test]
    public void TheBaseline_IsEveryAvailableToolsetButTheDelegatingOnes() =>
        Assert.That(RoutingScorer.Baseline(["files", "git", "web", "specialists", "minions", "skills"]),
            Is.EqualTo(new[] { "files", "git", "web", "skills" }));

    [Test]
    public void AnError_IsNeverRight()
    {
        var scores = RoutingScorer.Score("chat", Turn(error: "overloaded"), delegating: false);

        Assert.That(scores.RoutedRight, Is.False);
        Assert.That(scores.Errors, Is.EqualTo(1));
    }

    [Test]
    public void Delegating_IsTheSpecialistsToolset_NotMinions()
    {
        // The Python's rule is "specialists" in the mode's toolsets (routing/run_check.py); a minions-only mode can't hand on.
        Assert.That(RoutingScorer.Delegating(new EvalMode("minions-only", "puls-cook", ["minions", "web"])), Is.False);
        Assert.That(RoutingScorer.Delegating(new EvalMode("puls-as-built", "puls-cook", ["specialists", "web"])), Is.True);
    }

    [Test]
    public void TheDelegatedMode_IsPulsAsBuilt() =>
        Assert.That(RoutingSetup.Delegated, Is.EqualTo(new[] { "specialists", "web" }));

    private static TurnView Turn(string? role = null, ToolCall[]? calls = null, string? error = null) =>
        new("Antwoord.", calls ?? [], [], role, null, error, [], []);

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
