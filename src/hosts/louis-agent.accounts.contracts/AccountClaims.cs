namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// The claims the sign-in cookie carries beyond Identity's own (the user id is <c>ClaimTypes.NameIdentifier</c>),
/// named once here so the accounts library writes them and the API reads them by the same name.
/// </summary>
public static class AccountClaims
{
    /// <summary>The tenant the account is in (ADR 0088).</summary>
    public const string Tenant = "tenant";
}
