using System.ComponentModel;

namespace LouisAgent.Packs.Git.Messages;

/// <summary>What the git pack's screens say on the admin pages (ADR 0009), for a person, never the model.</summary>
public enum GitScreenMessage
{
    [Description("Changes")]
    ChangesTitle,

    [Description("What's changed in the workspace: review a file, then stage, discard or commit it.")]
    ChangesDescription,

    [Description("Branches")]
    BranchesTitle,

    [Description("Every branch and the commit log: read what a branch holds, then merge or delete it.")]
    BranchesDescription
}
