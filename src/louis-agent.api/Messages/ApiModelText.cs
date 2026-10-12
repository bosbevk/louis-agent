namespace LouisAgent.Api.Messages;

using System.ComponentModel;

/// <summary>Texts the API adds for the model: attached files.</summary>
public enum ApiModelText
{
    [Description("[Attached file: {Name}]\n```\n{Content}\n```")]
    Attachment,

    [Description("\n[Attachment truncated: showing {Shown} of {Length} chars]")]
    AttachmentTruncated,
}
