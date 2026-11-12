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

        /// <summary>Asks the person with a form, which also ends the turn; a refused form ends nothing.</summary>
        [EndsTurn]
        public string Form(bool good) =>
            good ? "Asked: 2 to fill in." : "Error: not asked. The form needs its question.";
    }

    /// <summary>A model reply that asks with a form, a good one or a refused one.</summary>
    private static ChatResponse CallsForm(bool good)
    {
        var formArguments = new Dictionary<string, object?> { ["good"] = good };
        return new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent("call_f", nameof(AskingTools.Form), formArguments)]));
    }

    /// <summary>A model reply that only talks.</summary>
    private static ChatResponse Says(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>A model reply that calls Ask.</summary>
    private static ChatResponse CallsAsk() => new(new ChatMessage(ChatRole.Assistant,
        [new FunctionCallContent("call_1", nameof(AskingTools.Ask), new Dictionary<string, object?>())]));

    /// <summary>The guide's stop, as its identity names it.</summary>
    private static readonly string[] Stops = ["Stop here"];

    /// <summary>
    /// A turn as an identity that ends each answer with <paramref name="endsWith"/> and stops on
    /// <see cref="Stops"/>, the visitor saying <paramref name="message"/>.
    /// </summary>
    private static async Task TurnAsync(IChatClient client, string? endsWith, string message = "Walk me through")
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), client, TestPaths.Agent(),
            toolsets: [new AskingTools()])
        { Thinking = ReasoningEffort.Low };
        var guide = new TurnIdentity("guide", false, endsWith, Stops);
        using var _ = TurnIdentity.Use(guide);
        await foreach (var __ in engine.StreamPromptAsync(engine.NewHistory(), message))
        {
        }
    }

    /// <summary>
    /// Louis, on "Stop here" answered with choices again: "stop should end". After a message that is one of the
    /// identity's stops, alone or with the page's context after it, the answer owes no choices; any other still does.
    /// </summary>
    [TestCase("Stop here", 1)]
    [TestCase("stop here\n\n[page] /pricing", 1)]
    [TestCase("Stop here and tell me the pricing", 2)]
    public async Task AfterAStop_NoChoicesAreOwed(string message, int requests)
    {
        var client = new FakeChatClient(_ => Says("Alright, we'll stop here."), _ => CallsAsk());

        await TurnAsync(client, endsWith: nameof(AskingTools.Ask), message);

        Assert.That(client.Requests, Has.Count.EqualTo(requests));
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

    /// <summary>
    /// F127.4.3, Louis: "and then only that card". An answer that ended with another tool that ends the turn (a form)
    /// is closed: it owes no choices beside it.
    /// </summary>
    [Test]
    public async Task AnAnswerThatEndedWithAForm_OwesNoChoices()
    {
        var client = new FakeChatClient(_ => CallsForm(good: true), _ => CallsAsk());

        await TurnAsync(client, endsWith: nameof(AskingTools.Ask));

        Assert.That(client.Requests, Has.Count.EqualTo(1), "no second request for choices");
    }

    /// <summary>A refused form ended nothing: when the answer then stops in words, it still owes its choices.</summary>
    [Test]
    public async Task AnAnswerWhoseFormWasRefused_StillOwesChoices()
    {
        var client = new FakeChatClient(_ => CallsForm(good: false), _ => Says("Tell me your name."), _ => CallsAsk());

        await TurnAsync(client, endsWith: nameof(AskingTools.Ask));

        Assert.That(client.Requests, Has.Count.EqualTo(3));
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
