using System.ComponentModel;
using LouisAgent.Api.Messages.Holes.Attachments;
using LouisAgent.Api.Messages.Holes.Pages;
using LouisAgent.Api.Messages.Holes.Places;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Api.Messages;

/// <summary>Texts the API adds for the model: attached files, and the page the visitor is reading.</summary>
public enum ApiModelText
{
    [Description("[Attached file: {Name}]\n```\n{Content}\n```")]
    [Holes(typeof(AttachmentHoles))]
    Attachment,

    [Description("[The user is in {Location}; their time zone is {TimeZone}.]")]
    UserPlace,

    [Description("\n[Attachment truncated: showing {Shown} of {Length} chars]")]
    [Holes(typeof(AttachmentTruncatedHoles))]
    AttachmentTruncated,

    [Description("[The visitor is reading {Url} ({Title}). The page says:\n{Text}{Areas}{Links}{InView}]")]
    [Holes(typeof(PageHoles))]
    Page,

    [Description("\nIts areas:\n{List}")]
    [Holes(typeof(PageAreasHoles))]
    PageAreas,

    [Description("\nIts links:\n{List}")]
    [Holes(typeof(PageLinksHoles))]
    PageLinks,

    [Description("\nOn their screen now: the section {Id}, \"{Heading}\", of the page {Url} they are reading{Open}")]
    [Holes(typeof(PageInViewHoles))]
    PageInView,

    [Description(
        "[On their screen now: the section {Id}, \"{Heading}\", of the page {Url}, whose words came with an earlier message{Open}]")]
    [Holes(typeof(PageInViewOnlyHoles))]
    PageInViewOnly,

    [Description("\nOpen on it: the card {Id}, \"{Heading}\". When they say \"this\", they mean that card.")]
    [Holes(typeof(PageCardOpenHoles))]
    PageCardOpen,

    [Description("\n[Page truncated: showing {Shown} of {Length} chars]")]
    [Holes(typeof(PageTruncatedHoles))]
    PageTruncated,

    [Description("\n[The part of the site: {Focus}.]")]
    [Holes(typeof(PageFocusHoles))]
    PageFocus,

    [Description("\n[The visitor selected the piece {Id}, \"{Heading}\", on the page.]")]
    [Holes(typeof(PageSelectedHoles))]
    PageSelected,
}
