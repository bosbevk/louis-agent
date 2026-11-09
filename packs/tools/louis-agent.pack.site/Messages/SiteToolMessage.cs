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
}
