using System.ComponentModel;

namespace LouisAgent.Accounts.Messages;

/// <summary>What the account routes answer a person when a request can't be done as asked.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum AccountUserText
{
    [Description("A name is at most {Max} characters.")]
    DisplayNameTooLong,

    [Description("This is the last way into the account: set a password first.")]
    LastWayIn,

    [Description("This account already has a password; change it instead.")]
    HasPasswordAlready,
}
