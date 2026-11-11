using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Agent.Turns;

/// <summary>
/// An identity can have the answer of an agent it asks shown as it is (F127.3.1, Louis's pick A), the way an instance
/// showed a specialist's report (ADR 0079): whole, the line under it left off, the turn ended there. One that doesn't,
/// such as a guide that speaks for itself, keeps the last word.
/// </summary>
public class ShowTheAskedAnswerTests
{
    private const string Answer = "Fixed the null check; 14/14 tests pass.";

    /// <summary>A model that asks testprod, and would retype the answer if it were asked again.</summary>
    private static FakeChatClient Asks() => new(
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextContent("Asking testprod."),
            new FunctionCallContent("call_1", nameof(AskAgentTools.AskAgent),
                new Dictionary<string, object?>
                {
                    ["address"] = "testprod",
                    ["message"] = "Do the task."
                })
        ])),
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Retyped: " + Answer)));

    /// <summary>A turn of an engine with the ask tools on a stub peer, as the asker given; all its text.</summary>
    private static async Task<string> TurnAsync(IChatClient client, bool relays)
    {
        var asker = new FakeIdentity("asker", calls: ["testprod"], relaysAnswersWhole: relays);
        IReadOnlyList<IAgentIdentity> identities = [asker, new FakeIdentity("testprod", voice: "testprod/voice-en")];
        var engine = new AgentEngine(new CompositeSkillProvider(), client, TestPaths.Agent(),
            toolsets: [new AskAgentTools(Peers, identities, "the-key", new HttpClient(Peer()))]);

        using var _ = TurnIdentity.Use(TurnIdentity.Of(asker));
        var text = new System.Text.StringBuilder();
        await foreach (var update in engine.StreamPromptAsync(engine.NewHistory(), "Do it")) text.Append(update.Text);
        return text.ToString();
    }

    /// <summary>
    /// An asker that relays answers whole shows the answer as the agent wrote it, without the line under it, and its
    /// model isn't asked again.
    /// </summary>
    [Test]
    public async Task AnAskerThatRelays_ShowsTheAnswerWhole_AndItsModelIsNotAskedAgain()
    {
        var client = Asks();

        string text = await TurnAsync(client, relays: true);

        Assert.Multiple(() =>
        {
            Assert.That(client.Requests, Has.Count.EqualTo(1), "no request to retype it");
            Assert.That(text, Is.EqualTo("Asking testprod.\n\n" + Answer));
        });
    }

    /// <summary>An asker that doesn't keeps the last word: its model reads the answer and writes its own.</summary>
    [Test]
    public async Task AnAskerThatDoesNot_WritesItsOwnAnswer()
    {
        var client = Asks();

        string text = await TurnAsync(client, relays: false);

        Assert.Multiple(() =>
        {
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(text, Does.Contain("Retyped: "));
        });
    }
}
