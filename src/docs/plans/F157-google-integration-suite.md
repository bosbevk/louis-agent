# F157 The Google integration suite

A plan for a parent with four children. It covers how a business connects its Google account once, and how its
agent then reads the business's sheets and takes booking requests into its calendar. It also covers
how louis-agent.dev shows all of this in a new Integrations section. This file holds what the children share: what
was decided, how a connection works, the order, and what each child delivers. Each child gets its own plan, with its
own steps, when it starts.

![F157 at a glance: four children built in order, Connect Google, read a sheet, free times and a booking request, and the Integrations section, with Drive, Gmail and visitors' own Google as later rows in the TODO](diagrams/F157-google-integration-suite-overview.svg)

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
([Unverified apps](https://support.google.com/cloud/answer/7454865)). The Sheets scopes, read-only too, are
sensitive ([Sheets API scopes](https://developers.google.com/workspace/sheets/api/scopes)). **Not confirmed:** how
Google classes the Calendar scopes. That is checked before the app is sent for verification.

The repo already holds most of the plumbing. The accounts library signs people in with Google on Microsoft's own
handler (`src/hosts/LouisAgent.Accounts/External/Providers/GoogleProvider.cs`), but it asks Google for no API
permissions. Its database has Identity's `AspNetUserTokens` table, keyed by user, provider and name, and nothing
writes to it yet. Every account has a tenant (ADR 0088). The host picks the tenant from its own name
(`HostTenantResolver`), and a session records who opened it (`SessionOpener`: a user id and a tenant). A pack's tools
see only `IPackContext`. The engine builds that context, and the tools, **once per toolset list**
(`AgentEngineFactory`), and every chat shares them. So a tool can't hold "this chat's business" as a field. It has to
read it on each call, the same way `InATrial` reads `SessionReplay.Current` today.

One thing has to be fixed first. The site's host keeps its data-protection keys, the keys that encrypt a stored
token, inside its container, and nothing in the code saves them anywhere else. The container sleeps when nobody
visits, so a cold start makes new keys, and a refresh token protected with the old ones could never be read again.
The keys have to be kept on the app's share before F157.1 stores its first token. That is a fix of its own, called
*the keys fix* below. It was FX66, whose row has since gone to Puls's own TODO, so here it needs a new fix row.

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
yes, it puts a booking request in the business's calendar, once, and the owner confirms it. The refresh token never leaves the accounts library. The pack asks for an access token that
lasts about an hour, for one scope, for the turn's tenant, and gets nothing when the business hasn't connected.

![The owner connects once on the accounts pages; after that every visitor's chat reaches that business's Google through the pack, and the refresh token stays in the accounts library](diagrams/F157-google-integration-suite-architecture.svg)

### 2. A connection belongs to a tenant, and a pack asks for a token

`IPackContext` gains two things in F157.1. The first is `Connections`, an `IConnections` that answers "give me an
access token for this service, this tenant and these scopes". The second is `Tenant`, the tenant of the turn now
running, read on each call. The contract names a service by a small record, `ConnectedService`: the service's wire
name and its token address. The pack builds it from an enum of its own, so the engine and the composition never
learn which services exist and name no product in a type (ADR 0027: core names no product), and the choice is
still an enum where it is made (ADR 0024). Google appears only inside the pack.

The accounts library implements the interface as `AccountConnections`. A tenant has one owner, a user the admin sets, and
only the owner sees Connect Google. The tenant's refresh token is the owner's row in `AspNetUserTokens`, which is
keyed by user. `AccountConnections` reads that row, unprotects it with Data Protection, trades it at Google's token endpoint, and keeps the access
token in memory until it expires. A host without accounts gets `NoConnections`, which answers every ask with
nothing, so a pack on it reports the toolset as not connected instead of failing.

In pattern terms, `IConnections` is a **Separated Interface** (Fowler, PoEAA): the contract sits in the small
composition package, and the code that implements it sits in the accounts library, so a pack never references the
accounts host. The pack's own wrapper around Google's client is a **Gateway** (Fowler): one class that turns our
records into Google's calls and back, so the rest of the pack never sees Google's types
([Gateway](https://martinfowler.com/articles/gateway-pattern.html)). Each service's Gateway is an interface,
`ISheets` and `ICalendar`, with Google's client behind one implementation and canned rows behind another, which a
trial of an eval uses. That second one is a **Service Stub** (Fowler): a stand-in for a service outside our
control, so a test doesn't wait on it ([Service Stub](https://martinfowler.com/eaaCatalog/serviceStub.html)). The turn's tenant is an **Ambient Context**, a
value set at the start of a turn and read by whatever runs inside it. The engine already does this with
`SessionReplay.Current`, on an `AsyncLocal`, and F157.1 does the same.

A tenant is a host name today. On louis-agent.dev the showroom's three businesses and Orbi share one host, so
they share one tenant and one Google connection, the demo account's. That is enough for the showroom. Telling
several businesses on one host apart waits for F131's door per site.

![A pack never sees a refresh token: it asks the connections for a short-lived access token for the turn's tenant and one scope](diagrams/F157-google-integration-suite-interface-design.svg)

### 3. One turn, in order

The visitor asks; the agent calls `ReadSheet`; the tool asks the connections for a token; the accounts library reads
and unprotects the refresh token and trades it at Google; the tool reads the rows. The free/busy call that follows
reuses the same access token. The booking request waits for the visitor's yes, as a lead does ([ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md)).

![The tool asks for a token for the turn's tenant and one scope; only the accounts library holds the refresh token, and a booking request waits for the visitor's yes](diagrams/F157-google-integration-suite-communication-a-visitor-asks-for-a-room.svg)

### 4. What is in memory

The tools are built once per engine and shared by every chat. They hold the context, never a tenant. The access
tokens are cached per tenant and scope inside `AccountConnections`, and the turn's tenant sits in an `AsyncLocal` that
the session sets when a turn starts.

![The tools are built once per engine and read the tenant from the context on each call; the access token is cached per tenant and scope](diagrams/F157-google-integration-suite-objects-in-memory-one-turn.svg)

### 5. What a tool may do, held in code

A booking is a write into a real person's calendar from a stranger's chat, so the agent never makes one. It asks
for one: `RequestBooking` adds a tentative event marked as a request, the owner confirms or deletes it in Google
Calendar, and the visitor is told it is requested, not booked. Its limits are in code, not in the prompt. It writes
only on the one calendar the business named, only inside its opening hours, only into a time `FindFreeTimes` found
free and that is still free when it asks Google again just before the write, and only once a conversation. Reading is limited to the sheets the owner picked when
connecting, never "any sheet in the account", and Google holds that limit itself: the app asks for `drive.file`,
which reaches only the files a person shared with it in Google's own file picker. A sheet's cells are the business's data, but a visitor may have typed
them (a booking form that writes to a sheet), so they go back to the model as data in a table, never as instructions.
Each child's plan writes the test for its own limits first.

### 6. One pack, a toolset per piece

The suite is one pack, `LouisAgent.Packs.Google`, with a toolset per piece: `google-sheets` (F157.2),
`google-calendar` (F157.3). Drive and Gmail, when a buyer asks for them, are two more toolsets in it (F173, F174). One pack means one project, one
family of Google packages and one Gateway, while an identity still names only the toolsets it needs (ADR 0006). Each
toolset's `MissingSettings` says only whether the host is set up for Google, which means its client id and secret
are set. That is asked once for the whole host, outside any turn, so it can't know about one business. "This
business hasn't connected Google" is the tool's own answer when it is called, a message the agent passes on.

### Namespace map

| Type | Folder | Namespace | Child |
|---|---|---|---|
| `IConnections`, `ConnectionAsk`, `ConnectionToken`, `ConnectedService`, `NoConnections` | `src/engine/LouisAgent.Composition/Connections/` | `LouisAgent.Composition.Connections` | F157.1 |
| `IPackContext` (changed: `Connections`, `Tenant`) | `src/engine/LouisAgent.Composition/Packs/` | `LouisAgent.Composition.Packs` | F157.1 |
| `TurnTenant` (the `AsyncLocal`), `ToolsetContext` (changed) | `src/engine/LouisAgent.Core/Agent/Toolsets/` | `LouisAgent.Core.Agent.Toolsets` | F157.1 |
| `AccountConnections`, `ConnectHandlers`, `ConnectionScopes` | `src/hosts/LouisAgent.Accounts/Connections/` | `LouisAgent.Accounts.Connections` | F157.1 |
| The Connect Google page | `src/web/LouisAgent.Web.Accounts/` | `LouisAgent.Web.Accounts` | F157.1 |
| `GooglePack`, `GoogleToolset`, `GoogleClients` | `packs/tools/LouisAgent.Packs.Google/` | `LouisAgent.Packs.Google` | F157.2 |
| `SheetsTools` | `packs/tools/LouisAgent.Packs.Google/Sheets/` | `LouisAgent.Packs.Google.Sheets` | F157.2 |
| `CalendarTools` | `packs/tools/LouisAgent.Packs.Google/Calendar/` | `LouisAgent.Packs.Google.Calendar` | F157.3 |
| The pack's tests | `packs/tests/LouisAgent.Packs.Google.Tests/` | `LouisAgent.Packs.Google.Tests` | F157.2 |
| The Integrations page and its cards | `src/site/LouisAgent.Site/Pages/` | `LouisAgent.Site.Pages` | F157.4 |

The names are proposals. Each child's plan reads the code again and settles them, and a shape the plan doesn't
settle goes to Louis as a finding.

### Folder trees

Today:

```
packs/tools/   LouisAgent.Packs.Calculator  .Docs  .DotNet  .Files  .Git  .Leads  .Shell  .Site  .Triage  .Web
packs/tests/   (a test project per pack, LouisAgent.Packs.{Name}.Tests)
src/hosts/LouisAgent.Accounts/   AccountsStartup.cs  Config/  Endpoints/  External/  Mail/  Messages/  Migrations/  Store/  Tenants/  Users/
```

After F157.3:

```
packs/tools/   ...  LouisAgent.Packs.Google/     (new: GooglePack.cs, GoogleClients.cs, Sheets/, Calendar/, toolsets/)
packs/tests/   ...  LouisAgent.Packs.Google.Tests/
src/hosts/LouisAgent.Accounts/   ...  Connections/   (new: AccountConnections, ConnectHandlers, ConnectionScopes)
src/engine/LouisAgent.Composition/   ...  Connections/   (new: the contract)
```

### What is reused

| Piece | Reused from |
|---|---|
| The OAuth client | The Google Cloud OAuth client "Sign in with Google" already uses, with a second redirect address |
| Storing the refresh token | Identity's `AspNetUserTokens` table and `UserManager`'s token methods, already in the accounts database |
| Encrypting it | ASP.NET Core Data Protection, with its keys on the app's share once the keys fix is merged |
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
| (before) **The keys fix** | The site's host keeps its data-protection keys on the app's share | A cold start reads a cookie and a token protected before it |
| **F157.1** Connect Google | The contract (`IConnections`, the turn's tenant on `IPackContext`), an owner on the tenant set by the admin (one migration), `AccountConnections` over the owner's row in `AspNetUserTokens`, the Connect and Disconnect flow on the account pages with Google's file picker for the sheets the owner shares and the owner's yes to what is read and which model provider it reaches, offline and one scope at a time, the scopes Google actually granted checked, and a broken connection said with its reason, shown with Reconnect and mailed to the owner | A tenant's owner connects, the token is stored protected, a test pack gets an access token for that tenant and none for another, disconnecting revokes it at Google, and a connection Google refuses shows as broken with Reconnect and mails the owner once |
| **F157.2** Read a sheet | `LouisAgent.Packs.Google` with `google-sheets`: `ListSheets` and `ReadSheet` over the sheets the owner picked, on `drive.file`, results as a table of only the range asked and capped in rows, Google's quota refusal answered as a failure with its reason, "not connected" when it isn't, and an eval of a guesthouse's rates on the Gateway's stand-in | The eval passes against the stand-in's sheet, and a live read works on the demo account |
| **F157.3** Free times and a booking request | `google-calendar`: `FindFreeTimes` (freeBusy) and `RequestBooking`, a tentative event the owner confirms, held in code to one calendar, opening hours, a time still free and once a conversation, on the visitor's yes | The eval passes, including the refusals: a busy time, outside hours, a second request |
| **F157.4** The Integrations section | `/integrations` on louis-agent.dev: a card per integration we have (Google, Resend mail, MCP, Rider over ACP, the HTTP API, three model providers, OpenTelemetry), with Google's linked to the guesthouse in the showroom reading its sheet and booking on our demo account, its conversation recorded again (ADR 0115), the page at one address of its own (ADR 0128), its words from a dictionary factory (ADR 0132), the privacy page and the trust page's table saying what happens to Google's data, and the page in the place index | The page is live and checked at 1920 by 1080 and at 390 px, and the showroom's guesthouse books |

Three more pieces are not children of this plan. Drive (F173), Gmail (F174) and visitors connecting their own
Google (F175) are *Later* rows in the TODO, each planned when a buyer asks for it. Which comes first follows the
buyer question in the thread *Plan PR to get noticed after no replies*: .NET teams want the pattern and the code,
while a small business wants mail and bookings.

### Proposed ADR

**A connection to someone's account belongs to a tenant, and a pack asks for a token.** It takes the next free
number when it is written. The rule: a pack
never holds a refresh token or a person's credentials. It asks `IConnections` for a short-lived access token for the
turn's tenant and one scope, and the accounts library is the only place a refresh token is read. It is proposed here
in a few lines and written only on Louis's word, in F157.1.

### Settled before the first child

A review on 2026-11-22 walked the plan as the owner and as a visitor, and read Google's own pages again. It found
ten things the plan had no part for. Louis picked a way for each, one at a time, and the ten picks stand under
*Approved during the build*, each with what it changed and what it costs. What still has to be proved at Google is
said there too, as not confirmed.

## Verification

Each child writes its tests first and sees them fail. F157.1's are in the accounts tests, with Google's token endpoint
faked, and in core's, for the turn's tenant. The packs' tests and the evals use the Gateway's stand-in, with canned
rows and no call to Google, and each pack's eval then runs once live. The free checks by hand are Connect, Disconnect and a read on the demo account, and the page at both
sizes.

Paid runs: each pack's eval runs once live on the demo account. That is a test call of an agent, which Louis allowed
on 2026-11-09, and its cost is reported after. Google's APIs cost nothing at this volume (not confirmed per API; each
child checks its quota page).

**Done when.** The keys fix and the four built children are merged, each with its write-up. The Integrations page
is live on louis-agent.dev. And the showroom's guesthouse reads its sheet and takes a booking request on a connection that
is still working a month after it was made.

### What Louis does once

1. In the Google Cloud project that holds the "Sign in with Google" client, enable the **Google Sheets API** and the
   **Google Picker API**, and the **Google Calendar API** when F157.3 starts. Make an API key for the picker, and
   note the project's number, which the picker takes as its app id
   ([Google](https://developers.google.com/workspace/drive/picker/guides/web-picker)).
2. On the OAuth consent screen, add the `drive.file` scope (Calendar's later), and set the app's publishing
   status to **In production**. In "Testing" every connection ends after seven days. Until Google has verified the
   app, the person who connects sees Google's "unverified app" screen, and the app is held to 100 new users.
3. On the OAuth client, add the redirect address F157.1 names for Connect Google, for `localhost` and for
   louis-agent.dev.
4. Make the demo account's sheet and calendar for the showroom's guesthouse (F157.2 and F157.3 say what goes in
   them).
5. Once F157.2 reads a sheet on the demo account: submit the app for Google's verification.

## Concepts

| Child | What it teaches | GOAL.md skill |
|---|---|---|
| F157.1 | Delegated authorization: OAuth's code flow, offline access, incremental scopes, a secret kept encrypted and asked for by a short-lived token | 15 Safety and security |
| F157.1 | A value set per turn and read inside it (Ambient Context), because the tools are shared across chats | 9 Sessions and state |
| F157.2 | A Gateway over an outside API, and a tool's result shaped for the model (a table, not Google's JSON) | 4 Tool design |
| F157.3 | A write held in code: what the prompt can't promise, the tool refuses | 15 Safety and security, 16 Human in the loop |
| F157.2, F157.3 | Evals over a tool that calls someone else's service, on a stand-in (Service Stub) and then live | 12 Evaluation |
| F157.4 | Showing integrations as a selling point; the same pack offered over MCP | 11 Protocols |

## Approved during the build

**2026-11-22, the demo's connection.** The Google app is published "In production" from the start, unverified, and
is sent for Google's verification once F157.2 reads a sheet. An app in "Testing" loses every authorization seven
days after consent, and its refresh token with it, so the showroom's guesthouse would have stopped reading its sheet
a week after each connect ([Manage app audience](https://support.google.com/cloud/answer/15549945)). What it
costs: Google's "unverified app" screen at consent, which only the person connecting sees, and a cap of 100 new
users for the life of the project until the app is verified. It changes step 2 and step 5 of *What Louis does once*,
and moves the verification from F157.7 to beside F157.2.

**2026-11-22, the scope for reading sheets.** The app asks for `drive.file`, and the owner picks the sheets in
Google's file picker on the Connect page. Google's Sheets scopes, read-only too, are sensitive and "cannot be
limited to a specific sheet", so with them only our own list would have stood between the agent and every
spreadsheet the owner has. `drive.file` is non-sensitive and covers the files "that the user shares with an app
while using the Google Picker API"
([Choose Google Drive API scopes](https://developers.google.com/workspace/drive/api/guides/api-specific-auth)).
What it costs: F157.1 gains the picker, which is two of Google's scripts on the Connect page (from
`apis.google.com` and `accounts.google.com`), an API key and the project's number
([Integrate the Google Picker](https://developers.google.com/workspace/drive/picker/guides/web-picker)). It changes
part 5 of the design, F157.1's and F157.2's rows, and steps 1 and 2 of *What Louis does once*. **Not confirmed,**
two things Google's pages don't say. One is that a sheet picked this way reads through the Sheets API and not only
the Drive API: F157.2's first step proves it, and the plan stops there if it doesn't. The other is whether the
picker takes an access token from our server's sign-in, since Google's page shows only its own script asking for
one in the browser: F157.1's plan settles that before its first step.

**2026-11-22, Google's data and the model.** The owner is asked first, and the tools send the least. Before
Google's own screen, the Connect page says what is read, that it goes to the model to answer the business's
visitors, and who the model's provider is, and the owner ticks yes. A tool returns only the range it was asked
for, capped in rows, never a whole sheet. The privacy page gains Google's Limited Use statement, and the trust
page's table gains Google. What a tool reads reaches the model's provider, and Google's rules for sensitive and
restricted scopes allow a transfer "only with the user's consent" and ask the app to say that it keeps them
([API services user data policy](https://developers.google.com/terms/api-services-user-data-policy)). That a
model call is such a transfer is the review's reading: Google's page names no model provider. What it costs:
F157.1 gains the consent line, F157.2 the cap, and F157.4 the two pages' words. **Not confirmed:** whether those
rules reach `drive.file`, which is non-sensitive, or the Calendar scopes, whose class is not confirmed either. The
consent line is there whichever way that falls.

**2026-11-22, who may connect.** A tenant has one owner. `Tenant` gains the id of one user, set by the admin, only
that user sees Connect Google, and the tenant's connection is that user's row in `AspNetUserTokens`. A tenant is a
host, and everyone who signs up on that host belongs to it, with no owner and no role in use, so on a site where
anyone may sign up anyone could have connected a Google account as the business's. The other ways were Identity's
role tables with an owner role, which allow several owners and are more to wire and test, and a setting that names
the account, which is redone when one host holds many tenants. What it costs: one migration in F157.1, and one
owner only. It changes part 2 of the design and F157.1's row.

**2026-11-22, a request, not a booking.** The agent asks for a booking and the owner confirms it. The tool,
`RequestBooking`, asks Google once more whether the time is free and then adds a *tentative* event marked as a
request. The owner confirms or deletes it in Google Calendar, and the visitor is told it is requested, not booked.
Google's `events.insert` checks no conflict, so two visitors who saw the same free time could both have booked it
([events.insert](https://developers.google.com/workspace/calendar/api/v3/reference/events/insert)), and "once a
conversation" doesn't hold a stranger who opens several chats a day. The other ways were a real booking checked
twice, with a calendar per room or table, and no write at all, with a link to Google's own appointment pages. What
it costs: the owner has one thing to do for every request, and one calendar still means the whole business is busy
or free. It changes parts 1, 3 and 5 of the design, F157.3's name and row, the done-when, and four pictures.

**2026-11-22, when Google says no.** A connection that stops working is said, worked around and reported. The
tool's failure carries its reason ([ADR 0005](../adr/0005-guard-the-context.md)). The agent tells the visitor it
can't check just now and offers to pass their question on as a lead. The Connect page shows the connection as
broken, with a Reconnect button. And the owner gets a mail on the first failure, through the mail the accounts
library already sends. Google lists why a refresh token stops working: the owner revoked it, it went unused for
six months, or the account passed 100 refresh tokens for one client, when the oldest is dropped "without warning"
([OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)). The plan covered a business that never
connected, and had no part for one that worked yesterday. The other ways were the same without the mail, and the
same with a job that checks every connection once a day, which waits for real customers. What it costs: F157.1
gains the broken state and the mail. It changes F157.1's row.

**2026-11-22, sixty reads a minute.** A quota refusal from Google is handled like any other failure, with its
reason, and nothing more is built until a real site comes near the limit. The Sheets API allows 300 reads a minute
for the project and 60 a minute for a user ([Sheets limits](https://developers.google.com/workspace/sheets/api/limits)),
and every visitor of one business reads with that business's one token, so they share its 60. The other ways were
keeping a read in memory for a minute, and keeping a chat's first read for that chat. Both show a changed price
late, and no number asks for them yet ([ADR 0055](../adr/0055-keep-it-simple.md)). What it costs: a busy minute on
one site would turn visitors away with the reason, until a cache is planned. It changes F157.2's row.

**2026-11-22, Google in an eval.** The pack reaches Google through an interface per service, `ISheets` and
`ICalendar`, and a trial of an eval gets a stand-in with canned rows. That is Fowler's Gateway with a Service Stub.
The plan had the evals "use recorded answers from Google", but the engine records only what goes through its own
fetcher, and Google's client makes its own calls, so a trial would have called the real Google. The other ways were
handing Google's client our recording handler, which is truer to Google and rests on a hook nobody has read yet,
and live runs only, which are slow and go red whenever Google or the token does. What it costs: the stand-in's rows
are ours, so an eval proves how the agent uses the tools and not that Google answers as we think. The one live run
per pack covers that. It changes part 2 of the design, F157.2's row, *Verification* and *Concepts*.

**2026-11-22, the pack owns the name.** The contract takes `ConnectedService` as a small record, a service's wire
name and its token address, and the pack builds it from its own enum. The plan had `ConnectedService` as an enum in
the composition with the member `Google`, which names a product in the engine's contracts
([ADR 0027](../adr/0027-engine-identities-and-packs.md)). The other ways were to keep that enum, the simplest and
against the rule, and to take the service out of the contract, so `IConnections` would be Google's alone beside the
pack and a second service would need a second interface. What it costs: the composition can't list the services a
host has, since only the packs know them. It changes part 2 of the design.

**2026-11-22, four children.** The parent is Connect Google, read a sheet, free times and a booking request, and
the Integrations section. Drive, Gmail and visitors connecting their own Google were its fifth, sixth and seventh
children, proposed and with no date. They are now one *Later* row each in the TODO (F173, F174, F175), planned when
a buyer asks. Nobody has asked for them yet, their order waits on the buyer question, and Gmail's scopes are
restricted, which means Google's security assessment before the public can use them. The other ways were to keep
all seven, so the parent could not finish, and to drop Gmail outright. What it costs: the suite is "full" as a
direction and not as this plan's promise. It changes the opening, part 6 of the design, the steps table and the
overview picture, and the TODO and the work log gain three rows.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [TODO](../../../TODO.md) · [ADR 0088](../adr/0088-a-tenant-on-every-account.md) · [ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md)
