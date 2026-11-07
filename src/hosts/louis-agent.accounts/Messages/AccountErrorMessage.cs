using System.ComponentModel;

namespace LouisAgent.Accounts.Messages;

/// <summary>What the accounts library throws when a host or a caller gets its part wrong.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum AccountErrorMessage
{
    [Description("An account needs a tenant: nothing runs without one.")]
    TenantRequired,

    [Description("The request's tenant isn't set: UseTenant must run before the account routes and the store.")]
    TenantNotResolved,
}
