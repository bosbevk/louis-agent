using System.ComponentModel;
using LouisAgent.Acp.Messages.Holes.ModelText;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Acp.Messages;

/// <summary>
/// Texts the ACP server adds for the model: the session's workspace notice and the files Rider attached.
/// </summary>
public enum AcpModelText
{
    [Description("The Rider project workspace is mounted at '{Root}'. Use the workspace tools to inspect and edit " +
                 "project files. All file paths passed to those tools must be relative to that workspace.")]
    [Holes(typeof(WorkspaceNoticeHoles))]
    WorkspaceNotice,

    [Description(
        "[Attached file: {Name} could not be read - it is not inside a folder mounted into the agent ({Uri})]")]
    [Holes(typeof(AttachmentNotMountedHoles))]
    AttachmentNotMounted,

    [Description("[Attached file: {Name} could not be read: {Reason}]")]
    [Holes(typeof(AttachmentUnreadableHoles))]
    AttachmentUnreadable,

    [Description("\n[Attachment truncated: showing {Shown} of {Length} chars]")]
    [Holes(typeof(AttachmentTruncatedHoles))]
    AttachmentTruncated,

    [Description("[Attached file: {Name}]\n```\n{Content}\n```")]
    [Holes(typeof(AttachmentHoles))]
    Attachment,

    [Description("[Attached file: {Name} ({Path})]\n```\n{Content}\n```")]
    [Holes(typeof(AttachmentAtHoles))]
    AttachmentAt,
}
