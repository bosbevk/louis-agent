using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Components.Widget.Bar.Caption;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption;

/// <summary>
/// F134.4: which answers stand as a caption. Every answer with something to say does, as the design has it. The ones
/// that can't are those holding what only the whole talk draws.
/// </summary>
public class CaptionedTests
{
    /// <summary>A line the guide said.</summary>
    private static Block Said(string words) => new() { Kind = BlockKind.Text, Text = words };

    /// <summary>A finished call of a tool with its input.</summary>
    private static Block Call(string tool, string input) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = tool,
        ToolInput = input,
        ToolResult = "Done."
    };

    /// <summary>An answer of these blocks.</summary>
    private static Turn Answer(params Block[] blocks) => new() { UserText = "Hi", Blocks = [.. blocks] };

    /// <summary>Words alone are a caption, with or without a place shown.</summary>
    [Test]
    public void WordsAlone_AreACaption()
    {
        var caption = Captioned.Of(Answer(Said("Hello.")));

        Assert.That((caption?.Words, caption?.Offer), Is.EqualTo(("Hello.", (Offered?)null)));
    }

    /// <summary>
    /// An answer still coming in, with no words and no choices yet, is no caption: the orb says the guide is at work.
    /// </summary>
    [Test]
    public void WithNothingToSayYet_ThereIsNoCaption()
    {
        var thinking = new Block { Kind = BlockKind.Thinking, Text = "…" };

        Assert.That((Captioned.Of(null), Captioned.Of(Answer(thinking))),
            Is.EqualTo(((Captioned?)null, (Captioned?)null)));
    }

    /// <summary>A form to fill in is the talk's to draw, so an answer that asks with one needs the talk.</summary>
    [Test]
    public void AnAnswerThatAsksWithAForm_NeedsTheTalk()
    {
        var form = Call("AskFor", """{"question":"Your details?","fields":[{"label":"Email","kind":"email"}]}""");
        var turn = Answer(Said("Leave your details."), form);

        Assert.That((Captioned.Of(turn), Captioned.NeedsTheTalk(turn)), Is.EqualTo(((Captioned?)null, true)));
    }

    /// <summary>A card the guide drew is the talk's to draw too.</summary>
    [Test]
    public void AnAnswerThatDrewACard_NeedsTheTalk()
    {
        var card = Call(CardCall.Tool,
            """
            {
              "kicker": "BOOKING REQUEST",
              "title": "The garden room",
              "subtitle": "Seemeeu Guesthouse",
              "rows": [{ "icon": "calendar", "label": "Dates", "value": "Fri 14 to Sun 16 Nov" }],
              "status": { "label": "Not sent yet", "tone": "amber" },
              "note": "The guesthouse confirms by email."
            }
            """);
        var turn = Answer(Said("Here is your booking."), card);

        Assert.That((Captioned.Of(turn), Captioned.NeedsTheTalk(turn)), Is.EqualTo(((Captioned?)null, true)));
    }

    /// <summary>An answer that went wrong shows its error in the talk, never half of itself in a caption.</summary>
    [Test]
    public void AnAnswerThatWentWrong_NeedsTheTalk()
    {
        var turn = new Turn { UserText = "Hi", Blocks = [Said("I was saying")], Error = "The API went away." };

        Assert.That((Captioned.Of(turn), Captioned.NeedsTheTalk(turn)), Is.EqualTo(((Captioned?)null, true)));
    }

    /// <summary>An answer of words and choices needs no talk, and before any answer nothing does.</summary>
    [Test]
    public void AnOrdinaryAnswer_NeedsNoTalk()
    {
        var offer = Call("Offer", """{"question":"Which one?","choices":[{"label":"A"},{"label":"B"}]}""");

        Assert.That((Captioned.NeedsTheTalk(Answer(Said("Pick one."), offer)), Captioned.NeedsTheTalk(null)),
            Is.EqualTo((false, false)));
    }
}
