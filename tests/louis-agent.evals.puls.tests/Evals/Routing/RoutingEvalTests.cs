using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Routing;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Routing;

/// <summary>
/// The routing check as an eval of the harness: its name, its two modes, its questions, its one grader and what it
/// says of a turn. Written first, in F18 step 5.3, when the eval still threw.
/// </summary>
public class RoutingEvalTests
{
    /// <summary>The eval under test.</summary>
    private static readonly RoutingEval Eval = new();

    /// <summary>It runs under its dataset's name, and its portal title is a message.</summary>
    [Test]
    public void ItIsPulsRoutingEval_UnderTheNameItsDatasetCarries()
    {
        Assert.That((Eval.Name, Eval.Version), Is.EqualTo((PulsEvalName.Routing.Wire(), 2)));
        Assert.That(Eval.Proves, Is.EqualTo(MessageText.Format(PulsEvalMessage.RoutingProves)),
            "the portal's title is a message enum member (ADR 0009)");
    }

    /// <summary>
    /// The baseline gets every toolset the instance has except the ones that hand off; the delegated mode is Puls as
    /// it's built.
    /// </summary>
    [Test]
    public async Task TheBaselineIsEveryToolsetTheInstanceHasButTheDelegatingOnes_AndTheDelegatedIsPulsAsBuilt()
    {
        var modes = await Eval.ModesAsync(
            new ToolsetsClient(PulsToolset.Web.Wire(), PulsToolset.Files.Wire(), PulsToolset.Specialists.Wire(),
                PulsToolset.Git.Wire(), PulsToolset.Minions.Wire()), CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))),
            Is.EqualTo(new[] { ("baseline", Role, "web,files,git"), ("delegated", Role, "specialists,web"), }));
    }

    /// <summary>Its questions are the dataset's: twelve to work on and six held out.</summary>
    [Test]
    public void ItsTwelveDevQuestionsAndSixHeldOutOnesAreTheDatasetsOnes()
    {
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id),
            Is.EquivalentTo(new[]
            {
                PulsTaskIds.Web, PulsTaskIds.Files, PulsTaskIds.Git, PulsTaskIds.Numbers, PulsTaskIds.Chat,
                PulsTaskIds.Cook, "web-2", "files-2", "git-2", "numbers-2", "chat-2", "cook-2",
            }));
        Assert.That(Eval.Scenarios.Where(s => s.Split == DatasetSplit.HeldOut).Select(s => s.Id),
            Is.EquivalentTo(new[] { "web-3", "files-3", "git-3", "numbers-3", "chat-3", "cook-3", }));
    }

    /// <summary>Its one grader is the route, which must hold.</summary>
    [Test]
    public void ItsOneGraderIsTheRequiredRoute_AndNoErrorGraderWouldAddToIt()
    {
        // A route is right only without an error, so a no-error grader on a one-turn question would only repeat it.
        Assert.That(Eval.Graders.Select(g => (g.Name, g.Required, g.Kind)),
            Is.EqualTo(new[] { ("routed-to", true, GraderKind.FixedRule) }));
    }

    /// <summary>What it says of a turn is what Puls's turn view reads: who answered, the calls, the sites.</summary>
    [Test]
    public void ATurnsFacts_AreWhatTheTurnViewReads()
    {
        var facts = Eval.TurnFactsOf(Turn(1, [Start(PulsEvalRole.Cook.Wire()), Fetch("https://www.news24.com/a")]),
            CookEvalMode);

        Assert.That((facts.Role, facts.AgentCalls, facts.SpecialistCalls, facts.Error),
            Is.EqualTo((PulsEvalRole.Cook.Wire(), 2, 0, (string?)null)));
        Assert.That(facts.Hosts, Is.EqualTo(new[] { "news24.com" }));
    }

    /// <summary>A session client that lists the toolsets an instance has, as GET /v1/toolsets does.</summary>
    private sealed class ToolsetsClient(params string[] toolsets) : ISessionClient
    {
        /// <summary>The toolsets it was given.</summary>
        public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(toolsets);

        /// <summary>Never called: listing the modes opens no session.</summary>
        public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        /// <summary>Never called: listing the modes sends nothing.</summary>
        public IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <summary>Never called: there is no session to close.</summary>
        public Task CloseAsync(string session, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
