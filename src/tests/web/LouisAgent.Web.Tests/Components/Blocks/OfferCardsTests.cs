using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Components;
using LouisAgent.Web.Components.Chat;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Blocks;

/// <summary>
/// Choices the agent offers (F127.4.1, Louis: "when the guide must give options to the user, like you do for me"):
/// an <c>Offer</c> call is drawn as cards under the answer, with the work hidden too; a click sends the card's label
/// as the visitor's message, and once the visitor has answered, by a click or by typing, the cards fold to the one
/// chosen.
/// </summary>
public class OfferCardsTests
{
    /// <summary>The guesthouse's question before it sends a booking, as the model sends it.</summary>
    private const string Booking =
        """
        {
          "question": "Shall I send it to the guesthouse?",
          "choices": [
            { "label": "Book it", "why": "Sends this request to Seemeeu Guesthouse.", "suggested": true },
            { "label": "Change something", "why": "The dates, the guests or your contact." },
            { "label": "Not now" }
          ]
        }
        """;

    /// <summary>A finished call with its input, an Offer unless named otherwise.</summary>
    private static Block Call(string? input, bool failed = false, string tool = OfferCall.Tool) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = tool,
        ToolInput = input,
        ToolResult = failed ? "Error: not offered. No." : "Offered: the visitor sees 3 choices.",
        ToolFailed = failed
    };

    /// <summary>A context with the chat's words, before anything renders.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The blocks with the work hidden, as a business's window and the guide's widget draw them.</summary>
    private static IRenderedComponent<BlocksView> Render(BunitContext ctx, Block block, string? answer = null,
        bool streaming = false, Action<string>? chosen = null) =>
        ctx.Render<BlocksView>(p =>
        {
            p.Add(x => x.Blocks, [block]).Add(x => x.ShowWork, false).Add(x => x.Answer, answer)
                .Add(x => x.Streaming, streaming);
            if (chosen is not null) p.Add(x => x.Chosen, EventCallback.Factory.Create(new object(), chosen));
        });

    /// <summary>The question and a card per choice, its why under it, the suggested one marked.</summary>
    [Test]
    public void AnOffer_IsDrawnAsCards_WithTheWorkHidden()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Booking), chosen: _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".offer-question").TextContent, Is.EqualTo("Shall I send it to the guesthouse?"));
            Assert.That(cut.FindAll("button.offer-card .offer-label").Select(label => label.TextContent),
                Is.EqualTo(new[] { "Book it", "Change something", "Not now" }));
            Assert.That(cut.FindAll(".offer-why"), Has.Count.EqualTo(2));
            Assert.That(cut.FindAll("button.offer-card.is-suggested .offer-label").Single().TextContent,
                Is.EqualTo("Book it"));
            Assert.That(cut.FindAll("button.offer-card[disabled]"), Is.Empty);
        });
    }

    /// <summary>A click sends the card's label, as if the visitor had typed it.</summary>
    [Test]
    public void AClick_SendsTheLabel()
    {
        using var ctx = Context();
        string? sent = null;

        var cut = Render(ctx, Call(Booking), chosen: label => sent = label);
        cut.FindAll("button.offer-card")[1].Click();

        Assert.That(sent, Is.EqualTo("Change something"));
    }

    /// <summary>Once the visitor clicked a card, the cards fold to that one, and it can't be clicked again.</summary>
    [Test]
    public void Answered_TheCardsFoldToTheOneChosen()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Booking), answer: "book it", chosen: _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".offer-card .offer-label").Select(label => label.TextContent),
                Is.EqualTo(new[] { "Book it" }));
            Assert.That(cut.Find(".offer-card").ClassList, Does.Contain("is-chosen"));
            Assert.That(cut.FindAll("button.offer-card"), Is.Empty);
        });
    }

    /// <summary>A visitor who typed their own answer folds the cards away: none was chosen.</summary>
    [Test]
    public void AnsweredInTheirOwnWords_NoCardIsLeft()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Booking), answer: "Can I bring two dogs?", chosen: _ => { });

        Assert.That(cut.FindAll(".offer-card"), Is.Empty);
    }

    /// <summary>
    /// While the answer still streams, or where nothing can send (a recording), the cards can't be clicked.
    /// </summary>
    [Test]
    public void WhileStreamingOrWithNothingToSend_TheCardsWait()
    {
        using var ctx = Context();

        var streaming = Render(ctx, Call(Booking), streaming: true, chosen: _ => { });
        var recorded = Render(ctx, Call(Booking));

        Assert.Multiple(() =>
        {
            Assert.That(streaming.FindAll("button.offer-card[disabled]"), Has.Count.EqualTo(3));
            Assert.That(recorded.FindAll("button.offer-card[disabled]"), Has.Count.EqualTo(3));
        });
    }

    /// <summary>
    /// A refused offer is a failed call (its answer starts with "Error"), and the model offers again: it draws
    /// nothing, so the visitor never sees cards twice; any other call, or one with no input yet, is no offer.
    /// </summary>
    [Test]
    public void OnlyAGoodOfferCall_IsAnOffer()
    {
        using var ctx = Context();

        var failed = Render(ctx, Call(Booking, failed: true), chosen: _ => { });

        Assert.Multiple(() =>
        {
            Assert.That(failed.FindAll(".offer"), Is.Empty);
            Assert.That(OfferCall.Of(Call(Booking))?.Choices, Has.Count.EqualTo(3));
            Assert.That(OfferCall.Of(Call(Booking, tool: CardCall.Tool)), Is.Null);
            Assert.That(OfferCall.Of(Call(null)), Is.Null);
            Assert.That(OfferCall.Of(Call("{ \"question\": ")), Is.Null);
        });
    }

    /// <summary>
    /// In a chat, a click sends through the chat's own callback, and the visitor's next message is the answer: once it
    /// comes in, the finished turn draws again and its cards fold to the one sent.
    /// </summary>
    [Test]
    public void InAChat_TheNextMessageIsTheAnswer()
    {
        using var ctx = Context();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        string? sent = null;
        var chat = new ChatModel
        {
            SessionId = "s1",
            Turns =
            [
                new Turn
                {
                    UserText = "A room for Friday",
                    Blocks = [Call(Booking)]
                }
            ]
        };

        var cut = ctx.Render<MessageList>(p => p.Add(x => x.Chat, chat).Add(x => x.ShowWork, false)
            .Add(x => x.Chosen, EventCallback.Factory.Create<string>(new object(), label => sent = label)));
        cut.Find("button.offer-card").Click();
        chat.Turns.Add(new Turn { UserText = "Book it" });
        cut.Render();

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.EqualTo("Book it"));
            Assert.That(cut.FindAll("button.offer-card"), Is.Empty);
            Assert.That(cut.Find(".offer-card.is-chosen .offer-label").TextContent, Is.EqualTo("Book it"));
        });
    }

    /// <summary>The cards match their golden file.</summary>
    [Test]
    public void TheCards_MatchTheGoldenFile()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Booking), chosen: _ => { });

        Snapshots.Match("Components/Blocks/OfferCards.html", cut.Markup);
    }

    /// <summary>Choices where one asks the visitor to write: their business, in a box of its own.</summary>
    private const string Leads =
        """
        {
          "question": "Where to next?",
          "choices": [
            { "label": "Tell me about your business", "why": "So I can show how leads would work for it",
              "suggested": true, "writes": "Text" },
            { "label": "Guesthouse demo", "why": "See an agent turn a visitor into a booking" }
          ]
        }
        """;

    /// <summary>
    /// FX147, Louis on a card that only said to type: "that first card should have had a input with button"; his pick
    /// of three, a choice can hold a box. That card draws a box and a send button; what they write goes as their
    /// message, after the card's label, as a form's answer does, and the other cards stay plain.
    /// </summary>
    [Test]
    public void AChoiceThatWrites_HoldsABox_AndSendsWhatIsWritten()
    {
        using var ctx = Context();
        string? sent = null;
        var cut = Render(ctx, Call(Leads), chosen: label => sent = label);

        cut.Find(".offer-writes input").Input("A guesthouse in Paternoster");
        cut.Find(".offer-writes form").Submit();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".offer-writes"), Has.Count.EqualTo(1), "only the choice that writes has a box");
            Assert.That(sent, Is.EqualTo("Tell me about your business: A guesthouse in Paternoster"));
        });
    }

    /// <summary>Once answered, the offer folds to the card that was written in, showing what was sent.</summary>
    [Test]
    public void AWrittenAnswer_FoldsToItsCard()
    {
        using var ctx = Context();

        var cut = Render(ctx, Call(Leads), answer: "Tell me about your business: A guesthouse in Paternoster");

        Assert.That(cut.FindAll(".offer-card.is-chosen").Select(card => card.TextContent.Trim()),
            Is.EqualTo(new[] { "Tell me about your business: A guesthouse in Paternoster" }));
    }
}
