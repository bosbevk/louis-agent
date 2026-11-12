using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;
using LouisAgent.Packs.Site.Tools.Cards;

namespace LouisAgent.Packs.Site.Tools;

/// <summary>
/// The <c>site</c> toolset's cards (F127.4.1): a read-only card the agent draws itself, such as a booking request,
/// and (step 6) choices offered as cards. Like <see cref="SiteTools"/>, the tools draw nothing: they check what the
/// agent wrote and say it is shown, and the web's widget hears the call and draws it with the diagrams library.
/// </summary>
/// <remarks>
/// A tool for what the page draws (ADR 0080): a card that wouldn't draw comes back to the model with the reason
/// (Louis's picks 3 and 5, Q1). The check is <see cref="CardCheck"/>, the library's rules held equal by the pack's
/// tests, since a pack references no web (ADR 0086), so the card this calls shown is the card the visitor sees.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0118-a-diagram-is-data-our-renderer-draws.md">ADR 0118: a diagram is data our renderer draws</seealso>
public sealed class CardTools
{
    /// <summary>Shows the visitor a read-only card, a ticket, drawn from its parts.</summary>
    [Description(
        "Shows the visitor a read-only card in the chat, such as a booking request or an order, drawn from its " +
        "parts: a small kicker, a title, rows of facts with an icon each, and an optional subtitle, status and " +
        "note. The visitor can't change it; to ask them something, offer choices after it. Then say one line, " +
        "not the card again.")]
    public string ShowCard(
        [Description("A few words in capitals over the title, such as BOOKING REQUEST.")]
        string kicker,
        [Description("The card's headline, such as The garden room.")]
        string title,
        [Description(
            "One to six facts, each an icon by name (calendar, clock, users, user, paw, home, phone, mail, tag, " +
            "info…), what the fact is (Dates) and what it says (Fri 14 to Sun 16 Nov).")]
        IReadOnlyList<CardRow> rows,
        [Description("A line under the title, such as the business and its town.")]
        string? subtitle = null,
        [Description("Where it stands, such as Not sent yet in amber, or Sent in green once it went.")]
        CardStatus? status = null,
        [Description("A short line at the card's foot, such as how the business confirms.")]
        string? note = null)
    {
        if (CardCheck.Problem(title, rows, status) is { } problem)
            return MessageText.Format(SiteToolMessage.CardRefused, new { Reason = problem });
        return MessageText.Format(SiteToolMessage.CardShown, new { });
    }

    /// <summary>Offers the visitor two to four choices as cards; their click comes back as their message.</summary>
    [Description(
        "Offers the visitor two to four choices as cards under your words, when they must decide something: try " +
        "a demo, book or not, which plan. Their click comes back as their next message, in the label's words, and " +
        "they may type their own answer instead. Never act on a choice before it comes back.")]
    public string Offer(
        [Description("What the choices answer, as a short question, such as Shall I send it to the guesthouse?")]
        string question,
        [Description(
            "Two to four choices: a label of up to 40 characters, a button's words such as Book it; an optional " +
            "why of one line under it; and suggested true on the one you recommend, at most one.")]
        IReadOnlyList<OfferChoice> choices)
    {
        if (OfferCheck.Problem(question, choices) is { } problem)
            return MessageText.Format(SiteToolMessage.OfferRefused, new { Reason = problem });
        return MessageText.Format(SiteToolMessage.Offered, new { choices.Count });
    }
}
