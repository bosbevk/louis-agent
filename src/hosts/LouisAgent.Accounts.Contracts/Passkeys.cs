namespace LouisAgent.Accounts.Contracts;

/// <summary>
/// What the browser sends to <c>POST /account/passkeys</c>: the credential <c>navigator.credentials.create()</c> gave,
/// as JSON, and the name the person gives the passkey ("Laptop", "Phone").
/// </summary>
public sealed record PasskeyBody(string CredentialJson, string? Name);

/// <summary>What the browser sends to <c>POST /account/passkeys/login</c>: the credential <c>navigator.credentials.get()</c> gave.</summary>
public sealed record PasskeyLoginBody(string CredentialJson);

/// <summary>One passkey on the account, as the profile lists it: its id (base64url), its name and when it was added.</summary>
public sealed record PasskeyInfo(string Id, string? Name, DateTimeOffset CreatedAt);

/// <summary>What <c>GET /account/passkeys</c> answers.</summary>
public sealed record PasskeysReply(IReadOnlyList<PasskeyInfo> Passkeys);
