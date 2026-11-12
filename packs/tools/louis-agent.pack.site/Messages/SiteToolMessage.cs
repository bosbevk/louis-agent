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

    [Description("Not shown: {Reason} Fix the card and call ShowCard again.")]
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
}
