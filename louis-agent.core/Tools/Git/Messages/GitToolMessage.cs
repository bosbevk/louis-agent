
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Git.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the git toolset, returned to the model: what went wrong and what to do instead.</summary>
public enum GitToolMessage
{
    [Description("No commits match.")] GitNoCommits,

    [Description("Give at least one of text, author, since or until to search the commits.")]
    GitSearchNeedsCriteria,

    [Description(
        "{Parameter} '{Value}' starts with '-': git would read it as an option, so nothing was run. Give a name, reference or path, not an option.")]
    GitValueRefused,

    [Description(
        "SetConfig sets only {Allowed}; '{Key}' was not set. Other keys (core.fsmonitor, core.sshCommand, core.hooksPath, alias.*) can make git run commands. GetConfig can read any key.")]
    GitConfigKeyRefused,

    [Description(
        "{Parameter} '{Value}' is a refspec, not a branch name: '+' would force the push and ':' would push to or delete another ref. Give the branch name alone, and use force for a forced push.")]
    GitRefspecRefused,

    [Description("[Timeout] Git command exceeded 30s limit.")]
    GitTimeout,

    [Description("No uncommitted changes.")]
    NoUncommittedChanges,

    [Description("Uncommitted changes found:\n{Status}")]
    UncommittedChanges,
}
