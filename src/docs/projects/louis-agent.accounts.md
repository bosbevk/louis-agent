# louis-agent.accounts — design

> **Explanation.** How the framework's accounts work: who a person is to a site, which tenant they belong to, how they
> sign in and what the browser app reads. **Elsewhere:** why in [ADR 0087](../adr/0087-identity-is-the-identity-server.md)
> and [ADR 0088](../adr/0088-a-tenant-on-every-account.md); how to change the web in [Change the web](../guides/change-the-web.md).
> **Kind:** host (a library, ASP.NET Core Identity) · **Path:** `src/hosts/louis-agent.accounts/`
> **Also shows:** `component`

![Accounts, and their parts: ASP.NET Core Identity, our routes beside it, and the tenant](diagrams/louis-agent.accounts.svg)

## Purpose

Puls's chat spends the API key, so a public site needs to know who's asking. `louis-agent.accounts` is the
framework's answer: sign-in, registration and a profile, on ASP.NET Core Identity.

Every account has a **tenant** from day one. A tenant is whose site the account belongs to. So the public site is one
tenant, and a company on a business site is just another row, not another deployment.

A demo's program calls three methods beside the API's, and the framework's web draws the pages.
`louis-agent.accounts.contracts` holds the records the browser reads and sends, just as `louis-agent.api.contracts`
does for the API. Three programs turn accounts on today: Puls's API, the coding agent's API and the starter's API.

The rest of this doc starts with the three calls a program makes, then follows a person through Identity's routes and
ours, the cookie, the tenant, the agent's key gate, confirming an email, the three ways to sign in that aren't a
password, mail and storage. It ends with the files, the settings and the tests, to look things up.

*Decided in:* F61.2, [ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md).

## How it works

A program adds accounts with three calls, before the API's key check. The key check then reads the person the cookie
names.

```csharp
builder.AddLouisAgentAccounts(AccountsOptions.FromEnvironment());   // Identity, the tenant, the cookie, the database
app.UseLouisAgentAccounts();     // migrate, then: the tenant from the host, who the cookie says, what they may do
app.MapLouisAgentAccounts();     // /account/*: Identity's routes and ours
```

Without `ACCOUNTS_SQL_URL`, each call quietly does nothing: one warning, no accounts, no `/account` route. With it,
here's what you get.

### 1. Identity is the identity server

Identity's routes under `/account` are Microsoft's own: `register`, `login?useCookies=true`, `confirmEmail`,
`resendConfirmationEmail`, `forgotPassword`, `resetPassword`, `manage/info` and `manage/2fa`. A person can't sign in
until they've clicked the link in the confirmation mail.

*Decided in:* [ADR 0087](../adr/0087-identity-is-the-identity-server.md).

### 2. Our routes beside them

A few routes are ours, in `Endpoints/AccountHandlers`, as named static methods. `GET /account/me` gives the
`AccountInfo` the profile shows. `PUT /account/profile` changes the display name. `POST /account/logout` signs out.
`DELETE /account` lets a person delete their own account, as POPIA asks. And `GET /account/providers` lists the
sign-in providers this instance has keys for. The whole table is pinned in `AccountsRoutesSnapshot.json`.

*Decided in:* [ADR 0051](../adr/0051-flat-conditionals-and-named-endpoints.md).

### 3. The cookie

The cookie (`Config/AccountCookies`) is HttpOnly, SameSite Lax, secure wherever the request is, and lasts 14 days,
sliding. A call without it gets a 401, never a redirect.

### 4. The tenant

The tenant comes from the site's address. `Tenants/TenantHost` tidies the host: lower case, no port, no `www.`.
`HostTenantResolver` finds the row that owns that host, or falls back to `ACCOUNTS_TENANT`. `UseTenant` fills the
request's `CurrentTenant`. Then `Store/AccountUserStore` looks a person up within that tenant, and stamps it on any new
account. The store refuses to work without a tenant.

The cookie carries the tenant too (`Users/AccountClaimsFactory`; the claim's name is in the contracts'
`AccountClaims`). So when the API opens a session, it reads who opened it, and where, from the cookie alone
(`SessionOpener`), never from the request's body.

*Decided in:* [ADR 0088](../adr/0088-a-tenant-on-every-account.md).

### 5. A signed-in person gets past the agent's key gate

A `/v1` call gets through with `AGENT_API_KEY`, or with the cookie *and* the header
`X-Requested-With: louis-agent.web`. The framework's web sends that header on every call, and a form on another site
can't. The accounts' middleware runs before the gate, so the gate already knows who the person is.

*Decided in:* F61.2.

### 6. Confirming an email

When a browser follows the confirmation link, it lands on the web's own page: `/confirm-email?done=true`, or
`?failed=true`. That's `Endpoints/Confirmation/ConfirmationRedirect`, a filter on Identity's routes. A client that
asks for JSON gets Identity's own text instead.

### 7. Signing in through a provider

`External/IExternalProvider` is one way to sign in through someone else; that's the Strategy pattern. Each provider
is only on when both its keys are set. `External/Providers/` holds Google and Microsoft, on Microsoft's own handlers,
and LinkedIn, on the generic OpenID Connect handler against LinkedIn's discovery document.

`GET /account/external/{provider}` sends the browser off to the provider. `GET /account/external/callback` reads
what it said, from Identity's external cookie. A login we know goes straight in. An email we know is joined onto its
account. Someone new gets an account in the current tenant, confirmed, with the name the provider gives them. Adding
`?link=true` attaches the provider to the account that's already signed in.

`DELETE /account/external/{provider}` unlinks one, but refuses if it's the last way in and there's no password.
`POST /account/password` sets a first password. Every answer to a browser is a redirect to a path of ours
(`LocalPath`). Registering the apps with each provider is in [a guide](../guides/register-a-sign-in-provider.md).

*Decided in:* F61.3.

### 8. Passkeys

A passkey is a key pair the person's device holds: a fingerprint, a face, or a security key. The server keeps the
public half, and never sees a secret.

Identity does the WebAuthn part; schema version 3 adds its table. Our routes under `/account/passkeys`
(`Endpoints/Passkeys/PasskeyHandlers`) hand the browser Identity's creation or request options as JSON, take back the
credential the browser made, and keep it under the name the person gave it. There's also the list, a delete, and
`POST /account/passkeys/login` to sign in with one.

Identity checks the browser's `Origin` against the request, so a credential made for another site is refused. An
attestation sent when no options are under way is a 400, with Identity's reason.

*Decided in:* F61.5.

### 9. Two-factor

Two-factor is Identity's own `POST /account/manage/2fa`. An empty body reads the state: the shared key, whether it's
on, and how many recovery codes are left. `enable` with the app's first code turns it on, and answers with the ten
recovery codes, once. `enable: false` turns it off, and `resetRecoveryCodes` makes a new set.

After that, the password alone answers 401 `RequiresTwoFactor`, and `login` takes `twoFactorCode` or
`twoFactorRecoveryCode` beside it. A recovery code works once. The key goes to the person as text and as an
`otpauth://` link; there's no QR code yet.

*Decided in:* F61.5.

### 10. Mail

Mail goes through Identity's `IEmailSender<AccountUser>`. `Mail/LogEmailSender` writes the link to the log, decoded
from Identity's HTML, and hashed unless `LOG_CONTENT=full`. A real sender would be one class behind that seam.

### 11. Storage

It's EF Core on SQL Server. `Store/AccountsDbContext` is Identity's context plus `Tenants`. Identity's unique indexes
on the email and the user name are made again as (tenant, email) and (tenant, user name), so the same email can
exist once per tenant.

![Accounts: the browser's calls through Identity's routes and ours to the store](diagrams/louis-agent.accounts-diagrams.svg)

![Registering: the host picks the tenant, then Identity's route finds and creates the person within it and sends the confirmation](diagrams/louis-agent.accounts-diagrams-2.svg)

These are the types involved:

![The types: the host's address names the tenant, the user store works within it, and each outside sign-in is a provider](diagrams/louis-agent.accounts-interface.svg)

## Structure

```
src/hosts/louis-agent.accounts/
├── AccountsStartup.cs   # AddLouisAgentAccounts, UseLouisAgentAccounts, MapLouisAgentAccounts
├── Config/              # AccountsOptions (the settings), AccountCookies (the cookie's rules)
├── Endpoints/           # AccountHandlers (named static methods), AccountRoutes (the map), AccountProblems (a refusal in Identity's shape);
│                        # Confirmation/ (the link's landing), External/ (ExternalLoginHandlers: the challenge, the callback, unlink, a first password; LocalPath),
│                        # Passkeys/ (the options, add, list, remove, sign in)
├── External/            # IExternalProvider, ExternalProvider (the wire names), ProviderKeys, ExternalProviders; Providers/ (Google, Microsoft, LinkedIn)
├── Mail/                # LogEmailSender behind Identity's IEmailSender
├── Messages/            # AccountErrorMessage, AccountLogMessage, AccountUserText (ADR 0009)
├── Migrations/          # what `dotnet ef migrations add` wrote; a tool's record, not sized by the file rule
├── Store/               # AccountsDbContext, AccountUserStore, AccountsMigrator, the design-time factory (names Identity's schema version 3)
├── Tenants/             # TenantHost, ITenantResolver, HostTenantResolver, CurrentTenant, TenantMiddleware
└── Users/               # AccountUser, Tenant, AccountClaimsFactory (the tenant on the cookie)
src/hosts/louis-agent.accounts.contracts/   # AccountInfo, ProfileBody, ProviderInfo, the Identity bodies (two-factor's too), the passkey bodies, AccountNames and AccountClaims, AccountJson
src/tests/hosts/louis-agent.accounts.tests/             # pure: the host rule, the options, the cookie, the JSON pins
src/tests/hosts/louis-agent.accounts.integration-tests/ # against the Docker SQL Server (ACCOUNTS_SQL_TEST_URL)
```

**Migrations.** The schema is EF Core migrations, in `Migrations/`. The host applies them at start-up, along with the
default tenant's row. A schema change is made with:

`dotnet ef migrations add {Name} --project src/hosts/louis-agent.accounts --startup-project src/hosts/louis-agent.accounts --output-dir Migrations --namespace LouisAgent.Accounts.Migrations`

Run `dotnet tool restore` first, since `dotnet-ef` is in `.config/dotnet-tools.json`. The tool never connects to a
database; it reads the model from the code. If it writes the model snapshot under `LouisAgent/Accounts/Migrations/`,
move it to `Migrations/`.

## Configuration

Accounts run inside a demo's API program, with its people and tenants in SQL Server.

![Where the accounts run: in the API's container, keeping people and tenants in the SQL Server container, and asking an outside provider when its keys are set](diagrams/louis-agent.accounts-deployment.svg)

| Setting | File | Means |
|---|---|---|
| `ACCOUNTS_SQL_URL` | `.env.secrets` | The database; unset, no accounts |
| `ACCOUNTS_TENANT` | `.env` | The tenant for a host no row names (default `site`; Puls sets `puls`) |
| `ACCOUNTS_COOKIE_DOMAIN` | `.env` | The cookie's domain; unset, the request's host |
| `MSSQL_SA_PASSWORD` | `.env.sql` | The PC's SQL Server, read only by `puls-sql` |
| `ACCOUNTS_SQL_TEST_URL` | shell, CI | The integration tests' server |
| `AUTH_{GOOGLE,MICROSOFT,LINKEDIN}_CLIENT_ID` and `_CLIENT_SECRET` | `.env.secrets` | A provider's keys; on only with both ([the guide](../guides/register-a-sign-in-provider.md)) |

**On the PC**, the server is `puls-sql`, in the Puls compose: SQL Server Developer edition, on a Docker volume. It's a
volume because a Windows bind mount makes the non-root server crash at start-up. Its sa password is in
`config/.env.sql`. The integration tests reach it at `ACCOUNTS_SQL_TEST_URL`, and each fixture makes and drops its own
database.

## Tests

There are two test projects. `src/tests/hosts/louis-agent.accounts.tests` needs no server. It covers the host rule,
the options, the cookie's rules, the store's refusal, the mail sender, and the JSON pinned to Identity's request
types.

`louis-agent.accounts.integration-tests` runs against a real server. It covers the migrations and the per-tenant
rule, the resolver, and the store. It has a test host that does what a program does: register, confirm from the
mail, sign in with the cookie, read `/account/me`, change the name, sign out and delete. The route table is pinned
there too. CI runs it against a SQL Server service container.

Passkeys and two-factor need no device. The passkey routes are proved with a software authenticator the tests write
themselves (`SoftwareAuthenticator`: an ES256 key, a "none" attestation, and the CBOR by hand on
`System.Formats.Cbor`). Two-factor is proved with a TOTP of the tests' own (`Endpoints/TwoFactor/Totp`, RFC 6238).

## Limits and plans

Several things come later: a real mail sender, Facebook and X, a QR code for the authenticator key (today it's the
key as text and an `otpauth://` link; a QR encoder of our own would be a step of its own), editing a passkey's name
after it's made, and the business site.

The three providers are built, and tested against a scripted one. Google sign-in works for real (FX68 made the
return address match behind Azure's proxy). Whether Microsoft's and LinkedIn's apps are registered isn't recorded in
the repo.

A few smaller limits: one Google account links in one tenant only, and a tenant has at most one host. Identity's own
validation messages, like a password that's too short, come in English whatever the site's language. And the
cookie's `/account/refresh` route (for bearer tokens) is mapped but unused.

## Related docs

[louis-agent.api](louis-agent.api.md) · [louis-agent.web](louis-agent.web.md) · [ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md) ·
[ADR 0087](../adr/0087-identity-is-the-identity-server.md) · [ADR 0088](../adr/0088-a-tenant-on-every-account.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
