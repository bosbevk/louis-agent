using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Agent.Turns;

/// <summary>
/// An instance can show a specialist's report as it is, instead of asking its model to write it out again. Retyping
/// cost a whole request at the output rate, and a copy can only get worse: the report is shown, whole, with the turn
/// ended there.
/// </summary>
public class ShowTheReportTests
{
    private const string Report = "Fixed the null check; 14/14 tests pass.";

    /// <summary>A model that hands the task on, and would retype the report if it were asked again.</summary>
    private static FakeChatClient HandsOn() => new(
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextContent("Handing this on."),
            new FunctionCallContent("call_1", "StartSubSession", new Dictionary<string, object?>
            {
                ["toolsets"] = new[] { TestToolset.Web.Wire() },
                ["message"] = "Do the task."
            })
        ])),
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Retyped: " + Report)));

    /// <summary>The engine over specialist tools on a stub peer, showing the reports or not.</summary>
    private static AgentEngine Engine(IChatClient client, StubHandler peer, bool show) =>
        new(new CompositeSkillProvider(), client, TestPaths.Agent(),
            toolsets: [new SpecialistTools(Peers, "the-key", new HttpClient(peer), showReports: show)]);

    /// <summary>All of a turn's text as the host would receive it.</summary>
    private static async Task<(string Text, List<ChatMessage> History)> StreamAsync(AgentEngine engine)
    {
        var history = engine.NewHistory();
        var text = new System.Text.StringBuilder();
        await foreach (var update in engine.StreamPromptAsync(history, "Do it")) text.Append(update.Text);
        return (text.ToString(), history);
    }

    /// <summary>The model isn't asked again, and the user gets the report as the specialist wrote it.</summary>
    [Test]
    public async Task TheReport_IsShownAsItIs_AndTheModelIsNotAskedAgain()
    {
        var client = HandsOn();

        var (text, _) = await StreamAsync(Engine(client, Peer(), show: true));

        Assert.That(client.Requests, Has.Count.EqualTo(1), "no request to retype it");
        Assert.That(text, Is.EqualTo("Handing this on.\n\n" + Report));
    }

    /// <summary>The footer is for the model, and never reaches the user.</summary>
    [Test]
    public async Task TheFooter_IsNotShown()
    {
        var (text, _) = await StreamAsync(Engine(HandsOn(), Peer(), show: true));

        Assert.That(text, Does.Not.Contain("[Sub-session"));
    }

    /// <summary>The report is what the assistant said, so a follow-up has a whole conversation to read.</summary>
    [Test]
    public async Task TheHistory_EndsWithTheReport_AsTheAssistantsAnswer()
    {
        var (_, history) = await StreamAsync(Engine(HandsOn(), Peer(), show: true));

        Assert.That(history[^1].Role, Is.EqualTo(ChatRole.Assistant));
        Assert.That(history[^1].Text.Trim(), Is.EqualTo(Report));
    }

    /// <summary>A specialist that failed gives the model its reason, and the model says what to do about it.</summary>
    [Test]
    public async Task AFailedSpecialist_GoesBackToTheModel()
    {
        var client = HandsOn();

        await StreamAsync(Engine(client, Peer(Replies(FailedStream)), show: true));

        Assert.That(client.Requests, Has.Count.EqualTo(2));
    }

    /// <summary>Without the setting nothing changes: the model reads the report and writes the answer.</summary>
    [Test]
    public async Task WithoutTheSetting_TheModelWritesTheAnswer()
    {
        var client = HandsOn();

        var (text, _) = await StreamAsync(Engine(client, Peer(), show: false));

        Assert.That(client.Requests, Has.Count.EqualTo(2));
        Assert.That(text, Does.Contain("Retyped: "));
    }

    /// <summary>The non-streaming path shows it too.</summary>
    [Test]
    public async Task TheAnswerNotStreamed_IsTheReportToo()
    {
        var engine = Engine(HandsOn(), Peer(), show: true);

        string answer = await engine.ProcessPromptAsync(engine.NewHistory(), "Do it");

        Assert.That(answer, Does.EndWith(Report));
    }
}
