namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// Who is signed in, as <c>GET /account/me</c> tells the browser app: the ids, the email and whether it's confirmed,
/// the name they go by, the providers linked to the account, and whether it has a password of its own.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record AccountInfo(
    string UserId,
    string TenantId,
    string Email,
    string? DisplayName,
    bool EmailConfirmed,
    IReadOnlyList<string> Logins,
    bool HasPassword);

/// <summary>What the profile page sends to <c>PUT /account/profile</c>: the name, or none to clear it.</summary>
public sealed record ProfileBody(string? DisplayName);

/// <summary>What the profile sends to <c>POST /account/password</c>: a first password for an account that has none.</summary>
public sealed record SetPasswordBody(string NewPassword);
