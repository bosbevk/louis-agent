# 0087. ASP.NET Core Identity is the identity server, signing in with a cookie

**Status:** Accepted · **Date:** 2026-11-07 (Louis: a SQL backend with an identity server for logins, registrations and
profiles; "no paid products, if otherwise we build it ourselves") · **Recorded:** 2026-11-07

## Context

Puls's chat spends the API key, so a public site must know who is asking before the Azure lock can come off (F61).
Louis asked for an identity server, with sign-in through Google, Microsoft, LinkedIn and the rest, and for nothing
paid. Duende IdentityServer is the .NET identity server people reach for; it is paid, its free edition is for
self-hosting under revenue and capital thresholds, and it only matters when other apps need tokens from us, which
none does. A third-party login service (Entra External ID, Auth0) is someone else's product between us and our users.

ASP.NET Core Identity is Microsoft's own, MIT, in the shared framework: users, password hashing, lockout, email
confirmation, password reset, two-factor, external logins and, in .NET 10, passkeys. Its JSON routes
(`MapIdentityApi`) cover register, login, confirm, forgot, reset and account info. It doesn't do logout, a profile,
external-login routes or tenants; those are small.

The browser app is Blazor WebAssembly served by the same API, so the choice between a cookie and a bearer token is a
choice about where the credential lives: a cookie marked HttpOnly never reaches a script, a token in the browser's
storage does. The cost of a cookie is that a cross-site form could send it.

## Decision

**We use ASP.NET Core Identity as the identity server, in `louis-agent.accounts`, and we sign in with a cookie.**

- Identity's own routes are mapped under `/account` as they are; what it lacks is ours beside them, each a named
  method answering a record from `louis-agent.accounts.contracts` (ADR 0045). A body the browser sends to one of
  Identity's routes is a record of ours that a test pins to Identity's request type.
- The cookie is HttpOnly, SameSite Lax, secure where the request is, and a call without it gets a 401, never a
  redirect to a page: the API answers JSON and the browser app draws the page.
- A cookie counts on the agent's `/v1` routes only with the header `X-Requested-With: louis-agent.web`, which a
  cross-site form can't send (F61.2); the admin routes keep the admin key.
- Mail goes through Identity's own `IEmailSender<TUser>`; until a sender is picked, the log is the mail, with the
  link in full only where `LOG_CONTENT=full`.
- Storage is EF Core on SQL Server, the same code on the PC's Docker and on Azure SQL; the schema is migrations the
  host applies as it starts. A migration's files are a tool's record and aren't sized by the file rule.

## Consequences

- Register, confirm, sign in, forgot and reset work with no code of ours, and passkeys and two-factor come from the
  same library (F61.5). What we write is the tenant, the profile, logout, delete, and the external-login routes.
- No tokens for other apps: an app that needs one is a later decision, not a switch.
- Identity's routes answer validation errors as RFC 7807 problem details, so the browser app reads that shape for
  the whole `/account` group, beside the API's own `ApiError` elsewhere.
- Identity writes a confirmation link for an HTML mail (`&amp;`); every sender decodes it before a person sees it.
- `/account/refresh` (bearer tokens) is mapped with the rest and unused; a client that calls it gets nothing useful.

## Links

[F61](../plans/F61-accounts-and-azure.md) · [0088](0088-a-tenant-on-every-account.md) · [0045](0045-typed-answers-and-a-shared-contract.md) ·
[louis-agent.accounts](../projects/louis-agent.accounts.md) · [Identity API endpoints](https://learn.microsoft.com/aspnet/core/security/authentication/identity-api-authorization)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
