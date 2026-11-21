using System.ComponentModel;

namespace LouisAgent.Packs.Files.Messages;

/// <summary>What the files pack's screen says on the admin pages (ADR 0009), for a person, never the model.</summary>
public enum FilesScreenMessage
{
    [Description("Files")]
    Title,

    [Description("The workspace's files: open one to read it, or to ask the agent about it.")]
    Description
}
