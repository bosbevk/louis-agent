using System.Reflection;
using LouisAgent.Composition.Tools;
using LouisAgent.Packs.Site.Tools;
using LouisAgent.Packs.Site.Tools.Cards;
using LouisAgent.Packs.Site.Tools.Forms;

namespace LouisAgent.Packs.Site.Tests.Tools;

/// <summary>
/// <c>Offer</c> (F127.4.1, Louis: "when the guide must give options to the user, like you do for me"): the agent
/// offers two to four choices as cards; the widget draws them, and the visitor's click comes back as their own next
/// message. The tool checks the choices and refuses what wouldn't make good cards, saying why.
/// </summary>
public class OfferTests
{
    private readonly CardTools _tools = new();

    /// <summary>The guesthouse's question before it sends a booking.</summary>
    private static readonly OfferChoice[] Booking =
    [
        new("Book it", "Sends this request to Seemeeu Guesthouse.", true),
        new("Change something", "The dates, the guests or your contact."),
        new("Not now", "Nothing is sent.")
    ];

    /// <summary>Good choices are offered; the model hears the click comes back as the visitor's message.</summary>
    /// <summary>
    /// FX147, Louis: "that first card should have had a input with button". A choice may hold a box, by the kind of
    /// words it asks for, and the tool tells the model so: a choice that asks them to write holds one.
    /// </summary>
    [Test]
    public void AChoiceMayHoldABox()
    {
        OfferChoice[] leads =
        [
            new("Tell you my business", "So I can show how leads would work for it", true, FieldKind.Text),
            new("Guesthouse demo", "See an agent turn a visitor into a booking"),
        ];
        var described = typeof(CardTools).GetMethod(nameof(CardTools.Offer))!.GetParameters()[1]
            .GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()!.Description;

        Assert.Multiple(() =>
        {
            Assert.That(_tools.Offer("Where to next?", leads), Does.StartWith("Offered:"));
            Assert.That(described, Does.Contain("writes").And.Contain("never a choice that only says to type"));
        });
    }

    [Test]
    public void GoodChoices_AreOffered()
    {
        string answer = _tools.Offer("Shall I send it to the guesthouse?", Booking);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Offered:"));
            Assert.That(answer, Does.Contain("3 choices").And.Contain("next message"));
        });
    }

    /// <summary>
    /// FX138: offering ends the turn, so the agent waits for the visitor's pick instead of running on to the next
    /// stop.
    /// </summary>
    [Test]
    public void Offering_EndsTheTurn() =>
        Assert.That(typeof(CardTools).GetMethod(nameof(CardTools.Offer))!.IsDefined(typeof(EndsTurnAttribute), false),
            Is.True);

    /// <summary>One choice is no choice, and five crowd the chat: both are refused, saying how many may be.</summary>
    [TestCase(1)]
    [TestCase(5)]
    public void TooFewOrTooMany_AreRefused(int count)
    {
        var choices = Enumerable.Range(1, count).Select(n => new OfferChoice($"Choice {n}")).ToArray();

        Assert.That(_tools.Offer("Which?", choices), Does.StartWith("Error: not offered.").And.Contain("two to four"));
    }

    /// <summary>A question is needed: the cards say what they answer.</summary>
    [Test]
    public void AnEmptyQuestion_IsRefused() =>
        Assert.That(_tools.Offer(" ", Booking), Does.StartWith("Error: not offered.").And.Contain("question"));

    /// <summary>A label is a short button: an empty one or one over 40 characters is refused, naming it.</summary>
    [TestCase("")]
    [TestCase("Yes please, send the whole booking request to the guesthouse now")]
    public void ABadLabel_IsRefused(string label)
    {
        OfferChoice[] choices = [new(label), new("Not now")];

        Assert.That(_tools.Offer("Send it?", choices), Does.StartWith("Error: not offered.").And.Contain("Choice 1"));
    }

    /// <summary>A reason runs one line: over 80 characters it is refused.</summary>
    [Test]
    public void ALongWhy_IsRefused()
    {
        OfferChoice[] choices = [new("Book it", new string('x', 81)), new("Not now")];

        Assert.That(_tools.Offer("Send it?", choices), Does.StartWith("Error: not offered.").And.Contain("80"));
    }

    /// <summary>Only one choice may be suggested, or none: two suggestions suggest nothing.</summary>
    [Test]
    public void TwoSuggested_AreRefused()
    {
        OfferChoice[] choices = [new("Book it", Suggested: true), new("Change something", Suggested: true)];

        Assert.That(_tools.Offer("Send it?", choices),
            Does.StartWith("Error: not offered.").And.Contain("one suggested"));
    }

    /// <summary>Two choices with the same label would send the same words: refused.</summary>
    [Test]
    public void TheSameLabelTwice_IsRefused()
    {
        OfferChoice[] choices = [new("Not now"), new("not now")];

        Assert.That(_tools.Offer("Send it?", choices), Does.StartWith("Error: not offered.").And.Contain("same"));
    }
}
