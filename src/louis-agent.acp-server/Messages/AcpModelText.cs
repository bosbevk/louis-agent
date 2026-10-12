namespace LouisAgent.AcpServer.Messages;

using System.ComponentModel;

/// <summary>Texts the ACP server adds for the model: the session's workspace notice.</summary>
public enum AcpModelText
{
    [Description("The Rider project workspace is mounted at '{Root}'. Use the workspace tools to inspect and edit project files. All file paths passed to those tools must be relative to that workspace.")]
    WorkspaceNotice,

    [Description("[Attached file: {Name} could not be read - it is not inside a folder mounted into the agent ({Uri})]")]
    AttachmentNotMounted,

    [Description("[Attached file: {Name} could not be read: {Reason}]")]
    AttachmentUnreadable,

    [Description("\n[Attachment truncated: showing {Shown} of {Length} chars]")]
    AttachmentTruncated,

    [Description("[Attached file: {Name}]\n```\n{Content}\n```")]
    Attachment,

    [Description("[Attached file: {Name} ({Path})]\n```\n{Content}\n```")]
    AttachmentAt,
}
