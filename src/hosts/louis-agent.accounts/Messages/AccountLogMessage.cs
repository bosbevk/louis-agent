using System.ComponentModel;

namespace LouisAgent.Accounts.Messages;

/// <summary>The accounts library's log lines; written to stderr.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum AccountLogMessage
{
    [Description("Mail to {Email}: confirm the account at {Link} (no sender is set up, so this log is the mail)")]
    ConfirmationLink,

    [Description("Mail to {Email}: reset the password at {Link} (no sender is set up, so this log is the mail)")]
    PasswordResetLink,

    [Description("Mail to {Email}: the password reset code is {Code} (no sender is set up, so this log is the mail)")]
    PasswordResetCode,
}
