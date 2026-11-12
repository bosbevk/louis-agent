using System.ComponentModel;

namespace LouisAgent.Packs.Site.Messages;

/// <summary>The site pack's texts the model reads (ADR 0009).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum SiteToolMessage
{
    [Description("Shown: the visitor's browser is now on {Path}. Say in one line what to look at there.")]
    Shown,

    [Description(
        "Not shown: '{Path}' isn't a page of this site. Give an address from the page's list: a page such as " +
        "/framework/packs, or a place on one such as /framework/identities/levels.")]
    NotASitePage,

    [Description("Shown: the visitor sees the card. Say one line about it; don't repeat its rows in words.")]
    CardShown,

    [Description("Error: not shown. {Reason} Fix the card and call ShowCard again.")]
    CardRefused,

    [Description("A card needs its \"title\": the headline on its band.")]
    CardWithoutTitle,

    [Description("A card shows one to six rows; this one has {Count}.")]
    CardRowCount,

    [Description("Row {Number} of the card needs both its \"label\" and its \"value\".")]
    CardHalfRow,

    [Description("Row {Number}'s icon '{Icon}' isn't one the card draws; use one of {Icons}.")]
    CardUnknownIcon,

    [Description("The status's tone '{Tone}' isn't one there is; use one of {Tones}.")]
    CardUnknownTone,

    [Description(
        "Offered: the visitor sees {Count} choices as cards under your words; their click comes back as their next " +
        "message. Don't list the choices again, and wait for it.")]
    Offered,

    [Description("Error: not offered. {Reason} Fix the choices and call Offer again.")]
    OfferRefused,

    [Description("The cards need their question: what the choices answer.")]
    OfferWithoutQuestion,

    [Description("Offer two to four choices; this has {Count}.")]
    OfferCount,

    [Description("Choice {Number} needs a label of 1 to {Most} characters, a button's words; '{Label}' has {Length}.")]
    OfferBadLabel,

    [Description("Choice {Number}'s why runs one line, {Most} characters at most; it has {Length}.")]
    OfferLongWhy,

    [Description("Mark one suggested choice at most; {Count} are.")]
    OfferSuggestedTwice,

    [Description("Two choices say the same, '{Label}': each needs its own words.")]
    OfferSameLabel,
}
