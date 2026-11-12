using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools;
using LouisAgent.Packs.Site.Messages;
using LouisAgent.Packs.Site.Messages.Holes.Cards;
using LouisAgent.Packs.Site.Messages.Holes.Forms;
using LouisAgent.Packs.Site.Tools.Cards;
using LouisAgent.Packs.Site.Tools.Forms;

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
        if (CardCheck.Problem(title, rows, status) is not { } problem)
            return MessageText.Format(SiteToolMessage.CardShown);
        var refused = new CardRefusedHoles(problem);
        return MessageText.Format(SiteToolMessage.CardRefused, refused);
    }

    /// <summary>
    /// Offers the visitor two to four choices as cards; their click comes back as their message. Offering ends the
    /// turn, so the agent waits for the pick instead of running on (FX138).
    /// </summary>
    [EndsTurn]
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
        if (OfferCheck.Problem(question, choices) is not { } problem)
        {
            var offered = new OfferedHoles(choices.Count);
            return MessageText.Format(SiteToolMessage.Offered, offered);
        }

        var refused = new OfferRefusedHoles(problem);
        return MessageText.Format(SiteToolMessage.OfferRefused, refused);
    }

    /// <summary>
    /// Shows the visitor a receipt once something went on their behalf: what happened, and what comes next. Each site
    /// draws it in its own look.
    /// </summary>
    [Description(
        "Shows the visitor a receipt in the chat once something went on their behalf, such as a request passed on: a " +
        "short title saying what happened and one line on what comes next. Call it only after it really went; the " +
        "card the visitor filled in already shows the details, so never repeat them here.")]
    public string ShowReceipt(
        [Description("What happened, up to 40 characters, such as Sent to the owner.")]
        string title,
        [Description("What comes next, one line of up to 120 characters, such as when and how they hear back.")]
        string line)
    {
        if (ReceiptCheck.Problem(title, line) is not { } problem)
            return MessageText.Format(SiteToolMessage.ReceiptShown);
        var refused = new ReceiptRefusedHoles(problem);
        return MessageText.Format(SiteToolMessage.ReceiptRefused, refused);
    }

    /// <summary>
    /// Asks the visitor for what the agent needs with a form of one to four fields; what they fill in comes back as
    /// their message. Asking ends the turn, as offering does, so the agent waits for the answer.
    /// </summary>
    [EndsTurn]
    [Description(
        "Asks the visitor with a form under your words, whenever you need them to write something: their name and " +
        "how to reach them, or a job told in their own words. They fill it in and send it, and it comes back as " +
        "their next message, each field by its label. Never offer a choice that only says to type; ask with this.")]
    public string AskFor(
        [Description("What the form asks for, as a short question, such as Who shall I put the request under?")]
        string question,
        [Description(
            "One to four fields: a label of up to 30 characters, the words over its box such as Phone or email; " +
            "and its kind: Name, Phone, Email, Text for one line, or LongText for a box that grows.")]
        IReadOnlyList<FormField> fields)
    {
        if (FormCheck.Problem(question, fields) is not { } problem)
        {
            var asked = new AskedHoles(fields.Count);
            return MessageText.Format(SiteToolMessage.Asked, asked);
        }

        var refused = new AskRefusedHoles(problem);
        return MessageText.Format(SiteToolMessage.AskRefused, refused);
    }
}
