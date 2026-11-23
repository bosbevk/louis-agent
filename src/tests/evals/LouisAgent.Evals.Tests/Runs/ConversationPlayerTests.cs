using LouisAgent.Composition.Agents;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
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

    /// <summary>The player over this API, with a judge on this model.</summary>
    private static ConversationPlayer Player(FakeSessionsApi api, FakeChatClient judgeModel) =>
        new(api.Client(), new Judge(judgeModel));

    /// <summary>A judge's model that marks every criterion it is asked about met.</summary>
    private static FakeChatClient Met() => new(_ => Judged(true), _ => Judged(true), _ => Judged(true));

    /// <summary>The judge's answer, as the model gives it.</summary>
    private static ChatResponse Judged(bool met) =>
        new(new ChatMessage(ChatRole.Assistant,
            $$"""{"reason":"Read the answer.","met":{{(met ? "true" : "false")}}}"""));
}
