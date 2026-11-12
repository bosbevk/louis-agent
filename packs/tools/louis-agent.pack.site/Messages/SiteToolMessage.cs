using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages.Holes.Cards;
using LouisAgent.Packs.Site.Messages.Holes.Forms;
using LouisAgent.Packs.Site.Messages.Holes.Pages;

namespace LouisAgent.Packs.Site.Messages;

/// <summary>The site pack's texts the model reads (ADR 0009).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum SiteToolMessage
{
    [Description("Shown: the visitor's browser is now on {Path}. Say in one line what to look at there.")]
    [Holes(typeof(PlaceHoles))]
    Shown,

    [Holes(typeof(PlaceHoles))]
    [Description(
        "Not shown: '{Path}' isn't a page of this site. Give an address from the page's list: a page such as " +
        "/framework/packs, or a place on one such as /framework/identities/levels.")]
    NotASitePage,

    [Description("Shown: the visitor sees the card under your words. Don't repeat its rows in words.")]
    CardShown,

    [Description("Error: not shown. {Reason} Fix the card and call ShowCard again.")]
    [Holes(typeof(CardRefusedHoles))]
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
        "message. The cards end this answer: write nothing more, and wait for it.")]
    [Holes(typeof(OfferedHoles))]
    Offered,

    [Description("Error: not offered. {Reason} Fix the choices and call Offer again.")]
    [Holes(typeof(OfferRefusedHoles))]
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

    [Description(
        "Asked: the visitor sees the form under your words, {Count} to fill in; what they send comes back as their " +
        "next message, each field by its label. The form ends this answer: write nothing more, and wait for it.")]
    [Holes(typeof(AskedHoles))]
    Asked,

    [Description("Error: not asked. {Reason} Fix the form and call AskFor again.")]
    [Holes(typeof(AskRefusedHoles))]
    AskRefused,

    [Description("The form needs its question: what it asks for.")]
    AskWithoutQuestion,

    [Description("A form asks one to four fields; this has {Count}.")]
    [Holes(typeof(AskFieldCountHoles))]
    AskFieldCount,

    [Description(
        "Field {Number} needs a label of 1 to {Most} characters, the words over its box; '{Label}' has {Length}.")]
    [Holes(typeof(AskBadLabelHoles))]
    AskBadLabel,

    [Description("Field {Number}'s kind isn't one the form draws; use one of {Kinds}.")]
    [Holes(typeof(AskUnknownKindHoles))]
    AskUnknownKind,

    [Description("Two fields say the same, '{Label}': each needs its own label.")]
    [Holes(typeof(AskSameLabelHoles))]
    AskSameLabel,

    [Description("Shown: the visitor sees the receipt under your words. Don't say it again in words.")]
    ReceiptShown,

    [Description("Error: not shown. {Reason} Fix the receipt and call ShowReceipt again.")]
    [Holes(typeof(ReceiptRefusedHoles))]
    ReceiptRefused,

    [Description("A receipt needs its title: what happened, such as Sent to the owner.")]
    ReceiptWithoutTitle,

    [Description("The receipt's {Part} runs {Most} characters at most; it has {Length}.")]
    [Holes(typeof(ReceiptTooLongHoles))]
    ReceiptTooLong,

    [Description(
        "Places on the site for \"{Words}\", the best first. Show one with ShowPage by its address.\n{Places}")]
    [Holes(typeof(PlacesFoundHoles))]
    PlacesFound,

    [Description("- {Address}  {Heading} ({Kind})")]
    [Holes(typeof(PlaceLineHoles))]
    PlaceLine,

    [Description("Nothing on the site matches \"{Words}\". Try other words, or open a page from the site's pages.")]
    [Holes(typeof(NoPlaceFoundHoles))]
    NoPlaceFound,

    [Description("This host serves no site to search. Open a page from the site's pages instead.")]
    NoSiteToSearch,
}
