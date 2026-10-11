namespace LouisAgent.AcpServer.Messages;

using System.ComponentModel;

/// <summary>Texts the ACP server adds for the model: the session's workspace notice.</summary>
public enum AcpModelText
{
    [Description("The Rider project workspace is mounted at '{Root}'. Use the workspace tools to inspect and edit project files. All file paths passed to those tools must be relative to that workspace.")]
    WorkspaceNotice,
}
