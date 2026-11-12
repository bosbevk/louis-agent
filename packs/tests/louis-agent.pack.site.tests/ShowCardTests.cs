using LouisAgent.Packs.Site.Tools;
using LouisAgent.Packs.Site.Tools.Cards;

namespace LouisAgent.Packs.Site.Tests;

/// <summary>
/// <c>ShowCard</c> (F127.4.1): the agent draws a read-only card, a ticket, from its parts; the tool checks it and
/// says it is shown, or refuses it with the reason so the model fixes what it wrote. The tool draws nothing: the
/// widget hears the call and draws the card.
/// </summary>
public class ShowCardTests
{
    private readonly CardTools _tools = new();

    /// <summary>The guesthouse's booking request.</summary>
    private static readonly CardRow[] Rows =
    [
        new("calendar", "Dates", "Fri 14 to Sun 16 Nov"),
        new("users", "Guests", "2 adults"),
        new("mail", "Contact", "Anna · anna@example.com")
    ];

    /// <summary>A whole card is shown, and the model is asked for one line, not the card again in words.</summary>
    [Test]
    public void AWholeCard_IsShown()
    {
        string answer = _tools.ShowCard("BOOKING REQUEST", "The garden room", Rows, "Seemeeu Guesthouse",
            new CardStatus("Not sent yet", "amber"), "The guesthouse confirms by email.");

        Assert.That(answer, Does.StartWith("Shown: the visitor sees the card"));
    }

    /// <summary>
    /// An icon the card doesn't draw is refused, naming the ones it does. A refusal starts with "Error", so the stream
    /// marks the call failed and the widget draws nothing for it.
    /// </summary>
    [Test]
    public void AnUnknownIcon_IsRefused_WithTheIcons()
    {
        string answer = _tools.ShowCard("K", "T", [new CardRow("rocket", "Ship", "Soon")]);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error: not shown."));
            Assert.That(answer, Does.Contain("'rocket'").And.Contain("calendar"));
        });
    }

    /// <summary>A card without rows is refused with the reason.</summary>
    [Test]
    public void ACardWithoutRows_IsRefused_WithTheReason()
    {
        string answer = _tools.ShowCard("K", "T", []);

        Assert.That(answer, Does.StartWith("Error: not shown.").And.Contain("one to six rows"));
    }
}
