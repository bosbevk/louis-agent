using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Routing;
using LouisAgent.Evals.Puls.Evals.Routing.Scoring;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Routing;

// F18 step 5.3: the routing check as an eval of the harness. Written first: the eval threw NotImplementedException on the red run.
public class RoutingEvalTests
{
    private static readonly RoutingEval Eval = new();

    [Test]
    public void ItIsPulsRoutingEval_UnderTheNameItsDatasetCarries()
    {
        Assert.That((Eval.Name, Eval.Version), Is.EqualTo((PulsEvalName.Routing.Wire(), 2)));
        Assert.That(Eval.Proves, Is.EqualTo(MessageText.Format(PulsEvalMessage.RoutingProves)),
            "the portal's title is a message enum member (ADR 0009)");
    }

    [Test]
    public async Task TheBaselineIsEveryToolsetTheInstanceHasButTheDelegatingOnes_AndTheDelegatedIsPulsAsBuilt()
    {
        var modes = await Eval.ModesAsync(
            new ToolsetsClient(PulsToolset.Web.Wire(), PulsToolset.Files.Wire(), PulsToolset.Specialists.Wire(),
                PulsToolset.Git.Wire(), PulsToolset.Minions.Wire()), CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))), Is.EqualTo(new[]
        {
            ("baseline", Role, "web,files,git"),
            ("delegated", Role, "specialists,web"),
        }));
    }

    [Test]
    public void ItsTwelveDevQuestionsAndSixHeldOutOnesAreTheDatasetsOnes()
    {
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id), Is.EquivalentTo(new[]
        {
            PulsTaskIds.Web, PulsTaskIds.Files, PulsTaskIds.Git, PulsTaskIds.Numbers, PulsTaskIds.Chat,
            PulsTaskIds.Cook, "web-2", "files-2", "git-2", "numbers-2", "chat-2", "cook-2",
        }));
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).Select(s => s.Id), Is.EquivalentTo(new[]
        {
            "web-3", "files-3", "git-3", "numbers-3", "chat-3", "cook-3",
        }));
    }

    [Test]
    public void ItsOneGraderIsTheRequiredRoute_AndNoErrorGraderWouldAddToIt()
    {
        // A route is right only without an error (RoutingScorer), so a no-error grader on a one-turn question repeats it.
        Assert.That(Eval.Graders.Select(g => (g.Name, g.Required, g.Kind)),
            Is.EqualTo(new[] { ("routed-to", true, GraderKind.FixedRule) }));
    }

    [Test]
    public void ItsScoresAreTheRoutingScorers_KeyForKey()
    {
        var trial = Trial(CookEvalMode, "cook", """{"routes_to":"puls-cook"}""", null,
            Turn(1, [Start(PulsEvalRole.Cook.Wire()), Text("Resep.")]));

        var scores = Eval.ScoresOf(trial, []);

        Assert.That(Json(scores), Does.Contain("\"expected_role\":\"puls-cook\"").And.Contain("\"routed_right\":true"));
    }

    [Test]
    public void ABaselineQuestionIsScoredWithoutDelegation()
    {
        var trial = Trial(new EvalMode("baseline", Role, [PulsToolset.Web.Wire()]), "cook",
            """{"routes_to":"puls-cook"}""", null, Turn(1, [Text("Ek kan nie.")]));

        Assert.That(Json(Eval.ScoresOf(trial, [])),
            Does.Contain("\"expected_role\":null").And.Contain("\"routed_right\":true"));
    }

    [Test]
    public void ATurnsFacts_AreWhatTheTurnViewReads()
    {
        var facts = Eval.TurnFactsOf(Turn(1, [Start(PulsEvalRole.Cook.Wire()), Fetch("https://www.news24.com/a")]),
            CookEvalMode);

        Assert.That((facts.Role, facts.AgentCalls, facts.SpecialistCalls, facts.Error),
            Is.EqualTo((PulsEvalRole.Cook.Wire(), 2, 0, (string?)null)));
        Assert.That(facts.Hosts, Is.EqualTo(new[] { "news24.com" }));
    }

    private static string Json(object scores) => JsonSerializer.Serialize(scores, scores.GetType(), EvalJson.SnakeCase);

    /// <summary>A session client that lists the toolsets an instance has, as GET /v1/toolsets does.</summary>
    private sealed class ToolsetsClient(params string[] toolsets) : ISessionClient
    {
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(toolsets);

        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CloseAsync(string session, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
