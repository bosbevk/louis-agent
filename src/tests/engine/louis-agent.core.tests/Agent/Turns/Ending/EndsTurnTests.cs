using LouisAgent.Composition.Tools;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Turns.Ending;

/// <summary>
/// FX138, from Louis on louis-agent.dev: the guide offered the next stop as cards, then ran on to the stop after it
/// and offered again, with no click from the visitor. A tool that asks the visitor ends the turn once it succeeds, so
/// the model waits for their answer; a refused call doesn't, so the model can put it right.
/// </summary>
public class EndsTurnTests
{
    /// <summary>A tool that asks the visitor, as the site pack's Offer does, and one that only answers.</summary>
    private sealed class AskingTools
    {
        /// <summary>Asks; refused when told to be.</summary>
        [EndsTurn]
        public string Ask(bool refuse) => refuse ? "Error: not offered." : "Offered: 3 choices.";
    }

    /// <summary>A model that calls Ask, and would run on to another stop if asked again.</summary>
    private static FakeChatClient Asks(bool refuse) => new(
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextContent("Here is the stop."),
            new FunctionCallContent("call_1", nameof(AskingTools.Ask),
                new Dictionary<string, object?> { ["refuse"] = refuse })
        ])),
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "And on to the next stop.")));

    /// <summary>A turn of an engine with the asking tool; all its text.</summary>
    private static async Task<string> TurnAsync(IChatClient client)
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), client, TestPaths.Agent(),
            toolsets: [new AskingTools()]);
        var text = new System.Text.StringBuilder();
        await foreach (var update in engine.StreamPromptAsync(engine.NewHistory(), "Walk me through"))
            text.Append(update.Text);
        return text.ToString();
    }

    /// <summary>Once it has asked, the turn ends: the model isn't asked again, so it can't run on.</summary>
    [Test]
    public async Task ATurnEndsOnceTheVisitorIsAsked()
    {
        var client = Asks(refuse: false);

        string text = await TurnAsync(client);

        Assert.Multiple(() =>
        {
            Assert.That(client.Requests, Has.Count.EqualTo(1));
            Assert.That(text, Does.Not.Contain("next stop"));
        });
    }

    /// <summary>A refused ask doesn't end the turn: the model reads why and tries again.</summary>
    [Test]
    public async Task ARefusedAsk_LetsTheModelTryAgain()
    {
        var client = Asks(refuse: true);

        await TurnAsync(client);

        Assert.That(client.Requests, Has.Count.EqualTo(2));
    }
}
