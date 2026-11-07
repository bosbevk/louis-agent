# louis-agent.accounts — design

> **Explanation.** How the framework's accounts work: who a person is to a site, which tenant they belong to, how they
> sign in and what the browser app reads. **Elsewhere:** why the choices are what they are in the ADRs it links; how to
> change the web in [Change the web](../guides/change-the-web.md); the plan that builds it,
> [F61](../plans/F61-accounts-and-azure.md), until it's done.

## Purpose

Puls's chat spends the API key, so a public site needs to know who is asking. `louis-agent.accounts` is the
framework's answer: sign-in, registration and a profile on ASP.NET Core Identity, with a **tenant on every account**
from the first day, so the public site is one tenant and a company on a business site is another row, not another
deployment. A demo's program calls three methods (`AddLouisAgentAccounts`, `UseLouisAgentAccounts`,
`MapLouisAgentAccounts`) beside the API's, and the framework's web draws the pages. `louis-agent.accounts.contracts`
holds the records the browser reads and sends, as `louis-agent.api.contracts` does for the API (ADR 0045).

## How it works

- **Identity is the identity server.** Users, password hashing, lockout, email confirmation, password reset, external
  logins and passkeys are Microsoft's own (MIT), in the shared framework. Its JSON routes (`MapIdentityApi`) are
  mapped under `/account`; what it lacks (logout, a profile, the tenant, external-login routes) is ours beside them.
- **`AccountUser`** is Identity's user plus `TenantId`, `DisplayName` and `CreatedAt`; `AccountUser.New` refuses a
  blank tenant, since nothing runs without one. **`Tenant`** is an id, a name and the host name it owns.
- **Storage** is EF Core on SQL Server: the Docker `puls-sql` on the PC, Azure SQL in the cloud, the same code.

Built step by step in F61.1; this doc grows with each step.

## Structure

```
src/hosts/louis-agent.accounts/
├── Messages/        # AccountErrorMessage: what the library throws (ADR 0009)
├── Migrations/      # what `dotnet ef migrations add` wrote; a tool's record, not sized by the file rule
├── Store/           # AccountsDbContext (Identity's tables, Tenants, the per-tenant indexes), the design-time factory
└── Users/           # AccountUser, Tenant
src/hosts/louis-agent.accounts.contracts/   # the records the routes send and read
src/tests/hosts/louis-agent.accounts.tests/             # pure tests
src/tests/hosts/louis-agent.accounts.integration-tests/ # against the Docker SQL Server (ACCOUNTS_SQL_TEST_URL)
```

**Migrations.** The schema is EF Core migrations in `Migrations/`, applied by the host at start-up. A schema change is
`dotnet ef migrations add {Name} --project src/hosts/louis-agent.accounts --startup-project src/hosts/louis-agent.accounts
--output-dir Migrations --namespace LouisAgent.Accounts.Migrations` (`dotnet tool restore` first: `dotnet-ef` is in
`.config/dotnet-tools.json`). The tool never connects; it reads the model from the code.

**On the PC** the server is `puls-sql` in the Puls compose: SQL Server Developer edition on a Docker volume, its sa
password in `config/.env.sql`. The integration tests reach it at `ACCOUNTS_SQL_TEST_URL`
(`Server=127.0.0.1,1433;User Id=sa;Password=…;TrustServerCertificate=true`) and use the database `puls_accounts_test`.

## Tests

`src/tests/hosts/louis-agent.accounts.tests` (no server) and `louis-agent.accounts.integration-tests`, which open the
SQL Server at `ACCOUNTS_SQL_TEST_URL` and skip honestly without it.

## Related docs

[louis-agent.api](louis-agent.api.md) · [louis-agent.web](louis-agent.web.md) · [ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
