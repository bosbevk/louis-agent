# louis-agent.accounts — design

> **Explanation.** How the framework's accounts work: who a person is to a site, which tenant they belong to, how they
> sign in and what the browser app reads. **Elsewhere:** why in [ADR 0087](../adr/0087-identity-is-the-identity-server.md)
> and [ADR 0088](../adr/0088-a-tenant-on-every-account.md); how to change the web in [Change the web](../guides/change-the-web.md).

![Accounts, and their parts: ASP.NET Core Identity, our routes beside it, and the tenant](diagrams/louis-agent.accounts.svg)

## Purpose

Puls's chat spends the API key, so a public site needs to know who is asking. `louis-agent.accounts` is the
framework's answer: sign-in, registration and a profile on ASP.NET Core Identity, with a **tenant on every account**
from the first day, so the public site is one tenant and a company on a business site is another row, not another
deployment. A demo's program calls three methods beside the API's, and the framework's web draws the pages (F61.2).
`louis-agent.accounts.contracts` holds the records the browser reads and sends, as `louis-agent.api.contracts` does
for the API (ADR 0045).

## How it works

A program does this, before the API's key check (which reads the person the cookie names, F61.2):

```csharp
builder.AddLouisAgentAccounts(AccountsOptions.FromEnvironment());   // Identity, the tenant, the cookie, the database
app.UseLouisAgentAccounts();     // migrate, then: the tenant from the host, who the cookie says, what they may do
app.MapLouisAgentAccounts();     // /account/*: Identity's routes and ours
```

Without `ACCOUNTS_SQL_URL` each call is a quiet no: one warning, no accounts, no `/account` route. With it:

- **Identity is the identity server** (ADR 0087). Its routes under `/account` are Microsoft's own: `register`,
  `login?useCookies=true`, `confirmEmail`, `resendConfirmationEmail`, `forgotPassword`, `resetPassword`,
  `manage/info`, `manage/2fa`. A person can't sign in until the mail's link confirmed them.
- **Ours beside them** (`Endpoints/AccountHandlers`, named static methods, ADR 0051): `GET /account/me` (the
  `AccountInfo` the profile shows), `PUT /account/profile` (the display name), `POST /account/logout`,
  `DELETE /account` (the person deletes their own account, POPIA) and `GET /account/providers` (the sign-in
  providers this instance has keys for; empty until F61.3). The table is pinned in `AccountsRoutesSnapshot.json`.
- **The cookie** (`Config/AccountCookies`): HttpOnly, SameSite Lax, secure where the request is, 14 days sliding; a
  call without it gets a 401, never a redirect.
- **The tenant** (ADR 0088): `Tenants/TenantHost` normalises the host (lower case, no port, no `www.`),
  `HostTenantResolver` finds the row that owns it or falls back to `ACCOUNTS_TENANT`, `UseTenant` fills the request's
  `CurrentTenant`, and `Store/AccountUserStore` looks a person up within it and stamps it on a new account. The
  store refuses without a tenant.
- **The cookie carries the tenant** (`Users/AccountClaimsFactory`, the claim's name in the contracts' `AccountClaims`),
  so the API reads who opened a session and where from the cookie alone (`SessionOpener`), never from the body.
- **The agent's key gate takes a signed-in person** (F61.2): a `/v1` call gets through with `AGENT_API_KEY`, or with
  the cookie *and* the header `X-Requested-With: louis-agent.web`, which the framework's web sends on every call and a
  cross-site form can't. The accounts' middleware runs before the gate, so the person is known to it.
- **A browser following the confirmation link lands on the web's page** (`Endpoints/Confirmation/ConfirmationRedirect`,
  an endpoint filter on Identity's routes): `/confirm-email?done=true`, or `?failed=true`; a client that asks for JSON
  gets Identity's own text.
- **Through a provider** (F61.3): `External/IExternalProvider` is one way to sign in through someone else (Strategy),
  on only with both its keys; `External/Providers/` holds Google and Microsoft on Microsoft's own handlers and LinkedIn
  on the generic OpenID Connect handler against LinkedIn's discovery document. `GET /account/external/{provider}`
  sends the browser to the provider, `GET /account/external/callback` reads what it said from Identity's external
  cookie: a known login straight in, a known email onto its account, a first-timer made in the current tenant,
  confirmed and named as the provider names them; `?link=true` attaches the provider to the signed-in account.
  `DELETE /account/external/{provider}` unlinks, refused for the last way in without a password; `POST
  /account/password` is a first password. Every answer to a browser is a redirect to a path of ours (`LocalPath`).
  Registering the apps is [a guide](../guides/register-a-sign-in-provider.md).
- **A passkey** (F61.5) is a key pair the person's device holds (a fingerprint, a face, a security key): the server
  keeps the public half and never sees a secret. Identity does the WebAuthn part (schema version 3 adds its table);
  our routes under `/account/passkeys` (`Endpoints/Passkeys/PasskeyHandlers`) hand the browser Identity's creation
  or request options as JSON, take the credential the browser made back, and keep it under the name the person gave;
  the list, a delete, and `POST /account/passkeys/login` sign in with one. Identity checks the browser's `Origin`
  against the request, so a credential made for another site is refused; an attestation sent with no options under
  way is a 400 with Identity's reason.
- **Two-factor** (F61.5) is Identity's own `POST /account/manage/2fa`: an empty body reads the state (the shared
  key, whether it's on, how many recovery codes are left), `enable` with the app's first code turns it on and answers
  the ten recovery codes once, `enable: false` turns it off, `resetRecoveryCodes` makes a new set. After that the
  password alone answers 401 `RequiresTwoFactor`, and `login` takes `twoFactorCode` or `twoFactorRecoveryCode`
  beside it; a recovery code works once. The key goes to the person as text and as an `otpauth://` link; no QR code
  yet.
- **Mail** goes through Identity's `IEmailSender<AccountUser>`; `Mail/LogEmailSender` writes the link to the log,
  decoded from Identity's HTML form, hashed unless `LOG_CONTENT=full`. A real sender is one class behind the seam.
- **Storage** is EF Core on SQL Server: `Store/AccountsDbContext` is Identity's context plus `Tenants`, with the
  unique indexes on the email and the user name made again as (tenant, email) and (tenant, user name).

### Diagrams

![Accounts: the browser's calls through Identity's routes and ours to the store](diagrams/louis-agent.accounts-diagrams.svg)

![Registering: the host picks the tenant, then Identity's route finds and creates the person within it and sends the confirmation](diagrams/louis-agent.accounts-diagrams-2.svg)

![The types: the host's address names the tenant, the user store works within it, and each outside sign-in is a provider](diagrams/louis-agent.accounts-interface.svg)

## Structure

```
src/hosts/louis-agent.accounts/
├── AccountsStartup.cs   # AddLouisAgentAccounts, UseLouisAgentAccounts, MapLouisAgentAccounts
├── Config/              # AccountsOptions (the settings), AccountCookies (the cookie's rules)
├── Endpoints/           # AccountHandlers (named static methods), AccountRoutes (the map), AccountProblems (a refusal in Identity's shape);
│                        # Confirmation/ (the link's landing), External/ (the challenge, the callback, unlink, a first password, LocalPath),
│                        # Passkeys/ (the options, add, list, remove, sign in)
├── External/            # IExternalProvider, ExternalProvider (the wire names), ProviderKeys, ExternalProviders; Providers/ (Google, Microsoft, LinkedIn)
├── Mail/                # LogEmailSender behind Identity's IEmailSender
├── Messages/            # AccountErrorMessage, AccountLogMessage, AccountUserText (ADR 0009)
├── Migrations/          # what `dotnet ef migrations add` wrote; a tool's record, not sized by the file rule
├── Store/               # AccountsDbContext, AccountUserStore, AccountsMigrator, the design-time factory (names Identity's schema version 3)
├── Tenants/             # TenantHost, ITenantResolver, HostTenantResolver, CurrentTenant, TenantMiddleware
└── Users/               # AccountUser, Tenant
src/hosts/louis-agent.accounts.contracts/   # AccountInfo, ProfileBody, ProviderInfo, the Identity bodies (two-factor's too), the passkey bodies, AccountNames, AccountJson
src/tests/hosts/louis-agent.accounts.tests/             # pure: the host rule, the options, the cookie, the JSON pins
src/tests/hosts/louis-agent.accounts.integration-tests/ # against the Docker SQL Server (ACCOUNTS_SQL_TEST_URL)
```

**Migrations.** The schema is EF Core migrations in `Migrations/`, applied by the host at start-up with the default
tenant's row. A schema change is `dotnet ef migrations add {Name} --project src/hosts/louis-agent.accounts
--startup-project src/hosts/louis-agent.accounts --output-dir Migrations --namespace LouisAgent.Accounts.Migrations`
(`dotnet tool restore` first: `dotnet-ef` is in `.config/dotnet-tools.json`). The tool never connects; it reads the
model from the code. If it writes the model snapshot under `LouisAgent/Accounts/Migrations/`, move it to `Migrations/`.

## Configuration

| Setting | File | Means |
|---|---|---|
| `ACCOUNTS_SQL_URL` | `.env.secrets` | The database; unset, no accounts |
| `ACCOUNTS_TENANT` | `.env` | The tenant for a host no row names (default `site`; Puls sets `puls`) |
| `ACCOUNTS_COOKIE_DOMAIN` | `.env` | The cookie's domain; unset, the request's host |
| `MSSQL_SA_PASSWORD` | `.env.sql` | The PC's SQL Server, read only by `puls-sql` |
| `ACCOUNTS_SQL_TEST_URL` | shell, CI | The integration tests' server |
| `AUTH_{GOOGLE,MICROSOFT,LINKEDIN}_CLIENT_ID` and `_CLIENT_SECRET` | `.env.secrets` | A provider's keys; on only with both ([the guide](../guides/register-a-sign-in-provider.md)) |

**On the PC** the server is `puls-sql` in the Puls compose: SQL Server Developer edition on a Docker volume (a
Windows bind mount makes the non-root server crash at start-up), its sa password in `config/.env.sql`. The
integration tests reach it at `ACCOUNTS_SQL_TEST_URL` and each fixture makes and drops its own database.

## Tests

`src/tests/hosts/louis-agent.accounts.tests` (no server: the host rule, the options, the cookie's rules, the store's
refusal, the mail sender, the JSON pins to Identity's request types) and `louis-agent.accounts.integration-tests`
(the migrations on a real server and the per-tenant rule, the resolver, the store, and a test host that does what a
program does: register, confirm from the mail, sign in with the cookie, read `/account/me`, change the name, sign
out, delete; the route table pinned). The passkey routes are proved with a software authenticator the tests write
(`SoftwareAuthenticator`: an ES256 key, a "none" attestation, the CBOR by hand on `System.Formats.Cbor`), and
two-factor with a TOTP of the tests' own (`Endpoints/TwoFactor/Totp`, RFC 6238), so neither needs a device. CI runs
the second project against a SQL Server service container.

## Limits and plans

A real mail sender, Facebook and X, the QR code for the authenticator key (today the key as text and an `otpauth://`
link; a QR encoder of our own is a step of its own), a passkey's name edited after the fact, and the business site are
later. The three
providers are built and tested on a scripted one; a sign-in through the real Google, Microsoft or LinkedIn waits for
Louis to register the apps. One Google
account links in one tenant only. A tenant has at most one host. Identity's own validation messages (a password too
short) come in English whatever the site's language. The cookie's `/account/refresh` (bearer tokens) is mapped and
unused.

## Related docs

[louis-agent.api](louis-agent.api.md) · [louis-agent.web](louis-agent.web.md) · [ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md) ·
[ADR 0087](../adr/0087-identity-is-the-identity-server.md) · [ADR 0088](../adr/0088-a-tenant-on-every-account.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
