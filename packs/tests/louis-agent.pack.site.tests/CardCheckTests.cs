using LouisAgent.Choices;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tickets;
using LouisAgent.Diagrams.Tickets.Inputs;
using LouisAgent.Diagrams.Tones;
using LouisAgent.Packs.Site.Tools.Cards;

namespace LouisAgent.Packs.Site.Tests;

/// <summary>
/// The pack's card rules are a tested duplicate of the diagrams library's (F127.4.1): a pack references no web (ADR
/// 0086) and the library lives with the web, so these tests hold the two equal. A card the tool calls shown is one
/// the widget draws, and one it refuses the library refuses too.
/// </summary>
public class CardCheckTests
{
    /// <summary>The icons the pack names are the ones the library draws, no more and no fewer.</summary>
    [Test]
    public void TheIcons_AreTheLibrarys() =>
        Assert.That(CardCheck.Icons, Is.EquivalentTo(SettingChoice.Names<DiagramIcon>()));

    /// <summary>The tones the pack names are the library's.</summary>
    [Test]
    public void TheTones_AreTheLibrarys() =>
        Assert.That(CardCheck.Tones, Is.EquivalentTo(SettingChoice.Names<DiagramTone>()));

    /// <summary>Cards on either side of each rule: the pack and the library agree on every one.</summary>
    [TestCaseSource(nameof(Cards))]
    public void ThePackAndTheLibrary_AgreeOnACard(string title, CardRow[] rows, CardStatus? status)
    {
        bool packShows = CardCheck.Problem(title, rows, status) is null;
        bool libraryDraws;
        try
        {
            TicketCard.Of("KICKER", title,
                rows.Select(row => new TicketRowInput(row.Icon, row.Label, row.Value)).ToList(),
                status: status is null ? null : new TicketStatusInput(status.Label, status.Tone));
            libraryDraws = true;
        }
        catch (InvalidDataException)
        {
            libraryDraws = false;
        }

        Assert.That(packShows, Is.EqualTo(libraryDraws));
    }

    /// <summary>A whole card, then one broken each way the rules know.</summary>
    private static IEnumerable<TestCaseData> Cards()
    {
        CardRow good = new("calendar", "Dates", "Fri to Sun");
        yield return new TestCaseData("Title", new[] { good }, new CardStatus("Sent", "green")).SetName("whole");
        yield return new TestCaseData(" ", new[] { good }, null).SetName("no title");
        yield return new TestCaseData("Title", Array.Empty<CardRow>(), null).SetName("no rows");
        yield return new TestCaseData("Title", Enumerable.Repeat(good, 7).ToArray(), null).SetName("seven rows");
        yield return new TestCaseData("Title", new[] { good with { Value = "" } }, null).SetName("half a row");
        yield return new TestCaseData("Title", new[] { good with { Icon = "rocket" } }, null).SetName("unknown icon");
        yield return new TestCaseData("Title", new[] { good with { Icon = "PAW" } }, null).SetName("icon in capitals");
        yield return new TestCaseData("Title", new[] { good }, new CardStatus("Sent", "gold")).SetName("unknown tone");
    }
}
