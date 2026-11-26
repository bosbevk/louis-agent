using LouisAgent.Composition.Agents;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Pages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runs;
using LouisAgent.Evals.Sessions;
using Microsoft.Extensions.AI;
using static LouisAgent.Evals.Tests.Runs.FakeSessionsApi;

namespace LouisAgent.Evals.Tests.Runs;

/// <summary>
/// One conversation, played once: a session opened as the file's agent, each ask sent in order, the session closed
/// whatever happens, and then every turn graded, its facts by code and its criteria by the judge.
/// </summary>
public class ConversationPlayerTests
{
    private const string FirstAsk = "What is louis-agent?";
    private const string SecondAsk = "Where does it say so?";

    private static readonly AgentAddress Guide = new("louis-agent-guide", "window");
    private static readonly RunTrial Trial = new("20261123-043012-guide", "guide-what-is", 1);

    /// <summary>The guide's conversation of two turns, each with a fact and a criterion.</summary>
    private static readonly Conversation TwoTurns = new("guide-what-is",
    [
        new Turn(FirstAsk)
        {
            Facts = [Fact.Parse("no-error")],
            Criteria = ["Says it is a framework"]
        },
        new Turn(SecondAsk)
        {
            Facts = [Fact.Parse("no-error")],
            Criteria = ["Names the doc"]
        }
    ]);

    /// <summary>The session opens once, as the address's identity and its role.</summary>
    [Test]
    public async Task ATwoTurnConversation_OpensOneSession_AsTheFilesAgent()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("The overview."));

        await Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial);

        string opened = api.Opened().Single();
        Assert.That(opened, Does.Contain("\"identity\":\"louis-agent-guide\"").And.Contain("\"role\":\"window\""));
    }

    /// <summary>Both asks go to that session in the file's order, and then it is closed.</summary>
    [Test]
    public async Task BothAsks_AreSentInOrder_AndThenTheSessionIsClosed()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("The overview."));

        await Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial);

        Assert.That(api.Calls(),
            Is.EqualTo(new[]
            {
                "POST /v1/sessions", "POST /v1/sessions/s-1/messages", "POST /v1/sessions/s-1/messages",
                "DELETE /v1/sessions/s-1"
            }));
        Assert.That(api.Asked(), Is.EqualTo(new[] { FirstAsk, SecondAsk }));
    }

    /// <summary>Each turn comes back with what happened, a mark for each fact and a mark for each criterion.</summary>
    [Test]
    public async Task EachTurn_ComesBackWithItsTranscript_AndAMarkPerFactAndPerCriterion()
    {
        var api = new FakeSessionsApi(Answered("A framework.", 0.002m), Answered("The overview."));

        var repeat = await Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial);

        Assert.That(repeat.Number, Is.EqualTo(1));
        Assert.That(repeat.Turns, Has.Count.EqualTo(2));
        var first = repeat.Turns[0];
        Assert.That(first.Transcript.Ask, Is.EqualTo(FirstAsk));
        Assert.That(first.Transcript.Answer, Is.EqualTo("A framework."));
        Assert.That(first.Facts.Select(mark => (mark.Text, mark.Met)), Is.EqualTo(new[] { ("no-error", true) }));
        Assert.That(first.Criteria.Select(mark => (mark.Text, mark.Met)),
            Is.EqualTo(new[] { ("Says it is a framework", true) }));
        Assert.That((first.Cost, first.Currency), Is.EqualTo(((decimal?)0.002m, "USD")));
    }

    /// <summary>The judge reads the turns up to and including the one it marks, never a later one.</summary>
    [Test]
    public async Task TheJudge_IsGivenTheTurnsUpToTheOneItMarks()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("The overview."));
        var judgeModel = Met();

        await Player(api, judgeModel).PlayAsync(Guide, TwoTurns, Trial);

        string firstAsked = judgeModel.Requests[0].Messages[^1].Text;
        string secondAsked = judgeModel.Requests[1].Messages[^1].Text;
        Assert.That(firstAsked, Does.Contain(FirstAsk).And.Not.Contain(SecondAsk));
        Assert.That(secondAsked, Does.Contain(FirstAsk).And.Contain(SecondAsk));
    }

    /// <summary>A turn the API refuses stops the play, and the session is still closed.</summary>
    [Test]
    public void TheSession_IsClosed_WhenATurnThrows()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Refused);

        Assert.ThrowsAsync<SessionRefusedException>(() => Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial));

        Assert.That(api.Calls()[^1], Is.EqualTo("DELETE /v1/sessions/s-1"));
    }

    /// <summary>The agent's own error is a turn like any other: it gets its marks, and the next one is asked.</summary>
    [Test]
    public async Task ATurnThatEndedInTheAgentsError_StillGetsItsMarks_AndTheNextIsAsked()
    {
        var api = new FakeSessionsApi(Failed("The model is overloaded."), Answered("The overview."));

        var repeat = await Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial);

        var failed = repeat.Turns[0];
        Assert.That(failed.Transcript.Error, Is.EqualTo("The model is overloaded."));
        Assert.That(failed.Facts.Single().Met, Is.False);
        Assert.That(failed.Criteria, Has.Count.EqualTo(1));
        Assert.That(api.Asked(), Is.EqualTo(new[] { FirstAsk, SecondAsk }));
    }

    /// <summary>A repeat passed when every mark of every turn is met.</summary>
    [Test]
    public async Task ARepeat_PassedWhenEveryMarkIsMet()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("The overview."));

        var repeat = await Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial);

        Assert.That(repeat.Passed, Is.True);
    }

    /// <summary>One mark not met, here the judge's last, and the repeat hasn't passed.</summary>
    [Test]
    public async Task ARepeat_HasNotPassed_WhenOneMarkIsNotMet()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("The overview."));
        var judgeModel = new FakeChatClient(_ => Judged(true), _ => Judged(false));

        var repeat = await Player(api, judgeModel).PlayAsync(Guide, TwoTurns, Trial);

        Assert.That(repeat.Turns[1].Criteria.Single().Met, Is.False);
        Assert.That(repeat.Passed, Is.False);
    }

    /// <summary>
    /// F126.3.3: a conversation that names a page asks for it once, and sends it with its first ask and no other, as
    /// the widget does for a visitor who stays on one page.
    /// </summary>
    [Test]
    public async Task APageNamed_IsAskedOnce_AndSentWithTheFirstAskOnly()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));

        await Player(api, Met()).PlayAsync(Guide, TwoTurns with { Page = "/" }, Trial);

        Assert.That(api.PagesAsked(), Is.EqualTo(new[] { "/" }));
        Assert.That(api.PagesSent(), Is.EqualTo(new[] { "/", null }));
    }

    /// <summary>
    /// F126.5.1: a conversation that gives its page in the file sends it with its first ask, its address, title and
    /// text, and asks the site's page route for nothing, since the file holds the page.
    /// </summary>
    [Test]
    public async Task APageGiven_IsSentWithTheFirstAsk_AndNoPageIsAsked()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));
        var given = new GivenPage("/pricing", "What it costs.", "Pricing");

        await Player(api, Met()).PlayAsync(Guide, TwoTurns with { GivenPage = given }, Trial);

        Assert.That(api.PagesAsked(), Is.Empty);
        Assert.That(api.PagesSent(), Is.EqualTo(new[] { "/pricing", null }));
    }

    /// <summary>A conversation that names no page asks for none and sends none.</summary>
    [Test]
    public async Task NoPageNamed_AsksForNone_AndSendsNone()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));

        await Player(api, Met()).PlayAsync(Guide, TwoTurns, Trial);

        Assert.That(api.PagesAsked(), Is.Empty);
        Assert.That(api.PagesSent(), Is.EqualTo(new string?[] { null, null }));
    }

    /// <summary>F126.3.4: a turn's judge cost is its criteria's costs added, the ledger's price of each call.</summary>
    [Test]
    public async Task ATurnsJudgeCost_IsItsCriteriasCostsAdded()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));
        var ledger = new ListUsageSink();
        using var judgeModel = new UsageRecordingChatClient(PricedMet(), new PricingUsageSink(ledger, Prices), "judge");

        var repeat = await new ConversationPlayer(api.Client(), api.Pages(), new Judge(judgeModel))
            .PlayAsync(Guide, TwoTurns, Trial);

        var costs = ledger.Records.Select(record => record.Cost!.Amount).ToList();
        Assert.That(repeat.Turns.Select(turn => turn.JudgeCost), Is.EqualTo(new decimal?[] { costs[0], costs[1] }));
        Assert.That(costs, Has.All.GreaterThan(0m));
    }

    /// <summary>
    /// F126.3.4, step 8's finding: the judge's calls are recorded under the play's run and task, as the agent's are,
    /// so the ledger ties what judging a run cost to that run.
    /// </summary>
    /// <summary>An ask that writes {trial} is sent with the trial's own id in its place (FX257).</summary>
    [Test]
    public async Task AnAskThatWritesTheTrial_IsSentWithItsId()
    {
        var api = new FakeSessionsApi(Answered("Looked into it."));
        var oneCase = new Conversation("triage-refund", [new Turn("Run id: eval-refund-{trial}.")]);

        await Player(api, Met()).PlayAsync(Guide, oneCase, Trial);

        Assert.That(api.Asked(), Is.EqualTo(new[] { "Run id: eval-refund-20261123-043012-guide-1." }));
    }

    [Test]
    public async Task TheJudgesCalls_AreRecordedUnderThePlaysRunAndTask()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));
        var ledger = new ListUsageSink();
        using var judgeModel = new UsageRecordingChatClient(PricedMet(), ledger, "judge");

        await new ConversationPlayer(api.Client(), api.Pages(), new Judge(judgeModel))
            .PlayAsync(Guide, TwoTurns, Trial);

        Assert.That(ledger.Records.Select(record => (record.Run, record.Task)),
            Has.All.EqualTo((Trial.Run, Trial.Task)));
    }

    /// <summary>A turn judged with no ledger has no known judge cost, nor does a turn with no criteria.</summary>
    [Test]
    public async Task ATurnsJudgeCost_IsNoneWhenNotKnown_OrWithNoCriteria()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));
        var noCriteria = TwoTurns with { Turns = [TwoTurns.Turns[0], TwoTurns.Turns[1] with { Criteria = [] }] };

        var repeat = await Player(api, Met()).PlayAsync(Guide, noCriteria, Trial);

        Assert.That(repeat.Turns.Select(turn => turn.JudgeCost), Is.EqualTo(new decimal?[] { null, null }));
    }

    /// <summary>
    /// F126.3.4: a play stopped once its session is open still closes the session, with no token, so a stopped run
    /// leaves none open.
    /// </summary>
    [Test]
    public async Task APlayStopped_AfterItsSessionOpens_StillClosesIt()
    {
        var api = new FakeSessionsApi(Answered("A framework."), Answered("Yes."));
        using var stop = new CancellationTokenSource();
        var http = new HttpClient(new StopAfterOpening(api.Handler, stop))
        {
            BaseAddress = new Uri("http://api.test/")
        };
        var player = new ConversationPlayer(new SessionClient(http), api.Pages(), new Judge(Met()));

        Assert.CatchAsync<OperationCanceledException>(() => player.PlayAsync(Guide, TwoTurns, Trial, stop.Token));

        Assert.That(api.Calls(), Has.Some.StartsWith("DELETE"));
        await Task.CompletedTask;
    }

    /// <summary>Stops the play as soon as the API has opened its session.</summary>
    private sealed class StopAfterOpening(HttpMessageHandler inner, CancellationTokenSource stop)
        : DelegatingHandler(inner)
    {
        /// <summary>Passes each request on, and cancels the play once a session has opened.</summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/v1/sessions")
                await stop.CancelAsync();
            return response;
        }
    }

    /// <summary>A price table for the judge's model, so its calls are priced.</summary>
    private static readonly PriceTable Prices = new()
    {
        Models = new Dictionary<string, ModelPrice>
        {
            ["judge"] = new()
            {
                Input = 1m,
                Output = 5m,
                CacheRead = 0.1m,
                CacheWrite5m = 1.25m,
                CacheWrite1h = 2m
            }
        }
    };

    /// <summary>A judge's model that marks every criterion met, each answer with its usage.</summary>
    private static FakeChatClient PricedMet() => new(_ => PricedJudged(), _ => PricedJudged(), _ => PricedJudged());

    /// <summary>The judge's answer met, with the tokens it used.</summary>
    private static ChatResponse PricedJudged() =>
        new(new ChatMessage(ChatRole.Assistant, """{"reason":"Read the answer.","met":true}"""))
        {
            ModelId = "judge",
            Usage = new UsageDetails
            {
                InputTokenCount = 1000,
                OutputTokenCount = 100
            }
        };

    /// <summary>The player over this API and its page route, with a judge on this model.</summary>
    private static ConversationPlayer Player(FakeSessionsApi api, FakeChatClient judgeModel) =>
        new(api.Client(), api.Pages(), new Judge(judgeModel));

    /// <summary>A judge's model that marks every criterion it is asked about met.</summary>
    private static FakeChatClient Met() => new(_ => Judged(true), _ => Judged(true), _ => Judged(true));

    /// <summary>The judge's answer, as the model gives it.</summary>
    private static ChatResponse Judged(bool met) =>
        new(new ChatMessage(ChatRole.Assistant,
            $$"""{"reason":"Read the answer.","met":{{(met ? "true" : "false")}}}"""));

    /// <summary>A live play fills an ask's {trial} with its run's id and its repeat (FX257).</summary>
    [Test]
    public void ALivePlay_FillsItsOwnId() =>
        Assert.That(new RunTrial("20261126-211755-triage", "triage-refund", 2).Fill("Run id: eval-refund-{trial}."),
            Is.EqualTo("Run id: eval-refund-20261126-211755-triage-2."));

    /// <summary>A replay fills the id of the live play it replays, so it sends the words that were recorded.</summary>
    [Test]
    public void AReplay_FillsTheIdOfThePlayItReplays() =>
        Assert.That(
            new RunTrial("20261127-080000-triage", "triage-refund", 2, "20261126-211755-triage")
                .Fill("Run id: eval-refund-{trial}."),
            Is.EqualTo("Run id: eval-refund-20261126-211755-triage-2."));

    /// <summary>An ask that writes no mark is sent as it is written.</summary>
    [Test]
    public void AnAskWithNoMark_IsSentAsWritten() =>
        Assert.That(new RunTrial("20261126-211755-guide", "guide-what-is", 1).Fill("What is louis-agent?"),
            Is.EqualTo("What is louis-agent?"));
}
