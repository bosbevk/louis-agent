# 0088. A tenant on every account, resolved from the host name

**Status:** Accepted · **Date:** 2026-11-07 (Louis: "make sure while building we take tenants into account, cause
companies could be tenants on business.mypuls.co.za, where this login is more for mypuls.co.za") · **Recorded:** 2026-11-07

## Context

The public site has people; a business site will have companies, each with its own people, some of whom have an
account on the public site too with the same email. Identity makes an email unique across its whole table. Adding
tenants later would mean touching every lookup and every row; a library that does it (Finbuckle.MultiTenant) now asks
a maintenance fee for revenue use, and Louis wants nothing paid.

## Decision

**Every account is in a tenant, and the tenant comes from the request's host name.**

- `Tenant` is a row: an id, a name and the host it owns. `AccountUser` carries `TenantId`. An email and a user name
  are unique within a tenant, not across the table: Identity's two indexes are made again with the tenant in front.
- The request's tenant is the row whose host matches (lower case, no port, no leading `www.`), else the instance's
  default (`ACCOUNTS_TENANT`, made as a row at start-up). `ITenantResolver` is the seam (Strategy); by host is the
  one implementation today.
- The user store looks up an email or a name within the request's tenant and stamps a new account with it, so
  Identity's own routes land in the right tenant without knowing tenants exist. Without a tenant the store refuses
  rather than landing in the wrong one. An id and an external login stay global.
- Tenancy is data, not deployment: one database, one host, and a second tenant is a row.

## Consequences

- A business site is a tenant row with its host; its people register as anyone does. What it still needs (who may
  join a company, roles inside it) is later work on the same column.
- One Google (or other provider) account links in one tenant only, since Identity keys a login by provider and key
  across the table.
- A test never uses `puls` as a tenant id: it's an identity's wire name, and the names rule flags the string.
- `localhost` and the public site share the default tenant, so a developer's accounts and a user's are the same shape.

## Links

[F61 (history)](https://github.com/bosbevk/louis-agent/blob/20f28c9f/src/docs/plans/F61-accounts-and-azure.md) · [0087](0087-identity-is-the-identity-server.md) · [0043](0043-repeated-names-are-enums.md) ·
[louis-agent.accounts](../projects/louis-agent.accounts.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
