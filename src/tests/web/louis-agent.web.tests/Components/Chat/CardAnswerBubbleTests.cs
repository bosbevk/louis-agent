using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Chats.Calls.Forms;
using LouisAgent.Web.Components.Chat;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat;

/// <summary>
/// FX142, Louis on a filled form and a clicked card each followed by a bubble saying the same: "should not have done
/// the bottom part there", "can be fixed in the framework", "as an option", "should be a chat settings", "by default
/// it would be off". With <see cref="ChatSettings.ShowCardAnswers"/> off, the default, a message that answers the card
/// or form the turn before drew, which already shows it, draws no bubble; a site that turns it on gets both.
/// </summary>
public class CardAnswerBubbleTests
{
    /// <summary>An answer that offered choices, and one that asked with a form.</summary>
    private static Block Offer() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = OfferCall.Tool,
        ToolInput =
            """{ "question": "Shall I send it?", "choices": [ { "label": "Book it" }, { "label": "Not now" } ] }""",
        ToolResult = "Offered: the visitor sees 2 choices."
    };

    /// <summary>A form asking for a name.</summary>
    private static Block Form() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = AskForCall.Tool,
        ToolInput = """{ "question": "Who?", "fields": [ { "label": "Name", "kind": "Name" } ] }""",
        ToolResult = "Asked: the visitor sees the form under your words, 1 to fill in."
    };

    /// <summary>A chat whose turns answer what the turn before drew: a click, a form, then words of their own.</summary>
    private static ChatModel Answered() => new()
    {
        SessionId = "sess_test",
        Turns =
        [
            new Turn
            {
                UserText = "A room for Friday",
                Blocks = [Offer()]
            },
            new Turn
            {
                UserText = "Book it",
                Blocks = [Form()]
            },
            new Turn
            {
                UserText = "Name: Anna",
                Blocks = [Offer()]
            },
            new Turn { UserText = "Can I bring two dogs?" }
        ]
    };

    /// <summary>An answer whose first choice holds a box the visitor writes in.</summary>
    private static Block OfferWithABox() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = OfferCall.Tool,
        ToolInput =
            """
            { "question": "Where to next?", "choices": [
              { "label": "Tell me about your business", "writes": "Text" }, { "label": "Stop here" } ] }
            """,
        ToolResult = "Offered: the visitor sees 2 choices."
    };

    /// <summary>A chat whose visitor wrote in a choice's box, then typed words that only look like a card's.</summary>
    private static ChatModel Written() => new()
    {
        SessionId = "sess_test",
        Turns =
        [
            new Turn
            {
                UserText = "Just interested",
                Blocks = [OfferWithABox()]
            },
            new Turn
            {
                UserText = "Tell me about your business: A guesthouse in Paternoster",
                Blocks = [OfferWithABox()]
            },
            new Turn { UserText = "Stop here: or maybe not" }
        ]
    };

    /// <summary>The bubbles the chat draws, the setting as a chat that draws cards passes it down.</summary>
    private static List<string> Bubbles(ChatModel chat, ChatSettings settings)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<MessageList>(parameters =>
            parameters.Add(list => list.Chat, chat).Add(list => list.ShowCardAnswers, settings.ShowCardAnswers));

        return cut.FindAll(".user-text").Select(bubble => bubble.TextContent).ToList();
    }

    /// <summary>By default a click's and a form's answers draw no bubble; words of the visitor's own still do.</summary>
    [Test]
    public void ByDefault_ACardsAnswer_DrawsNoBubble()
    {
        var cardsShowIt = new ChatSettings();

        Assert.That(Bubbles(Answered(), cardsShowIt),
            Is.EqualTo(new[] { "A room for Friday", "Can I bring two dogs?" }));
    }

    /// <summary>
    /// FX152: words sent from a choice's box answer that card too, and it folds to show them, so they draw no bubble.
    /// The box's label is the agent's ask, and a bubble would have the visitor say it. Words typed after a label
    /// that holds no box are the visitor's own, and stay.
    /// </summary>
    [Test]
    public void ByDefault_WordsSentFromAChoicesBox_DrawNoBubble()
    {
        var cardsShowIt = new ChatSettings();

        Assert.That(Bubbles(Written(), cardsShowIt),
            Is.EqualTo(new[] { "Just interested", "Stop here: or maybe not" }));
    }

    /// <summary>
    /// Louis, on an empty grey bubble left where the answer was: "this should also be hidden?". A card's answer leaves
    /// no message frame at all, not an empty one.
    /// </summary>
    [Test]
    public void ACardsAnswer_LeavesNoEmptyFrame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var chat = Answered();

        var cut = ctx.Render<MessageList>(parameters => parameters.Add(list => list.Chat, chat));

        Assert.That(cut.FindAll(".user-message"), Has.Count.EqualTo(2),
            "the first message and the visitor's own words");
    }

    /// <summary>Turned on, every message is a bubble, a card's answer too.</summary>
    [Test]
    public void On_EveryMessageIsABubble()
    {
        var everything = new ChatSettings { ShowCardAnswers = true };

        Assert.That(Bubbles(Answered(), everything),
            Is.EqualTo(new[] { "A room for Friday", "Book it", "Name: Anna", "Can I bring two dogs?" }));
    }

    /// <summary>
    /// The chats that draw cards pass their settings' choice down: the guide's widget and a business's window.
    /// </summary>
    [TestCase("Widget/ChatWidget.razor")]
    [TestCase("Site/Showroom/Mock/Live/MockLiveChat.razor")]
    public void TheChatsThatDrawCards_PassTheSettingDown(string component)
    {
        string markup = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components", component));

        Assert.That(markup, Does.Contain("ShowCardAnswers=\"Settings.ShowCardAnswers\""));
    }
}
