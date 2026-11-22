# F171 The bill checker

A plan, approved by Louis on 2026-11-21: a phone app on the framework that reads a Western Cape municipal account from a
photo or a PDF, checks every line against the municipality's own tariffs, says in plain words what looks wrong, and
drafts the written dispute for the resident to check and send. It starts with three municipalities: the City of Cape
Town, Swartland (Malmesbury) and Overstrand (Hermanus). It is a parent with eight children. Each child gets its own
plan, branch, pull request and done-when when it starts; this file says what the whole thing is, why each part is
shaped the way it is, what is still unknown, and the order. The product is built in its own private repository, and
only the framework's parts are built here.

![F171 in eight children: photos reach a tool, the bills pack reads and checks, the dispute letter, the bill checker identity and its evals, the account and the subscription, the phone app, the stores, and the monthly report](diagrams/F171-the-bill-checker-overview.svg)

## Context

Louis, 2026-11-21, starting the project: "next project using our framework, an app on android and ios something
like this: https://www.dadwhatsfordinner.co.za/ but offcourse not this". Then "model must also bee free with
subscription": free to download, one subscription that includes the AI. From ten researched ideas (in the Claude
project's files, `research/reports/Ten phone app ideas on the framework.md`) he picked the bill checker on the
card, and then: "We will do Malmesbury and CPT", "focus on western cape", and "do thorough research on it get
references, what we need and how we will do it. Your goal is to make the plan as complete as can be".

### What the research found

The research notes, with a source and a confidence tag on every fact, are in the Claude project's files, under
`research/bill-checker/` (`research-cpt.md`, `research-swartland.md`, `research-law.md`,
`research-stack.md`, `research-market.md`). The parts that shape the plan:

**The problem is real and the law gives a clear path.** The Municipal Systems Act requires "accurate and verifiable
metering" and "prompt redress for inaccurate accounts" (s95), and says credit control "does not apply" to a dispute
"concerning any specific amount claimed" (s102(2)). The Supreme Court of Appeal in *Croftdene Mall v Ethekwini*
([2011] ZASCA 188) held that a dispute must name a specific item and give facts, not be raised "in general terms",
and the undisputed balance must still be paid. SALGA's advice of 9 September 2026 is the same workflow: lodge it in
writing, keep paying the rest, escalate to the municipal manager and then court
([EWN](https://www.ewn.co.za/2026/09/09/salga-warns-against-settling-incorrect-municipal-bills)). That is exactly
what an agent can do well: find the specific line, show the sum, and write the letter with the facts in it.

**Cape Town's tariffs just changed shape.** On 1 July 2025 the City brought in fixed water, sanitation and
City-wide Cleaning charges set by property value. The Western Cape High Court struck them down on 30 April 2026,
effective 30 June 2026. For 2026/27 the water and sanitation fixed charges are by meter size again (R90.13 a month
for a 12 to 22 mm water meter), the cleaning charge is gone into rates, and the rate in the rand fell to 0.7010 c on
the GV2025 roll. No rate is levied on the first R15,000 of any residential property, and a primary residence worth
under R8,000,001 gets a further R605,000 off
([the City's 2026/27 tariffs book, section 29](https://www.capetown.gov.za/media/jv0nq3z3/valuations_property_rates.pdf)).
So a bill from June 2026 and a bill from July 2026 are worked out by
different rules, and the checker needs a dated tariff table per municipality per year. Water in 2026/27 is stepped
at R21.78 / R29.93 / R47.48 / R95.63 per kL (0–6, 6–10.5, 10.5–35, over 35 kL, excluding VAT); sanitation is
stepped on 70% of the water used, capped at 35 kL; electricity is Lifeline, Domestic or Home User by meter and
property value (Home User pays R368.96 a month plus 309.52 / 407.88 c/kWh around 600 kWh).

**Swartland is simpler and different.** One tariff file a year, changing on 1 July. For 2026/27 (including VAT):
a credit-meter electricity network charge of R502.91 a month with four blocks (0–50, 51–350, 351–600, over 600 kWh
at R2.5366 to R5.4055), prepaid with the same blocks and no network charge, a water network charge of R100.59 with
seven blocks from R8.38 to R134.11 per kL, sewerage R376.54, refuse R253.90, and rates at 0.005831 rand per rand with
the first R15,000 not rated. The budget's household-bill table (National Treasury schedule SA14) gives three worked
monthly bills, R6,474.62 middle income, R3,389.52 affordable and R1,090.78 indigent, which make ready test cases.
Some indigent households get electricity straight from Eskom, so their account has no electricity line at all.

**There is no common tariff format.** Every municipality publishes its budget in National Treasury's A1 schedules,
but the tariff books are each their own PDF. So one reader for "any Western Cape municipality" isn't possible from
the published data; each municipality is a hand-entered, dated table, checked against its SA14 sample bills. That
is why the plan starts with two and makes a third one a data change, not a code change.

**The market.** Cape Town has 1,452,845 households (Census 2022) and Swartland about 40,000. Water and rates are
post-paid, so nearly every formal property gets a bill worth checking. In the City's 2025 survey of 3,400 households,
14% of formal households thought their use was not measured accurately, only 2% had ever complained, and only 38%
of those were satisfied with how it ended; 13% get their bill through a landlord or agent. Only about 2,758 owners a
month submit their own meter readings. Smart water meters are coming, but slowly (83,000 from April 2026 to February
2029, more than 680,000 over ten years).

**The rivals, honestly.** [OurPower](https://ourpower.co.za/tools/municipal-bill-dispute) is a free browser tool that
already builds a dispute letter, checks readings for estimates, explains the escalation and covers the City of Cape
Town (with detailed steps only for Johannesburg). It stores nothing and says it is "not legal advice". Munichecker
is a free iPhone calculator for some metros. d6 group's free app brings clients to a paid dispute service with no
published fee. So a letter alone is not enough to charge for. What nobody does is **read the bill for you, recompute
every line against the right year's tariff, follow the account month after month, and cover a non-metro like
Swartland**. That is what the subscription pays for, and it is put to Louis as finding 4 below. Abroad, Rocket Money
moved its bill negotiation from a success fee into its subscription on 2 November 2026, which supports a subscription
over a fee per win.

**The stack.** .NET 11 and MAUI 11 shipped on 10 November 2026 (MAUI 11 supported to 9 May 2028); Blazor Hybrid is
current, `MediaPicker.CapturePhotoAsync` resizes and compresses on the phone, there is no document scanner built in
(ML Kit's on Android, VisionKit's on iOS), PDFs come through `FilePicker`. Building for iPhone needs a Mac with a
current Xcode (the .NET 11 iOS notes say Xcode 27.2). Plugin.InAppBilling 11.0.1 (14 November 2026, MIT, StoreKit 2)
is maintained but targets net10.0, so a first step proves it builds in a net11 app. Apple's guideline 5.1.2(i), added
13 November 2025, requires the app to say it shares personal data with a third-party AI and get explicit permission
first; both stores require account deletion inside the app. Google takes 15% of subscriptions, Apple 15% under its
Small Business Program; Apple's membership is US$99 a year.

**The model.** Claude reads JPEG, PNG, GIF and WebP up to 8000×8000 px and 10 MB, and PDFs as documents. Haiku 5.5
is on the high-resolution tier (up to 2,576 px on the long edge, at most 4,784 visual tokens an image), so one bill
photo costs at most about 4,800 input tokens, under $0.0005 at our price table's $0.10 per million
(`demos/louis-agent/config/prices.json`, 2026-11-19). The docs warn that rotated, blurry or very small images are read less
accurately, so the app straightens and checks the photo before it sends it.

**What the code has today.** The engine takes a user turn as text: `TurnRunner` adds `new ChatMessage(ChatRole.User,
userInput)` with a string, and an attachment (`AttachmentRequest(string Name, string? Content)`) is text pasted into
the prompt by `TurnPrompt`. Nothing sends an image or a PDF to a model yet, and a pack's tools can't call a model
(`IPackContext` offers the workspace, the browser, search, the fetcher, rates and places). The provider is the
Anthropic SDK's `AsIChatClient`, behind Microsoft.Extensions.AI, whose `DataContent` carries bytes with a media type.
The two patterns for data the agent must not invent already exist: `IDataImport`, the seam an admin's upload goes
through (Puls's shop-prices pack uses it, in Puls's own repository since F166), and the `calculator` pack (sums in
C#, ADR 0080). The identity, pack, eval and ledger pieces all exist; the photo is the one new
engine capability, and it is skill 19 in GOAL.md, still listed as missing.

**What could not be confirmed** (the research hit the shared fetch limit, and some sites refuse downloads): what a
Cape Town and a Swartland account look like, how each marks an estimated reading, the account number formats,
Swartland's credit-control clauses on disputes and interest, Cape Town's ombudsman timelines, whether South Africa is
a Google Play merchant country paying out in rand, and the POPIA and Legal Practice Act points (written from the Acts,
not re-read). Each is named below under the step that confirms it. Most of the bill questions are answered by the
first thing the plan asks Louis for: real bills.

## Design

### 1. How it works, as a story

Anna in Malmesbury opens the app, takes a photo of her Swartland account and one of her water meter. The app
straightens the photo, checks it is sharp, asks once for her consent to send it to the AI that reads it, and uploads
both to our API. The bill checker reads the bill into a typed record (account, period, each line, each reading and
whether it is actual or estimated), picks Swartland's 2026/27 tariffs because the period is after 1 July, and
recomputes every line in C#. Her water line charges 38 kL at an estimated reading, while her meter photo shows
she used 14. The agent tells her that in two sentences, shows the sum, and offers a draft: a letter that calls it a
dispute under section 102(2) of the Municipal Systems Act, names the account, the period, the line, the readings,
the photo, and asks for a corrected account, while she pays the undisputed rest. She checks it, sends it from her own
email, and the app reminds her in 14 days to log the reference number or escalate.

### 2. The children, in order

| Child | What it builds | Done when | About |
|---|---|---|---|
| **F171.1 A photo or a PDF reaches a tool** | The engine: an upload kept with the session as bytes (`ISessionFiles`), and a way for a pack's tool to have the model read a file into a record (`IDocumentReader`), with a ledger row for that call. The API accepts an image or PDF attachment | A test sends a JPEG and a PDF through the real `AsIChatClient` adapter and gets a typed record back; a usage row has the reading's cost | 7 steps |
| **F171.2 The bills pack reads and checks** | The municipal bills pack: `ReadBill`, `CheckBill` and `ExplainLine`; the bill record; the tariffs per municipality per year in MongoDB, uploaded on the admin's import screen (an `IDataImport`, like Stats SA's prices) and checked on the preview; the arithmetic per service in C# | Swartland's three SA14 bills and Cape Town's published examples recompute to the cent from uploaded tariffs, an upload that breaks a sample bill is refused, and an estimated reading is flagged | 10 steps |
| **F171.3 The dispute letter** | `DraftDispute`: a letter from the facts the check found (s102(2) wording, the specific line, the readings, what is asked), each municipality's addresses and escalation steps as data; it never sends | A letter for each of five seeded errors names the account, period, line, amounts and the ask, and says plainly it is a draft for the resident | 5 steps |
| **F171.4 The bill checker and its evals** | The identity: its voice, the guard rails (information, never legal advice; never says a bill is wrong without the sum), and the eval suite of real redacted bills with their known answers | The eval passes on the bills Louis gives, with a measured cost per bill | 6 steps |
| **F171.5 The account, its bills and the subscription** | On the API: a person's bills and checks kept per account (MongoDB through records), deletion and retention, the consent record, the entitlement (store receipts checked on the server) and the free tier's cap | A test account checks one bill free, is refused a second, buys a test subscription in each store's sandbox and checks again; deleting the account deletes its bills | 9 steps |
| **F171.6 The phone app** | `LouisAgent.Web.App` (the framework's MAUI Blazor Hybrid shell any site could use) and the bill checker's app on it: camera and scanner, PDF picker, the chat, the consent screen, the paywall, account deletion; Android first, then iOS on the Mac | The app runs on an Android phone and an iPhone against the hosted API and does Anna's story end to end | 10 steps |
| **F171.7 To the stores** | The store listings, the privacy policy and PAIA manual, the ECTA page, Google's Data safety form, Apple's privacy labels, closed testing and TestFlight, the hosted API in Azure | Both stores approve the app for South Africa | 6 steps |
| **F171.8 The monthly report** | A household report once a month from the readings the account keeps: what changed, what it cost, and what to watch (*Approved during the build*, 2026-11-21) | Set when the child's plan is written | 4 to 5 steps |

**When and where.** Louis, 2026-11-21: "We do this after everything is packages so it will be it's repo". That wait
is over: F166 closed on 2026-11-21 with the framework on GitHub Packages (ADR 0130), the accounts among the
packages. The framework's parts (F171.1's two seams, the subscriptions part of finding 5 and the MAUI shell
`LouisAgent.Web.App`) are built in this repository and ship as new versions of those packages, each by a tag.
Everything that is the product (the bills pack, the identity, its evals, the API, the app, the store work) lives in
its own private repository, `bill-checker`, on the published packages, the way Puls does. Each child's plan, with its own
steps and pictures, is written when the child starts, in the repository its code goes to.

### 3. The pictures

The interface design: the two seams F171.1 adds to the engine, and the pack's tools and records on them.

![The engine adds ISessionFiles and IDocumentReader to the pack context; the bills pack's ReadBill uses them to make a BillReading, CheckBill recomputes it with a dated TariffBook, DraftDispute writes the letter](diagrams/F171-the-bill-checker-interface-design.svg)

The architecture: the phone app, the API in Azure, the engine and its new seams, the pack, the model and the stores.

![The phone app on MAUI talks to the API in Azure; the API runs the bill checker identity on the engine with the bills pack; the pack reads through the model and keeps bills in MongoDB; the stores confirm the subscription](diagrams/F171-the-bill-checker-architecture.svg)

The main case: Anna's bill, from the photo to the draft letter.

![The app uploads the photo, the agent calls ReadBill, which has the model read it into a BillReading, then CheckBill recomputes with Swartland's 2026/27 tariff, and DraftDispute writes the letter](diagrams/F171-the-bill-checker-communication-annas-bill.svg)

What is in memory while the bill is checked.

![Anna's session holds her uploaded files and the conversation; the BillReading, the TariffBook for Swartland 2026/27 and the BillCheck with its findings hang from the tool calls](diagrams/F171-the-bill-checker-objects-in-memory.svg)

### 4. Why this shape, part by part

**The photo goes to a tool, not into the conversation** (finding 1, recommended). The agent's conversation is sent
again on every turn, so a 4,800-token photo in it is paid for on every question afterwards, and a bill read by the
agent mid-chat can't be tested on its own. Instead the photo is kept with the session, and `ReadBill` has the model
read it once into a typed `BillReading` with structured output. The agent then reasons over a record, the reading
gets its own eval, and the sums never depend on the model. This is ADR 0080, a tool when it improves the prompt, and
it keeps the conversation append-only (ADR 0008).

**The sums are C#, the judgement is the agent's.** Tariffs are a step function on a reading, and a model that adds up
steps can be wrong in a way no one notices. So each service (electricity, water, sanitation, refuse, rates) is a
small calculator over a dated tariff table, and `CheckBill` returns findings with the expected and the billed amount.
The agent decides what matters, asks for the meter photo, explains, and drafts. Ask the type (ADR 0123): a tariff
step, a block and a meter size are named types, not numbers in the code.

**Tariffs are dated data per municipality, kept in MongoDB** (finding 2, Louis picked B on 2026-11-21). Because the
books share no format and change every 1 July (and Cape Town's changed shape mid-way), each municipality-year is one
tariff book. An admin uploads it on the import screen the framework already has (`IDataImport`, the same seam the
shop-prices pack uses in Puls's repository for Stats SA's sheets), as our own JSON layout for one municipality and one year. The preview
recomputes that municipality's published sample bills (Swartland's SA14 table) with the new book and refuses the
upload if one is off by a cent, so a typo can't reach a resident. The book is saved through a typed record (ADR 0048)
with the year and the date it was uploaded. A bill's period picks the book; a period across 1 July uses both, split by
days, which is how both municipalities bill. A tariff change, or a third municipality, is an upload, not a release.
The pack's tests keep a copy of each book as a fixture, so the sums are tested without a database.

**The letter is a draft the resident sends.** It never goes out from us, which keeps us outside "acting for" anyone
(the Legal Practice Act point a lawyer should confirm) and outside any duty to the municipality. Its wording follows
*Croftdene*: a specific item, the facts, the word "dispute", s102(2), and the undisputed amount still paid. Each
municipality's address, the escalation order (the municipal manager, Cape Town's ombudsman, the Public Protector,
court) and the City's own e-mail channels are data in the pack, dated like the tariffs.

**The framework gets the parts any site could use.** The upload seam, the document reader and the MAUI shell
(`LouisAgent.Web.App`: a Blazor Hybrid host that runs `LouisAgent.Web`'s chat against an agent API) are framework
parts, because any agent could take a photo or ship as an app ("things that can be shared, must go to framework",
Louis, 2026-11-13, and his pick of a framework app project in the earlier phone-app planning). Municipal bills,
tariffs and letters are the product's.

**What we keep, and for how long.** A bill photo holds a name, an address and an account number, so it is personal
information under POPIA. The app keeps the photo only until it is read (deleted within 30 days, like the reference
app's photo import), keeps the typed reading and the checks while the account lives, and deletes everything with the
account. Consent to send the photo to the AI is asked once and recorded (Apple 5.1.2(i), POPIA s72 for a provider
outside South Africa).

**Free with a subscription.** One bill check a month is free, without the letter. The subscription (R39 a month or
R349 a year, a week free) unlocks every check, the letter, the history and the reminders. The stores bill it; the
server checks each store's receipt and keeps the entitlement on the account. The free tier's cap is counted from the
usage ledger the engine already keeps (ADR 0007). At about R0.10 a reading and a few cents a question, a heavy month
costs well under R10 in model use.

### 5. The namespace map

The types each child adds, by where they sit. The child plans confirm the names against the code when they start.

| Type | Folder | Namespace | Child |
|---|---|---|---|
| `ISessionFiles`, `SessionFile` (an upload kept as bytes with the session) | `src/engine/LouisAgent.Composition/Packs/Files/` | `LouisAgent.Composition.Packs.Files` | F171.1 |
| `IDocumentReader` (a file and a record type in, the record out, one model call) | `src/engine/LouisAgent.Composition/Packs/Files/` | `LouisAgent.Composition.Packs.Files` | F171.1 |
| `ModelDocumentReader` (on `IChatClient` and `DataContent`, with a ledger row) | `src/engine/LouisAgent.Core/Files/` | `LouisAgent.Core.Files` | F171.1 |
| `AttachmentRequest` (bytes and a media type beside the text) | `src/engine/LouisAgent.SessionStream/Wire/Requests/` | `LouisAgent.SessionStream.Wire.Requests` | F171.1 |
| `IPackContext.Files`, `IPackContext.Reader` | `src/engine/LouisAgent.Composition/Packs/` | `LouisAgent.Composition.Packs` | F171.1 |
| `MunicipalBillsPack`, `MunicipalBillsToolset` | `bill-checker: packs/municipal-bills/` | `LouisAgent.Packs.MunicipalBills` | F171.2 |
| `BillReading`, `BillLine`, `MeterReading`, `ReadingKind` (actual, estimated) | `…/Bills/` | `LouisAgent.Packs.MunicipalBills.Bills` | F171.2 |
| `TariffBook`, `TariffStep`, `Municipality`, `TariffYear` | `…/Tariffs/` | `LouisAgent.Packs.MunicipalBills.Tariffs` | F171.2 |
| `TariffImport` (an `IDataImport`: preview, the sample-bill check, save), `TariffBookRecord`, `ITariffStore` | `…/Imports/`, `…/Tariffs/Store/` | `LouisAgent.Packs.MunicipalBills.Imports`, `.Tariffs.Store` | F171.2 |
| `WaterCharge`, `SanitationCharge`, `ElectricityCharge`, `RefuseCharge`, `RatesCharge` | `…/Charges/` | `LouisAgent.Packs.MunicipalBills.Charges` | F171.2 |
| `BillCheck`, `Finding` | `…/Checks/` | `LouisAgent.Packs.MunicipalBills.Checks` | F171.2 |
| `ReadBillTool`, `CheckBillTool`, `ExplainLineTool` | `…/Tools/` | `LouisAgent.Packs.MunicipalBills.Tools` | F171.2 |
| `DisputeLetter`, `Escalation`, `DraftDisputeTool` | `…/Disputes/`, `…/Tools/` | `LouisAgent.Packs.MunicipalBills.Disputes` | F171.3 |
| The bill checker identity (voice, skills) | `bill-checker: identity/` | `LouisAgent.Identities.BillChecker` | F171.4 |
| The evals (bills and their answers, as files) | `bill-checker: evals/` | (files, no C#) | F171.4 |
| `BillRecord`, `CheckRecord` (MongoDB through records), `Entitlement`, `StoreReceiptCheck` | `bill-checker: hosts/api/` | `LouisAgent.BillChecker.Api…` | F171.5 |
| `LouisAgent.Web.App` (the MAUI Blazor Hybrid shell) | `src/web/LouisAgent.Web.App/` | `LouisAgent.Web.App` | F171.6 |
| The bill checker's app | `bill-checker: app/` | `LouisAgent.BillChecker.App` | F171.6 |
| Tests for each, mirroring the projects | `src/tests/…` here, `tests/` there | `….Tests` | each |

The folder tree after F171, for the parts that are new. In this repository:

```
src/engine/LouisAgent.Composition/Packs/Files/      ISessionFiles, IDocumentReader          (F171.1)
src/engine/LouisAgent.Core/Files/                   ModelDocumentReader                     (F171.1)
src/web/LouisAgent.Web.App/                         the MAUI Blazor Hybrid shell            (F171.6)
```

In its own repository, `bill-checker`, on the framework's packages:

```
packs/municipal-bills/        Bills/ Tariffs/ Charges/ Checks/ Disputes/ Imports/ Tools/ prompts/
identity/  evals/  hosts/api/  app/  config/  deploy/  docs/
tests/                        with fixtures/cape-town/2025-26.json, 2026-27.json, swartland/2025-26.json, 2026-27.json
```

### 6. What is reused, and what is new

Reused: the engine's loop, identities and packs, `AgentHost` and the session API, the usage ledger and its price
table, the accounts (F61's sign-in, packaged as `LouisAgent.Accounts` and `LouisAgent.Web.Accounts`), MongoDB
through records (ADR 0048), the eval harness, `LouisAgent.Web`'s chat components, the calculator pack's pattern, and
Azure Container Apps. Two packages are settled by finding 5: Plugin.InAppBilling on the phone and
`Google.Apis.AndroidPublisher.v3` on the server, with Apple's check our own code. One is still open and is put to
Louis with three options before its step: a document scanner binding per platform, or the camera alone.

## The findings to settle first

Each one is put to Louis on its own card, with three options, before the child it belongs to starts. The
recommendation and its reason are here so the plan can be read whole.

1. **How the bill is read** (before F171.1). **Louis picked A on 2026-11-21.** A: a tool reads it once into a typed record with structured output
   (recommended: cheaper every turn after, testable alone, ADR 0080). B: the photo goes into the conversation and the
   agent reads it as it talks (simplest, but paid on every turn and not testable alone). C: an OCR service (Azure
   Document Intelligence) turns it into text first (a second vendor and a second bill, and the layout is lost).
2. **Where the tariffs live** (before F171.2). **Louis picked B on 2026-11-21.** A: a dated JSON file per municipality-year in the pack, tested against
   the published examples (recommended: simple, reviewed in a pull request, versioned with the code). B: MongoDB,
   uploaded through the admin's import screen like Stats SA's prices (changes without a release, but needs a screen
   and a check). C: the model reads the tariff PDF when a bill is checked (no table to keep, but sums from a model
   reading a PDF are what this app exists to check).
3. **Where the app lives** (before F171.4). **Louis picked B on 2026-11-21:** "We do this after everything is packages so it will be it's repo". A: `demos/bill-checker/` in this repo now, moved to its own repository
   when F166.2's packages ship, the way Puls moves (recommended: no waiting, the same path Puls is proving). B: its
   own repository now, on the framework's packages (waits for F166.2). C: inside Puls (Puls is leaving this repo,
   and the app is a different product).
4. **What the subscription sells, given OurPower is free** (before F171.4). **Louis picked A on 2026-11-21:** the whole check, Swartland too. A: the whole check (read the photo,
   recompute every line, keep the history, remind, and Swartland, which no rival covers), with the letter as part of
   it (recommended). B: aim at landlords and agents first, several properties on one account at a higher price (13%
   of Cape Town households are billed through one). C: make the app free and earn nothing for now (the CV value only).
5. **The subscription part, for many apps** (before F171.5). **Louis picked A on 2026-11-21:** our own framework part, our own Apple check (Plugin.InAppBilling on the phone, `Google.Apis.AndroidPublisher.v3` on the server, Apple's JWS verified with .NET's own `X509Chain` and `ECDsa`; no paid service). It is framework work, built in this repository and shipped in the packages, like F171.1's seams. Louis, 2026-11-21: "investigate if we need to build our
   own, cause we will have alot of these apps being produced". Research in
   the research note `research/bill-checker/research-billing.md`. A: our own framework part. The phone side is a seam over
   Plugin.InAppBilling, and a server part verifies (Google's official `Google.Apis.AndroidPublisher.v3`, Apple's JWS
   checked by us), acknowledges within Google's three days, takes both stores' notifications and keeps one
   entitlement per account and app (recommended: written once, configured per app). B: RevenueCat for every app (least
   server code, a net9 community wrapper, 1% above US$2.5k a month per app, purchase data abroad). C: all our own,
   phone bindings too (the most work, and every Apple build needs Xcode for the Swift bridge).
   Louis, 2026-11-21: "we will not pay anything", so B is out: only free packages and our own code. The stores'
   own 15% stays on every subscription bought inside the app.

## What Louis needs to give or do

- **Real bills.** Five Cape Town, five Swartland and five Overstrand accounts, with names, addresses and account numbers blacked out,
  some with an estimated reading if possible, and a meter photo with each. They answer the layout questions, and
  they become the eval set. Without them F171.2 can't finish.
- **A lawyer's hour before launch** (F171.7), on five points from the research: whether the draft letter stays outside
  the Legal Practice Act; POPIA's s72 for a US model provider and whether an account number is a "unique identifier"
  needing prior authorisation (s57); the CPA and ECTA rules for a subscription (cooling-off, the s43 page); the app's
  disclaimer wording; the privacy policy.
- **A company and its registrations**: the stores' seller name and POPIA's responsible party should be a company,
  not Louis himself; register its Information Officer with the Information Regulator.
- **The store accounts**: Apple Developer (US$99 a year) and Google Play (a one-time fee, not confirmed today).
- **A Mac** with a current Xcode for the iPhone build (F171.6), as already planned for the phone apps.
- **A run's go**: each child's paid eval run is small (a bill costs under R0.10 to read), and is reported after.

## Verification

Each step has its test first, seen failing. Free checks by child: F171.1 a JPEG and a PDF through the real adapter
with a fake chat client, and one live reading on Louis's go; F171.2 the published sample bills to the cent, both
years, a period across 1 July, an estimated reading flagged; F171.3 each seeded error's letter holds its facts;
F171.4 the eval over the real bills, with the reading scored separately from the check (accuracy per field) and the
answer by criteria (names the line, shows the sum, says it's a draft, never claims legal advice); F171.5 sandbox
purchases in both stores, the free cap, deletion; F171.6 Anna's story on a real Android phone and an iPhone, the
1920×1080 and 390 checks for the shared chat; F171.7 store review. The full suite runs before each pull request.

## Concepts

| Child | What it teaches | Skill in GOAL.md |
|---|---|---|
| F171.1 | Images and documents as input, and the cost of a visual token | 19 Multimodal (missing today), 13 Cost |
| F171.2 | Extraction into a schema, then code that checks it | 5 Structured outputs, 4 Tool design |
| F171.3 | Writing from facts the code found, never from guesses | 3 Prompt design, 15 Safety |
| F171.4 | An eval of real documents, field accuracy, criteria for an answer | 12 Evaluation |
| F171.5 | Durable state per person, consent, deletion, paying for use | 9 Sessions and state, 15 Safety, 13 Cost |
| F171.6 | An agent behind a phone app, the same components in a new host | 18 Testing and shipping |
| F171.7 | Releasing to the public, and what the law asks | 18 Testing and shipping, 15 Safety |
| F171.8 | A report written from kept readings, never from guesses | 9 Sessions and state, 3 Prompt design |

## The records that go with it

The plan with its pictures, its row in `src/docs/plans/README.md`, a `TODO.md` row listing the eight children, and
its row in the work log's open table with the count. Louis, 2026-11-22: "save merge this plan".

## Approved during the build

- **Overstrand is the third municipality** (Louis, 2026-11-21: "so it will be swartland, cpt and overstrand").
  Overstrand covers Hermanus, Kleinmond, Gansbaai, Stanford, Betty's Bay and Pringle Bay. Its tariffs have not been
  researched yet. Before F171.2's tariffs step, it needs what the other two have: its 2026/27 tariff book, its
  published sample bills to check against to the cent, and real redacted bills for the evals. F171.2's done-when
  and F171.4's eval set take it in when those children's plans are written.

- **Usage tracking, alerts, fair use and a monthly report in the subscription** (Louis, 2026-11-21, his pick B on
  the card after comparing a ChatGPT pricing note and the rival BillCheck SA). Every bill's meter readings go on a
  usage chart per service. A jump against the person's own average raises an alert. "Every check" gets a fair-use
  cap, counted from the usage ledger. Once a month the app also gives a household report: what changed, what it
  cost, and what to watch. The tracking and alerts go in F171.2 (the readings and the average) and F171.5 (keeping
  them per account, the alert, the cap), about two steps. The report is about four to five steps, mostly in the app.
  F171.6 already holds ten steps, so the report becomes its own child, F171.8 The monthly report, after F171.6.
  Rivals seen the same day are in the research note `research/bill-checker/research-market.md`.

- **Two verdicts, and a re-read request for a jump** (Louis, 2026-11-21, his pick B). A bill can add up and still
  be high, from a leak or a catch-up reading after months of estimates. So the check gives one of two verdicts,
  decided in C#: *a line is wrong* (the sum doesn't match the tariff, and the dispute letter is offered), or *the
  sums are right, but use jumped* (no dispute; the agent gives the leak and catch-up checks to do). For a jump it
  also drafts a short request to the municipality to re-read or test the meter, a second letter type in F171.3
  (about one more step). F171.4's evals hold a correct-but-high bill to the second verdict, so a right bill is never
  called wrong.

- **The id is F171, and what a day changed** (Louis, 2026-11-22: "save merge this plan this feature is next", and
  of where it is built, "okay own repo it is"). The plan was written as F167 on 2026-11-21. That id went the same
  day to *Every project named for its namespace*, so the plan and its children are F171 here, and the names of the
  framework's projects and folders are spelled as that rename left them. F166 closed, so nothing waits on packages.
  Cape Town's rates exemption is corrected from the City's tariffs book, the price table is cited where it is now,
  and what left with Puls (the shop-prices pack, its deploy) is said so.

- **The look is the meter's wheels** (Louis's pick A on the card, 2026-11-22). The name is set in a water meter's
  number wheels, four white and three red, with ink, water blue and wheel red, Schibsted Grotesk and Martian Mono.
  Red is never an alarm: a line that doesn't match gets a red tint and "look here", and a jump with right sums is
  blue. The brand guide and its files are in the Claude project's files, under `marketing/bill-checker/brand/`.
  The name is still open (Klopdit is the working one), and the app's id and the product's root namespace wait on it.

## Open before the first child

A review on 2026-11-22 read this plan against `main` and the stores' rules. It is in the Claude project's files,
`plans/F167/F167-review-2026-11-22.md`, beside the proofreading and the gaps research of 2026-11-21 (the folder
keeps the plan's first id). None of what follows is picked yet, and each goes to Louis on its own card before the
child it belongs to starts.

Four children are over the ten-step limit once the changes above are counted: the bills pack, the account and
subscription, and the phone app. The subscriptions part and push are framework work and want to be children of
their own, and the bills pack wants splitting into reading and checking. The product's own names and layout would
follow Puls's example, its own root namespace in `src/` and `tests/`, and not the `LouisAgent.…` names and the
`packs/`, `identity/`, `hosts/` layout the namespace map shows. How the bill checker's own Azure is set up is open
too, since Puls's deploy left with Puls and the framework's sites stand on their own Bicep file (ADR 0131).

The review's nine new gaps, and the twelve of the day before, are in those files with three ways to close each.
The ones that change the scope come first: whether to measure how often bills are wrong on real bills before the
subscriptions child is built, and what "we will not pay anything" means beside the stores' own fees.

