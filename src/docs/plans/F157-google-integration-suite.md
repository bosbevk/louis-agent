# F157 The Google integration suite

A plan for a parent with seven children. It covers how a business connects its Google account once, and how its
agent then reads the business's sheets and books into its calendar, with Drive and Gmail after that. It also covers
how louis-agent.dev shows all of this in a new Integrations section. This file holds what the children share: what
was decided, how a connection works, the order, and what each child delivers. Each child gets its own plan, with its
own steps, when it starts.

## Context

Louis asked for a section of the site that shows what the framework plugs into (2026-11-17: "I want to make an
integration section, what integration points can I build for that showcase?"), and then picked the biggest of the
ideas: "cool yeah let's go for full google integration suite". A business that runs on Google keeps its bookings in
Google Calendar and its price lists, menus and stock in Google Sheets. An agent that can read those and book into
that calendar is the most common thing a small business asks for. The showroom's own notes already promise it ("we
could integrate with the booking system if they have one", ADR 0115).

### What was looked up

Google runs its own MCP servers for Gmail, Drive, Docs, Sheets, Slides, Calendar, Chat and People, at
`https://<product>mcp.googleapis.com/mcp/v1`. They use OAuth per person and streamable HTTP
([Configure the Google Workspace MCP servers](https://developers.google.com/workspace/guides/configure-mcp-servers)).
They are a **Developer Preview**. Google's guide asks for membership of its preview programme, while its blog of
1 May 2026 says the preview is open to all developers
([Workspace updates](https://workspaceupdates.googleblog.com/2026/05/agent-tools-and-security-updates-for-workspace-developers.html)).
Neither page says whether a plain @gmail.com account works. Our side can't talk to them yet either: our MCP client
is F125.8, which hasn't started.

Google's .NET client is one package per API: `Google.Apis.Sheets.v4`, `Google.Apis.Calendar.v3`, `Google.Apis.Drive.v3`
and `Google.Apis.Gmail.v1`. Each stands on `Google.Apis` and `Google.Apis.Auth`. Calendar's is version 1.78.0.4320,
last updated on 10 November 2026, Apache-2.0, and it runs on .NET Standard 2.0 and .NET 6 or later
([NuGet](https://www.nuget.org/packages/Google.Apis.Calendar.v3/)). The other three were not opened. Each child's
plan reads its own package page before its first step.

To act for a business while nobody is at the browser, the app asks for **offline access**. Google then returns a
refresh token, which is a long-lived key the app trades for a short-lived access token whenever it needs one. With
`include_granted_scopes`, the app can ask for one more permission later without asking again for the ones it
already holds. That is **incremental authorization**. A person may also grant only some of what was asked, so the
app must check what it got
([Using OAuth 2.0 for Web Server Applications](https://developers.google.com/identity/protocols/oauth2/web-server)).
Calendar's free/busy query is `POST https://www.googleapis.com/calendar/v3/freeBusy`, and it accepts the narrow
`calendar.events.freebusy` and `calendar.freebusy` scopes
([freebusy.query](https://developers.google.com/workspace/calendar/api/v3/reference/freebusy/query)).

Google sorts scopes into non-sensitive, sensitive and restricted. Reading mail, and the whole-Drive scopes
(`drive`, `drive.readonly`), are **restricted**. They need Google's security assessment before the public can use
them. `drive.file`, which only reaches files the app made or the person opened with it, is non-sensitive
([Requesting minimum scopes](https://support.google.com/cloud/answer/13807380)). An app that asks for a sensitive or
restricted scope and isn't verified is held to 100 new users
([Unverified apps](https://support.google.com/cloud/answer/7454865)). **Not confirmed:** how Google classes the
Sheets and Calendar scopes. F157.7 checks that before it plans the verification.

The repo already holds most of the plumbing. The accounts library signs people in with Google on Microsoft's own
handler (`src/hosts/LouisAgent.Accounts/External/Providers/GoogleProvider.cs`), but it asks Google for no API
permissions. Its database has Identity's `AspNetUserTokens` table, keyed by user, provider and name, and nothing
writes to it yet. Every account has a tenant (ADR 0088). The host picks the tenant from its own name
(`HostTenantResolver`), and a session records who opened it (`SessionOpener`: a user id and a tenant). A pack's tools
see only `IPackContext`. The engine builds that context, and the tools, **once per toolset list**
(`AgentEngineFactory`), and every chat shares them. So a tool can't hold "this chat's business" as a field. It has to
read it on each call, the same way `InATrial` reads `SessionReplay.Current` today.

One gap is found: **FX66** is still open. Both Azure apps keep their data-protection keys inside the container, so a
cold start makes new keys. A refresh token protected with the old keys could then never be read again. FX66 must be
merged before F157.1 stores its first token.

### What Louis picked

Six choices were put to him, one card at a time, on 2026-11-17.

| The choice | Louis's pick |
|---|---|
| Our own tools, or Google's MCP servers | Our own packs on Google's .NET client. Google's servers are a preview and wait for F125.8. Our packs stay convertible to MCP (ADR 0028), so we can still offer them that way. |
| How a business connects | OAuth per business with an offline refresh token, each permission asked only when its piece is switched on. A service account was the other way; it can't reach a gmail.com person's mail at all. |
| Which piece first | Reading Sheets, then Calendar bookings. Drive and Gmail follow, in the order the buyer question decides. |
| Where the refresh token is kept | Identity's token table in the accounts database, encrypted with Data Protection. Packs see only a small interface. |
| What the Integrations section shows | Both: a card per integration, with Google's linked to a showroom business that reads its sheet and books, on our own demo Google account, and later visitors connecting their own Google ("A and C"). |
| Whose Google a visitor's chat uses | The business's, by its tenant. The host names the tenant today; after F43 and F131, the site's door names it. |

He also said (2026-11-17): "you will need to register for it to work". Registering the app with Google is a one-time
setup on his side, listed under *What Louis does once*.

## Design

### 1. What a business and a visitor get

The owner of a business signs in on the account pages and presses **Connect Google**. Google shows its consent
screen and asks only for what the switched-on pieces need: first, reading their sheets. The owner says yes and comes
back to a page that says Google is connected, what it may do, and a button to disconnect. Switching Calendar on later
asks Google for that one permission more, and nothing is asked twice.

A visitor never sees any of this. They chat with the business's agent on its website, ask for a room next weekend,
and the agent reads the rates from the business's sheet and the free nights from its calendar. When the visitor says
yes, it books, once. The refresh token never leaves the accounts library. The pack asks for an access token that
lasts about an hour, for one scope, for the turn's tenant, and gets nothing when the business hasn't connected.

![The owner connects once on the accounts pages; after that every visitor's chat reaches that business's Google through the pack, and the refresh token stays in the accounts library](diagrams/F157-google-integration-suite-architecture.svg)

### 2. A connection belongs to a tenant, and a pack asks for a token

`IPackContext` gains two things in F157.1. The first is `Connections`, an `IConnections` that answers "give me an
access token for this service, this tenant and these scopes". The second is `Tenant`, the tenant of the turn now
running, read on each call. The contract names a service by an enum, `ConnectedService`, so the engine and the
composition never name Google in a type (ADR 0027: core names no product). Google appears only as that enum's member
and inside the pack.

The accounts library implements the interface as `AccountConnections`. It reads the tenant's refresh token from
`AspNetUserTokens`, unprotects it with Data Protection, trades it at Google's token endpoint, and keeps the access
token in memory until it expires. A host without accounts gets `NoConnections`, which answers every ask with
nothing, so a pack on it reports the toolset as not connected instead of failing.

In pattern terms, `IConnections` is a **Separated Interface** (Fowler, PoEAA): the contract sits in the small
composition package, and the code that implements it sits in the accounts library, so a pack never references the
accounts host. The pack's own wrapper around Google's client is a **Gateway** (Fowler): one class that turns our
records into Google's calls and back, so the rest of the pack never sees Google's types
([Gateway](https://martinfowler.com/articles/gateway-pattern.html)). The turn's tenant is an **Ambient Context**, a
value set at the start of a turn and read by whatever runs inside it. The engine already does this with
`SessionReplay.Current`, on an `AsyncLocal`, and F157.1 does the same.

![A pack never sees a refresh token: it asks the connections for a short-lived access token for the turn's tenant and one scope](diagrams/F157-google-integration-suite-interface-design.svg)

### 3. One turn, in order

The visitor asks; the agent calls `ReadSheet`; the tool asks the connections for a token; the accounts library reads
and unprotects the refresh token and trades it at Google; the tool reads the rows. The free/busy call that follows
reuses the same access token. The booking waits for the visitor's yes, as a lead does ([ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md)).

![The tool asks for a token for the turn's tenant and one scope; only the accounts library holds the refresh token, and a booking waits for the visitor's yes](diagrams/F157-google-integration-suite-communication-a-visitor-asks-for-a-room.svg)

### 4. What is in memory

The tools are built once per engine and shared by every chat. They hold the context, never a tenant. The access
tokens are cached per tenant and scope inside `AccountConnections`, and the turn's tenant sits in an `AsyncLocal` that
the session sets when a turn starts.

![The tools are built once per engine and read the tenant from the context on each call; the access token is cached per tenant and scope](diagrams/F157-google-integration-suite-objects-in-memory-one-turn.svg)

### 5. What a tool may do, held in code

A booking is a write into a real person's calendar from a stranger's chat, so its limits are in code, not in the
prompt. `BookTime` books only on the one calendar the business named, only inside its opening hours, only into a time
`FindFreeTimes` found free, and only once a conversation. Reading is limited to the sheets the business named in its
settings, never "any sheet in the account". A sheet's cells are the business's data, but a visitor may have typed
them (a booking form that writes to a sheet), so they go back to the model as data in a table, never as instructions.
Each child's plan writes the test for its own limits first.

### 6. One pack, a toolset per piece

The suite is one pack, `louis-agent.pack.google`, with a toolset per piece: `google-sheets` (F157.2),
`google-calendar` (F157.3), `google-drive` (F157.5) and `google-gmail` (F157.6). One pack means one project, one
family of Google packages and one Gateway, while an identity still names only the toolsets it needs (ADR 0006). Each
toolset's `MissingSettings` reports "not connected" when the turn's tenant has no token for its scope.

### Namespace map

| Type | Folder | Namespace | Child |
|---|---|---|---|
| `IConnections`, `ConnectionAsk`, `ConnectionToken`, `ConnectedService`, `NoConnections` | `src/engine/LouisAgent.Composition/Connections/` | `LouisAgent.Composition.Connections` | F157.1 |
| `IPackContext` (changed: `Connections`, `Tenant`) | `src/engine/LouisAgent.Composition/Packs/` | `LouisAgent.Composition.Packs` | F157.1 |
| `TurnTenant` (the `AsyncLocal`), `ToolsetContext` (changed) | `src/engine/LouisAgent.Core/Agent/Toolsets/` | `LouisAgent.Core.Agent.Toolsets` | F157.1 |
| `AccountConnections`, `ConnectHandlers`, `ConnectionScopes` | `src/hosts/LouisAgent.Accounts/Connections/` | `LouisAgent.Accounts.Connections` | F157.1 |
| The Connect Google page | `src/web/LouisAgent.Web.Accounts/` | `LouisAgent.Web.Accounts` | F157.1 |
| `GooglePack`, `GoogleToolset`, `GoogleClients` | `packs/tools/louis-agent.pack.google/` | `LouisAgent.Packs.Google` | F157.2 |
| `SheetsTools` | `packs/tools/louis-agent.pack.google/Sheets/` | `LouisAgent.Packs.Google.Sheets` | F157.2 |
| `CalendarTools` | `packs/tools/louis-agent.pack.google/Calendar/` | `LouisAgent.Packs.Google.Calendar` | F157.3 |
| The pack's tests | `packs/tests/louis-agent.pack.google.tests/` | `LouisAgent.Packs.Google.Tests` | F157.2 |
| The Integrations page and its cards | `src/site/LouisAgent.Site/Pages/` | `LouisAgent.Site.Pages` | F157.4 |

The names are proposals. Each child's plan reads the code again and settles them, and a shape the plan doesn't
settle goes to Louis as a finding.

### Folder trees

Today:

```
packs/tools/   calculator  devops  docs  dotnet  files  git  leads  paymo  recipes  shell  shop-prices  site  triage  web  where-to-buy
packs/tests/   (a test project per pack)
src/hosts/LouisAgent.Accounts/   AccountsStartup.cs  Config/  Endpoints/  External/  Mail/  Messages/  Migrations/  Store/  Tenants/  Users/
```

After F157.3:

```
packs/tools/   ...  louis-agent.pack.google/     (new: GooglePack.cs, GoogleClients.cs, Sheets/, Calendar/, prompts/toolsets/)
packs/tests/   ...  louis-agent.pack.google.tests/
src/hosts/LouisAgent.Accounts/   ...  Connections/   (new: AccountConnections, ConnectHandlers, ConnectionScopes)
src/engine/LouisAgent.Composition/   ...  Connections/   (new: the contract)
```

### What is reused

| Piece | Reused from |
|---|---|
| The OAuth client | The Google Cloud OAuth client "Sign in with Google" already uses, with a second redirect address |
| Storing the refresh token | Identity's `AspNetUserTokens` table and `UserManager`'s token methods, already in the accounts database |
| Encrypting it | ASP.NET Core Data Protection, with its keys on the app's share once FX66 is merged |
| Which tenant | `HostTenantResolver` and the tenant every account has (ADR 0088) |
| Calling Google | `Google.Apis.Sheets.v4`, `Google.Apis.Calendar.v3` and, later, `.Drive.v3` and `.Gmail.v1`, Apache-2.0. **New packages**: Louis picked Google's own client on 2026-11-17, and each child names its exact package and version in its plan |
| A write only on a yes | The lead's pattern (ADR 0101) |
| The section's look | The framework's `Band`, `CardDeck` and `Card` (ADR 0125), and the showroom (ADR 0115) |
| Offering the tools over MCP | The pack stays convertible (ADR 0028); nothing new |

## Steps

A parent's steps are its children. Each is its own plan, branch, pull request to `main`, done-when and write-up. It
holds at most about ten steps, and its plan is written when it starts.

| Child | What it delivers | Done when |
|---|---|---|
| (before) **FX66** | Data-protection keys on each app's share | A cold start reads a cookie and a token protected before it |
| **F157.1** Connect Google | The contract (`IConnections`, the turn's tenant on `IPackContext`), `AccountConnections` over `AspNetUserTokens`, the Connect and Disconnect flow on the account pages, offline and one scope at a time, and the scopes Google actually granted checked | A tenant's owner connects, the token is stored protected, a test pack gets an access token for that tenant and none for another, and disconnecting revokes it at Google |
| **F157.2** Read a sheet | `louis-agent.pack.google` with `google-sheets`: `ListSheets` and `ReadSheet` over the sheets the business named, results as a table, "not connected" when it isn't, and an eval of a guesthouse's rates | The eval passes against a recorded sheet, and a live read works on the demo account |
| **F157.3** Free times and a booking | `google-calendar`: `FindFreeTimes` (freeBusy) and `BookTime`, held in code to one calendar, opening hours, a free time and once a conversation, on the visitor's yes | The eval passes, including the refusals: a busy time, outside hours, a second booking |
| **F157.4** The Integrations section | `/integrations` on louis-agent.dev: a card per integration we have (Google, Resend mail, MCP, Rider over ACP, the HTTP API, three model providers, OpenTelemetry, Paymo, Azure DevOps), with Google's linked to the guesthouse in the showroom reading its sheet and booking on our demo account, its conversation recorded again (ADR 0115), and the page in the place index | The page is live and checked at 1920 by 1080 and at 390 px, and the showroom's guesthouse books |
| **F157.5** Drive | `google-drive` on `drive.file` only (non-sensitive): the files the business picked or the app made | Proposed; its order after F157.6 or before it follows the buyer question |
| **F157.6** Gmail | `google-gmail`: a reply drafted for the owner to send, never sent by the agent; restricted scopes, so Google's security assessment comes first | Proposed, after the buyer question |
| **F157.7** Visitors connect their own Google | Google's app verification (the classification of each scope checked first), the 100-user cap until then, a privacy notice (POPIA, F145), and a try-it on the Integrations section with a visitor's own sheet | Proposed, after F157.4 |

The order of F157.5 and F157.6 waits for the buyer question in the thread *Plan PR to get noticed after no replies*:
.NET teams want the pattern and the code, while a small business wants mail and bookings.

### Proposed ADR

**ADR 0126: A connection to someone's account belongs to a tenant, and a pack asks for a token.** The rule: a pack
never holds a refresh token or a person's credentials. It asks `IConnections` for a short-lived access token for the
turn's tenant and one scope, and the accounts library is the only place a refresh token is read. It is proposed here
in a few lines and written only on Louis's word, in F157.1.

## Verification

Each child writes its tests first and sees them fail. F157.1's are in the accounts tests, with Google's token endpoint
faked, and in core's, for the turn's tenant. The packs' tests use a fake Gateway, and the evals use recorded answers
from Google. The free checks by hand are Connect, Disconnect and a read on the demo account, and the page at both
sizes.

Paid runs: each pack's eval runs once live on the demo account. That is a test call of an agent, which Louis allowed
on 2026-11-09, and its cost is reported after. Google's APIs cost nothing at this volume (not confirmed per API; each
child checks its quota page).

### What Louis does once

1. In the Google Cloud project that holds the "Sign in with Google" client, enable the **Google Sheets API**, and the
   **Google Calendar API** when F157.3 starts.
2. On the OAuth consent screen, add the Sheets read-only scope (Calendar's later), and add the demo Google account
   as a test user.
3. On the OAuth client, add the redirect address F157.1 names for Connect Google, for `localhost` and for
   louis-agent.dev.
4. Make the demo account's sheet and calendar for the showroom's guesthouse (F157.2 and F157.3 say what goes in
   them).
5. Before F157.7: submit the app for Google's verification.

## Concepts

| Child | What it teaches | GOAL.md skill |
|---|---|---|
| F157.1 | Delegated authorization: OAuth's code flow, offline access, incremental scopes, a secret kept encrypted and asked for by a short-lived token | 15 Safety and security |
| F157.1 | A value set per turn and read inside it (Ambient Context), because the tools are shared across chats | 9 Sessions and state |
| F157.2 | A Gateway over an outside API, and a tool's result shaped for the model (a table, not Google's JSON) | 4 Tool design |
| F157.3 | A write held in code: what the prompt can't promise, the tool refuses | 15 Safety and security, 16 Human in the loop |
| F157.2, F157.3 | Evals over a tool that calls someone else's service, recorded and then live | 12 Evaluation |
| F157.4 | Showing integrations as a selling point; the same pack offered over MCP | 11 Protocols |

## Approved during the build

Empty.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [TODO](../../../TODO.md) · [ADR 0088](../adr/0088-a-tenant-on-every-account.md) · [ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md)
