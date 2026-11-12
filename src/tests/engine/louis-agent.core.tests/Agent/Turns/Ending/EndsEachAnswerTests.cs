using LouisAgent.Composition.Sessions;
using LouisAgent.Composition.Tools;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Turns.Ending;

/// <summary>
/// FX139, Louis: "the guide should always display options, even to go to another page after it was done", and "it
/// does not alwys offer offers". An identity that ends each answer with a tool (the guide's Offer) gets it: when its
/// answer ends without it, the engine asks once more with that tool required and thinking off, and the tool ends the
/// turn (FX138). An answer that offered already, or an identity that asks for nothing, costs no extra request.
/// </summary>
public class EndsEachAnswerTests
{
    /// <summary>The tool an answer must end with.</summary>
    private sealed class AskingTools
    {
        /// <summary>Asks the person.</summary>
        [EndsTurn]
        public string Ask() => "Offered: 3 choices.";

        /// <summary>Asks another agent, by the engine's own tool's name.</summary>
        public string AskAgent() => "Booked? What name should I put it under?";
    }

    /// <summary>A model reply that only talks.</summary>
    private static ChatResponse Says(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>A model reply that calls Ask.</summary>
    private static ChatResponse CallsAsk() => new(new ChatMessage(ChatRole.Assistant,
        [new FunctionCallContent("call_1", nameof(AskingTools.Ask), new Dictionary<string, object?>())]));

    /// <summary>A turn as an identity that ends each answer with <paramref name="endsWith"/>.</summary>
    private static async Task TurnAsync(IChatClient client, string? endsWith)
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), client, TestPaths.Agent(),
            toolsets: [new AskingTools()])
        { Thinking = ReasoningEffort.Low };
        using var _ = TurnIdentity.Use(new TurnIdentity("guide", false, endsWith));
        await foreach (var __ in engine.StreamPromptAsync(engine.NewHistory(), "Walk me through"))
        {
        }
    }

    /// <summary>
    /// An answer that ends without its tool gets one more request, the tool required and thinking off, and the tool
    /// ends the turn there.
    /// </summary>
    [Test]
    public async Task AnAnswerWithoutItsTool_IsAskedForIt_Once()
    {
        var client = new FakeChatClient(_ => Says("Here is the stop."), _ => CallsAsk(), _ => Says("Never asked."));

        await TurnAsync(client, endsWith: nameof(AskingTools.Ask));

        var asked = client.Requests[1].Options!;
        Assert.Multiple(() =>
        {
            Assert.That(client.Requests, Has.Count.EqualTo(2));
            Assert.That(asked.ToolMode, Is.EqualTo(ChatToolMode.RequireSpecific(nameof(AskingTools.Ask))));
            Assert.That(asked.Reasoning, Is.Null, "a required tool and thinking don't go together");
            Assert.That(client.Requests[1].Messages[^1],
                Has.Property(nameof(ChatMessage.Role)).EqualTo(ChatRole.User)
                    .And.Property(nameof(ChatMessage.Text)).Contains(nameof(AskingTools.Ask)),
                "a request ends with the person's message: Anthropic refuses an assistant prefill");
        });
    }

    /// <summary>
    /// FX141, Louis: "you see it is waiting for my name, so now no card should display". An answer that asked another
    /// agent owes no choices: that agent may be waiting for the person, in its own window.
    /// </summary>
    [Test]
    public async Task AnAnswerThatAskedAnotherAgent_OwesNoChoices()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", nameof(AskingTools.AskAgent), new Dictionary<string, object?>())
            ])),
            _ => Says("It answered in its window: answer its question there."),
            _ => CallsAsk());

        await TurnAsync(client, endsWith: nameof(AskingTools.Ask));

        Assert.That(client.Requests, Has.Count.EqualTo(2), "no third request for choices");
    }

    /// <summary>An answer that ends with its tool, or an identity that asks for none, costs no extra request.</summary>
    [Test]
    public async Task AnAnswerThatOffered_OrNoSetting_CostsNoExtraRequest()
    {
        var offered = new FakeChatClient(_ => CallsAsk(), _ => Says("Never asked."));
        var plain = new FakeChatClient(_ => Says("Here is the stop."), _ => CallsAsk());

        await TurnAsync(offered, endsWith: nameof(AskingTools.Ask));
        await TurnAsync(plain, endsWith: null);

        Assert.That((offered.Requests.Count, plain.Requests.Count), Is.EqualTo((1, 1)));
    }
}
