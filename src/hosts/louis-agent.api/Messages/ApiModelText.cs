
using System.ComponentModel;

namespace LouisAgent.Api.Messages;
/// <summary>Texts the API adds for the model: attached files, and the page the visitor is reading.</summary>
public enum ApiModelText
{
    [Description("[Attached file: {Name}]\n```\n{Content}\n```")]
    Attachment,

    [Description("[The user is in {Location}; their time zone is {TimeZone}.]")]
    UserPlace,

    [Description("\n[Attachment truncated: showing {Shown} of {Length} chars]")]
    AttachmentTruncated,

    [Description("[The visitor is reading {Url} ({Title}). The page says:\n{Text}{Areas}{InView}]")]
    Page,

    [Description("\nIts areas:\n{List}")]
    PageAreas,

    [Description("\nOn their screen now: {Id} {Heading}")]
    PageInView,

    [Description("[On their screen now, on {Url}: {Id} {Heading}]")]
    PageInViewOnly,

    [Description("\n[Page truncated: showing {Shown} of {Length} chars]")]
    PageTruncated,
}
