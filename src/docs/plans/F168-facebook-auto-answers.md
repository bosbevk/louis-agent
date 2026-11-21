# F168 Facebook auto-answers

A plan, for Louis's approval: how the guide, Orbi, answers the people who message the louis-agent.dev Facebook Page
on Messenger, by itself, in Louis's voice, and steps back the moment Louis answers himself. Eleven steps, one
commit each, on `feature/F168-facebook-auto-answers`, starting after F126 (the redesigned evals), since its
measurement is an eval in F126's format. Louis made eight picks on 2026-11-21, three on the design and five on the
gaps a review found (*Louis's picks*), and the plan is drawn on them.

![The overview: a door Meta calls, a conversation per person, the guide with a Messenger focus, Louis able to take over, then measured and shipped](diagrams/F168-facebook-auto-answers-overview.svg)

## Context

Louis, 2026-11-21, after the first real question reached the Page on Messenger ("Can I customize my website's
design?") and the `answer-a-message` skill was written to draft his replies: "then we should plan integration into
facebook to auto answer questions".

So the job is to take what `answer-a-message` does by hand (read the question, answer it first, say an honest no
with its reason, end with one question back) and let the guide do it on Messenger, day and night, inside the rules
Meta sets for Pages that answer with a program. The guide already knows what we sell, how Louis speaks and when to
pass a lead on. What it lacks is a way in from Facebook and a way back out.

**What was read in the repo.** The guide's identity (`GuideIdentity`: the docs, leads and site packs, every answer
ending with `Offer`), its voice and its focus skills (`focus-selling.md`, `focus-demos.md`), how a focus reaches the
prompt (`PageRequest.Focus`, read by `PageBlock.FocusOf`), the sessions API (`AgentSessions`, in memory, a session
idle for four hours dropped), the leads pack's `SendLead`, the site pack's `Offer` (two to four choices, labels up
to 40 characters), `SessionStreamReader` in `LouisAgent.SessionStream`, `PeerSession` (the engine asking another
agent's sessions over HTTP), the API's test host (`ApiTestHost` and `TestAgent` in `src/tests/hosts/LouisAgent.Api.Testing`), the open door (`OpenDoorOptions`), the site's host
(`LouisAgent.SiteApi/Program.cs`) and its deploy files (`src/site/deploy/azure`).

**What was read at Meta**, all on developers.facebook.com, on 2026-11-21:

- *Webhooks for Messenger* ([webhooks](https://developers.facebook.com/documentation/business-messaging/messenger-platform/webhooks)):
  the handshake (`hub.mode`, `hub.verify_token`, `hub.challenge` echoed back), every POST signed in
  `X-Hub-Signature-256` with HMAC-SHA256 under the app secret, a `200` expected within five seconds, a few quick
  retries and then, after an hour of failures, the app unsubscribed from the Page. The page asks us to drop
  duplicates ourselves and says events can arrive out of order. `entry[].messaging[]` is an array, so a POST may
  carry more than one message.
- *Send API* ([send messages](https://developers.facebook.com/documentation/business-messaging/messenger-platform/send-messages),
  [reference](https://developers.facebook.com/docs/messenger-platform/reference/send-api)): `POST /{page-id}/messages`
  with a Page access token, `messaging_type` `RESPONSE` for an answer, text under 2,000 characters,
  `sender_action` `typing_on` as its own call, error `1545041` when the window has closed, `613` for the rate limit.
  The pages show three different Graph versions (v21, v25, v26), so the version is pinned in one constant.
- *Quick replies* ([quick replies](https://developers.facebook.com/docs/messenger-platform/send-messages/quick-replies)):
  up to 13 buttons, which vanish after the next message; long titles are cut, at a length the page doesn't state.
- *The policy* ([policy](https://developers.facebook.com/documentation/business-messaging/messenger-platform/policy)):
  a Page may answer freely for 24 hours after a person's message; after that only with a tag, and the
  `HUMAN_AGENT` tag (seven days, a person's own reply) needs App Review and Business Verification. A bot typed
  *automated* must answer every input within 30 seconds. Where a law requires it, an automated chat says it is
  automated at the start of a thread, and Meta recommends saying so everywhere. No page sets a rule about AI or
  language models as such.
- *Conversation routing* ([routing](https://developers.facebook.com/documentation/business-messaging/messenger-platform/conversation-routing)),
  which has replaced the handover protocol: our app can be the Page's default app, every connected app still gets
  the webhooks, and when Louis answers from the Page's inbox, the inbox takes control of that conversation.
- *Access levels and permissions* ([access levels](https://developers.facebook.com/docs/graph-api/overview/access-levels),
  [permissions](https://developers.facebook.com/docs/permissions)): `pages_messaging`, which needs
  `pages_manage_metadata` and `pages_show_list`. With Standard Access the app only talks to people who have a role on
  it (Louis, and testers he adds). To answer anyone, the app needs Advanced Access, which needs App Review and
  **Business Verification**.

**What could not be confirmed**, and the step that confirms it:

- Whether Louis can pass Business Verification without a registered company (Meta asks for business documents).
  Everything up to step 9 works with Standard Access, talking to Louis and his testers, so the plan doesn't wait on
  it; Louis starts the verification in parallel (*What Louis does at Meta*). If it fails, the plan stops after step 10
  and the guide answers only testers.
- Whether the signature is over the raw bytes exactly as they arrive. The webhooks page warns that Meta signs the
  escaped-unicode form of the payload. We hash the raw body we receive, which is that form, and step 2's test holds
  a payload with `ä` in it; the first live message in step 1's check proves it.
- Whether our app still gets the person's messages while the inbox holds the conversation (the routing page says so
  for Meta's own Business AI, on the `standby` field, and not for the inbox). Step 8 subscribes to `standby` and
  `message_echoes` and tests on Louis's own account before it relies on either.
- How long a guide turn takes on Messenger, against the 30-second rule. Nothing measures it today (the stream
  probe, F159, is still a TODO row), so step 6's live check times it by hand, and step 6 sends a short holding line
  if the answer isn't ready in 25 seconds, so the rule holds either way.
- What `app_id` an echo carries when Louis answers from the Facebook or Messenger app on his phone rather than the
  Business Suite inbox. Step 8 opens with a live check from all three.
- Whether Meta's data-deletion callback can be traced to a Messenger conversation. The callback carries an
  *app-scoped* user id ([data deletion callback](https://developers.facebook.com/docs/development/create-an-app/app-dashboard/data-deletion-callback)),
  and a conversation is keyed by the *page-scoped* id; the page doesn't say how the two map. Step 9 opens with a
  live check, and falls back to a deletion-instructions page, which the same page's FAQ accepts.
- Whether a Page token from the dashboard's *Token Generation* ever expires. The quick start says each new one
  keeps the old ones working; Louis checks his in Meta's Access Token Debugger before it goes in the Key Vault.

## Louis's picks

Each was put to Louis on its own, with three options, on 2026-11-21.

**1. How much the guide does by itself.** Louis picked A on 2026-11-21. (A, recommended) It answers every message straight away, says once that
it's Louis's AI assistant, and steps back for that person the moment Louis answers himself; a price, a booking or
"can I talk to Louis" becomes a lead to Louis, as on the site. (B) It drafts each answer into the admin, and Louis
presses send: safest, but an answer has to go within 24 hours and Louis becomes the bottleneck the work was meant to
remove. (C) It answers only the questions in `answer-a-message`'s table and leaves the rest for Louis: predictable,
but most real questions aren't in the table yet. A is what Louis asked for, and the guide already holds the rules
that make it safe (never a price, never a person, never a made-up fact).

**2. Where the code lives.** Louis picked A on 2026-11-21. (A, recommended) A new framework project, `louis-agent.api.messenger`, that any site's
host maps with one line, `app.MapLouisAgentMessenger(options)`, the way a host opts into the eval studio
(`LouisAgent.Api.Evals`). It asks the agent through the same `/v1/sessions` API the widget uses, over HTTP. This is
an Adapter (Refactoring.Guru: two interfaces that don't fit, joined by a class that translates), and the queue in front
is Microsoft's Queue-Based Load Leveling (Azure Architecture Center): Meta gets its `200` at once, the slow work
happens behind it. (B) Inside `LouisAgent.SiteApi` only: less to wire today, but Puls, the showroom's businesses and
every client site would copy it, and the rule says a part any site could use goes to the framework. (C) A small host
of its own, a separate container: cleanest split, but a second app to deploy, pay for and keep alive for one
endpoint. It is not a pack: a pack is tools the model calls, and Messenger is a way in, not a tool.

**3. How the guide speaks on Messenger.** Louis picked A on 2026-11-21. (A, recommended) The same guide, with a `messenger` focus: one more skill,
`focus-messenger.md`, like `focus-selling.md`, reached through `PageRequest.Focus`, which the engine already reads.
Its `Offer` choices become quick replies; a card or a page link becomes a plain link in the text. (B) A second voice
file in the guide's identity, written for Messenger: free to differ, but two voices to keep in step on every rule.
(C) A new identity for the Page: separate packs and tests, and all the guide's knowledge copied. A keeps one Orbi,
whose rules already match `answer-a-message`.

**4. The open door's cap.** The guide's open door caps the sessions one *address* may open a day, and every
Messenger session comes from the host itself, so the door would shut after a few people. Louis picked A: the
library calls the sessions API with its own key, behind the key gate, and keeps its own daily cap on turns per
person, which also caps what one person (or a script) can spend. The other ways were exempting the host's own
address from the cap, or raising it.

**5. Deleting a person's data.** App Review asks how a person's data is deleted. Louis picked a deletion callback,
and, after it turned out the callback sends a different id, kept it with a live check first and an instructions
page to fall back on (step 9). The other ways were an instructions page with deletion by hand, or keeping no
Messenger turn log.

**6. Where the measurement lives.** The guide has no eval today, and F126's format isn't built. Louis picked
building F126 first, so step 10 is a judged eval in F126's format. The other ways were a free scripted check now
with the judged eval later, or an eval in today's format that F126 would delete.

**7. When answers stop going out.** Louis picked A: every failed send is logged with Meta's reason, and the admin
overview shows the channel's last good send. The other ways were a mail on the first failure of a kind, or an
Azure alert.

**8. Louis answers from his phone.** Louis picked A: any echo that didn't come from our app quiets the guide for
that person, checked live from the inbox, the Facebook app and the Messenger app before step 8 relies on it. The
other ways were answering only from the inbox, or a take-over keyword.

## Design

### How a message travels

![The architecture: Meta calls one new door on louis-agent.dev's host; the Messenger library asks the guide through the sessions API and sends the answer back through Meta's Send API](diagrams/F168-facebook-auto-answers-architecture.svg)

Someone writes to the Page. Meta posts the message to `/messenger/webhook` on louis-agent.dev. The endpoint checks
Meta's signature over the raw body, reads the JSON into records, drops a message id it has seen before, puts each
new message on a queue and answers `200` in milliseconds. A background worker takes the message, shows "typing"
in Messenger, and finds that person's conversation: their page-scoped id (the PSID, the id Meta gives a person for
this Page only) mapped to a guide session. The first message opens the session; each message after it is the next
turn. The worker reads the turn's stream to its end, takes the text and the `Offer`'s labels, cuts the text under
2,000 characters at a sentence, and sends it back with the labels as quick replies.

![A question on Messenger, answered: Meta gets its 200 straight away, the answer goes back on its own call a few seconds later](diagrams/F168-facebook-auto-answers-communication-a-question-on-messenger.svg)

**Why answer first and work after.** Meta wants its `200` within five seconds and the guide can take longer.
Answering late makes Meta retry, the retry makes a second answer, and an hour of failures unsubscribes the Page.
So the endpoint only checks, records and queues, and the worker does the rest.

**Why over HTTP and not in the same process.** The sessions are `internal` to `LouisAgent.Api`, and the design rules
say a whole task goes through the API's sessions. Over HTTP, the Messenger library uses the same sessions and the
same usage ledger as the widget, and it can sit in front of any host. The cost is a local call on the same
container, a few milliseconds.

**Its own key and its own cap.** The widget comes in through the open door, which counts sessions per visitor's
address. Messenger has no visitor address: every call comes from the host. So the library calls with its own API
key (`MESSENGER_AGENT_KEY`), like any other caller behind the key gate, and keeps its own cap,
`MESSENGER_TURNS_PER_DAY` per person, counted in `PageConversations`. A person over the cap gets one polite line
saying Louis will pick it up, and no more paid turns that day.

**One turn at a time per person.** People send three short messages in a row. A session runs one turn at a time
(`TryStartTurn` refuses a second), so each conversation holds a small lock, and the next message waits for the
answer before it. Different people run side by side.

**Louis steps in.** When Louis answers from the Page's inbox or his phone, Meta sends an echo of his message, from an app that
isn't ours. From then on the worker stays quiet for that person for 24 hours, and the inbox holds the conversation
(*conversation routing*). When the person asks for Louis, a price or a booking, the guide does what it does on the
site: asks for consent, sends the lead with `SendLead`, and says Louis will answer here.

**What it remembers.** Only what the sessions already hold, in memory: a session idle for four hours is dropped,
and a restart starts every conversation fresh. Durable sessions are a known gap (the TODO's known limitations), and
a fresh start costs one "Hi" at most. Nothing from Messenger is stored apart from the usage ledger's rows and the
turn log the sessions already write.

**When a send fails.** Meta can refuse a send (the window closed, the rate limit, a dead token), and after an hour
of failed webhooks it unsubscribes the Page. Every failed send is logged with Meta's reason, never thrown into the
worker, and the admin overview shows the channel's last good send, so a dead token shows up the next time Louis
looks.

**Deleting a person's data.** Meta's data-deletion callback POSTs a `signed_request`: a base64url payload signed with
HMAC-SHA256 under the app secret, holding the person's app-scoped id. We answer with `{ url, confirmation_code }`,
the url a page where the person reads the status of their request. What we delete is that person's Messenger turn
logs. Since a conversation is keyed by the page-scoped id, step 9 first proves the two can be matched; if they
can't, the callback's place is taken by an instructions page (message the Page "delete my data"), which Meta's FAQ
accepts.

**Not in this plan.** Comments on the Page's posts (Meta's private replies, which need `pages_manage_engagement`
and allow one message per comment), Instagram and WhatsApp. Each is another way in for the same library, and
Messenger comes first because that is where people are asking.

### What it shows the person

The first answer in a conversation opens with one line saying it's Orbi, Louis's AI assistant, and that Louis reads
these too (Meta recommends it, and the guide's first rule already says it never passes as a person). The rest is
`answer-a-message`'s shape: their name and the answer first, the why in a sentence or two, what we do instead when
it's a no, and one question back. No price, no emojis, British English. In Page settings Louis types the bot as
*automated*, which brings the 30-second rule with it, so step 6 holds that rule in code.

### The four pictures

The interface design: Meta's JSON in and out as records, and two seams, one to the agent and one to Meta, so every
test runs without the network.

![The interface design: the endpoints, the records, the worker, and the two seams IAgentTurns and IMessengerSender with their implementations](diagrams/F168-facebook-auto-answers-interface-design.svg)

The objects one person's message touches in memory:

![Objects in memory: one worker reads one queue, and each person has one conversation holding the guide's session id](diagrams/F168-facebook-auto-answers-objects-in-memory.svg)

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `MessengerOptions` | `src/hosts/louis-agent.api.messenger/` | `LouisAgent.Api.Messenger` | 1 |
| `MessengerEndpoints` | `…/Webhook/` | `LouisAgent.Api.Messenger.Webhook` | 1, 2, 4 |
| `WebhookSignature` | `…/Webhook/` | `LouisAgent.Api.Messenger.Webhook` | 2 |
| `WebhookPayload`, `WebhookEntry`, `MessagingEvent`, `MessengerUser`, `IncomingMessage`, `Postback` | `…/Wire/In/` | `LouisAgent.Api.Messenger.Wire.In` | 3 |
| `IncomingTurn`, `TurnKind`, `SeenMessages` | `…/Inbox/` | `LouisAgent.Api.Messenger.Inbox` | 3 |
| `MessengerWorker` | `…/Inbox/` | `LouisAgent.Api.Messenger.Inbox` | 4 |
| `IAgentTurns`, `AgentTurnClient`, `AgentAnswer` | `…/Turns/` | `LouisAgent.Api.Messenger.Turns` | 5 |
| `PageConversations`, `PageConversation` | `…/Turns/` | `LouisAgent.Api.Messenger.Turns` | 5, 8 |
| `IMessengerSender`, `GraphMessengerSender`, `SendOutcome` | `…/Sending/` | `LouisAgent.Api.Messenger.Sending` | 6 |
| `SendRequest`, `OutgoingMessage`, `QuickReply` | `…/Wire/Out/` | `LouisAgent.Api.Messenger.Wire.Out` | 6 |
| `MessengerLogMessage`, `MessengerText` (ADR 0009) | `…/Messages/` | `LouisAgent.Api.Messenger.Messages` | 1 onward |
| `GuideFocus.Messenger`, `focus-messenger.md` | `src/site/LouisAgent.Identities.Guide/` | `LouisAgent.Identities.Guide` | 7 |
| `DeletionEndpoints`, `SignedRequest`, `DeletionReply` | `…/Deletion/` | `LouisAgent.Api.Messenger.Deletion` | 9 |
| The Messenger eval (a `.eval.json`, files only) | where F126 puts the guide's evals | n/a | 10 |
| The tests | `src/tests/hosts/louis-agent.api.messenger.tests/`, mirroring the folders | `LouisAgent.Api.Messenger.Tests.*` | each step |

### Folder trees

Today, `src/hosts/` holds `LouisAgent.Api`, `.api.contracts`, `.api.evals` and the others. After:

```
src/hosts/louis-agent.api.messenger/     # new (step 1)
  MessengerOptions.cs
  Webhook/   Wire/In/   Wire/Out/   Inbox/   Turns/   Sending/   Deletion/   Messages/
src/tests/hosts/louis-agent.api.messenger.tests/   # new (step 1), the same folders
```

### What is reused

`LouisAgent.SessionStream` (`CreateSessionRequest`, `SendMessageRequest`, `PageRequest.Focus`,
`SessionStreamReader`) for talking to the guide; `LouisAgent.Choices` for `TurnKind` and the guide's new focus;
ASP.NET Core's `BackgroundService` and `System.Threading.Channels` for the queue; `HMACSHA256` and
`CryptographicOperations.FixedTimeEquals` from .NET for the signature; `IHttpClientFactory` for both HTTP clients;
`LouisAgent.Api.Testing`'s `ApiTestHost` and `TestAgent` for step 5's tests. **No new package.** Meta's Graph API is a few JSON
calls, so there's no SDK to adopt (Meta publishes none for .NET, and the community ones are unmaintained).

## What Louis does at Meta

Free, by hand, before step 1's live check. These go in the project's design doc as a guide when the plan closes.

1. At developers.facebook.com, create an app (type Business), add the *Messenger* use case, and connect the
   louis-agent.dev Page.
2. Generate the Page access token under *Token Generation*, and check it in the Access Token Debugger.
3. Note the app secret (*App settings > Basic*) and choose a verify token (any long random string).
4. Start Business Verification in Meta Business Suite (it can take days), and add testers under *App roles*.
5. After step 1 is deployed: set the callback URL `https://louis-agent.dev/messenger/webhook` with the verify token,
   and subscribe the Page to `messages`, `messaging_postbacks`, `message_echoes` and `standby`.
6. After step 9: set the Data Deletion Request URL in the app's Basic settings.
7. After step 10: App Review for `pages_messaging` (with `pages_manage_metadata` and `pages_show_list`), with the
   screen recording Meta asks for. Then in Page settings, set our app as the default app and the bot type to
   *automated*.

## Steps

One commit each, its test written first and seen failing; a review stop after each.

1. **The project and the handshake.** `louis-agent.api.messenger` with `MessengerOptions.FromEnvironment()`
   (`MESSENGER_VERIFY_TOKEN`, `MESSENGER_APP_SECRET`, `MESSENGER_PAGE_TOKEN`, `MESSENGER_PAGE_ID`,
   `MESSENGER_AGENT_URL`, `MESSENGER_AGENT_KEY`, `MESSENGER_TURNS_PER_DAY`; none set means the door isn't mapped),
   `MapLouisAgentMessenger`, and the `GET` that echoes the challenge for the right token and answers `403`
   otherwise. `LouisAgent.SiteApi` maps it. The project's design doc starts here, and grows with each step. Test:
   the right token gets the challenge back, a wrong one `403`, no settings no route.
2. **Meta's signature.** `WebhookSignature.IsValid` over the raw body; a `POST` without a valid signature gets
   `401` and nothing is read. Test: a body signed with a known secret passes; one changed byte, a missing header, a
   `sha1=` header and a body with `ä` in it each behave as Meta's page says.
3. **The message as records.** Meta's JSON into `WebhookPayload` and its records (ADR 0041), and `TurnsOf(pageId)`
   gives one `IncomingTurn` per message: `Ask` for a person's text or a quick reply's payload, `PersonReplied` for
   an echo that isn't ours, `NotText` for a photo or a sticker. `SeenMessages` lets a message id in once. Test:
   Meta's own sample payloads, a batch of two, an echo, a repeat.
4. **Answer at once, work after.** A bounded `Channel<IncomingTurn>`, and `MessengerWorker` as a `BackgroundService`
   reading it. Test: the `POST` returns `200` while the worker is held, and the turn arrives once it's released.
5. **One conversation per person.** `IAgentTurns` and `AgentTurnClient`: opens a guide session with the library's
   key (identity `louis-agent-guide`, focus `messenger`), sends the text, reads the stream to its end into an
   `AgentAnswer` (the text, and the labels of an `Offer`). `PageConversations` keeps PSID to session, one turn at a
   time, the daily cap per person, and a new session when the old one was dropped. Test: on `ApiTestHost` with its
   `TestAgent`, two people at once, three messages in a row from one, a dropped session, and the turn over the cap.
6. **Send the answer.** `IMessengerSender` and `GraphMessengerSender`: `typing_on`, then the text cut under 2,000
   characters at a sentence, the labels as quick replies, a holding line if no answer is ready in 25 seconds, and an
   error read into `SendOutcome` with Meta's reason (window closed, rate limit, token expired), logged, never
   thrown into the worker; the admin overview shows the channel's last good send. A photo or sticker gets a short
   line that Orbi reads text. Test: a fake HTTP handler sees the right calls in order; each error code lands in its
   outcome and its log line; the overview's reader finds the last good send.
7. **The guide on Messenger.** `GuideFocus.Messenger` and `focus-messenger.md`, written from `answer-a-message`: the
   opening line that it's Louis's AI assistant, the answer in the first line, an honest no with its reason, one
   question back, at most six short sentences, no cards, plain links, the lead when they want Louis. The guide's
   doc says it. Test: `GuideFocusesTests`, `GuideSkillsTests` and `SiteNamesItsFocusesTests` in
   `src/tests/site/LouisAgent.Site.Tests/Guide/`, each with the new focus.
8. **Louis takes over.** First a live check: Louis answers a tester from the inbox, the Facebook app and the
   Messenger app, and the echoes are read. Then any echo not from our app makes `PageConversation` quiet for 24
   hours, and `standby` messages are read but never answered. Test: after an echo from another app, the next
   message gets no answer; after 24 hours (a fake `TimeProvider`) it does.
9. **Deleting a person's data.** First a live check: whether a tester's app-scoped id can be matched to their
   page-scoped id. If it can, `DeletionEndpoints` verifies the `signed_request`, deletes that person's Messenger turn
   logs, and answers `{ url, confirmation_code }`, with a status page at the url. If it can't, a deletion-
   instructions page instead, and the guide passes a "delete my data" on to Louis. Test: a signed request made with
   a known secret is verified and refused when changed; the turn logs of that person, and only that person, are
   gone; the reply's shape.
10. **Measured.** An eval of Messenger conversations in F126's format: the six questions in `answer-a-message`'s
    table, a no ("can I edit it like Wix"), a price, "are you a bot" and "can I talk to Louis", each judged on the
    four parts of an answer. The run is paid, so it waits for Louis's go. Test: the eval loads, and its replay
    passes with no model.
11. **Shipped and written up.** The settings in the site's deploy (`src/site/deploy/azure`: the app secret, the
    Page token and the agent key in Key Vault, the plain values in the app's YAML), AGENT_COMMUNICATION.md, the
    plan folded and deleted, and the write-up (`/finish-a-feature`). Test: the deploy files' tests and the docs'
    tests.

## Verification

- **The tests** of each step, as above, and the full suite before the pull request (after merging `main`).
- **Free checks by hand.** After step 1 is live: Meta's *Verify and save* turns green. After step 6: Louis messages
  the Page from his own account and gets an answer within 30 seconds, with quick replies. After step 8: Louis
  answers from the inbox, writes again from a tester's account, and the guide stays quiet. After step 9: a tester
  removes the app in their Facebook settings and the callback, or the instructions page, does what it says.
- **The paid run** (step 10): one pass of the Messenger eval on the guide's model, on Louis's go, its cost reported
  after.
- **Done when:** a tester's question on Messenger gets an answer in the guide's Messenger voice inside 30 seconds;
  Louis answering from the inbox silences the guide for that person; the eval passes; and once Meta approves
  `pages_messaging`, the same holds for anyone who writes to the Page.

## Concepts

- **Webhooks and signatures** (steps 1, 2): how a platform calls you, and how you prove it was the platform. GOAL.md
  skill 11, *Protocols*, and 15, *Safety and security*.
- **Idempotency and at-least-once delivery** (steps 3, 4): a message can arrive twice, so the second does nothing;
  answering fast and working after. Skill 14, *Reliability*.
- **The agent behind a channel** (steps 5, 6): one agent, many doors; a channel adapts the agent's answer to what the
  channel can show (Offer to quick replies). Skill 10, *Multi-agent orchestration*, and 11, *Protocols*.
- **Prompt design per channel** (step 7): one voice, a focus per place it speaks. Skill 3, *Prompt design*.
- **Rate limiting and observability** (steps 5, 6): a cap per person, and failures you can see. Skills 13 and 17.
- **Human in the loop** (step 8): the person always wins, and the agent knows when to stop. Skill 16.
- **Privacy by design** (step 9): a signed request, deleting exactly one person's data, and saying what we keep.
  Skill 15, *Safety and security*.
- **Evaluation** (step 10): measuring the answers against the rules they must keep. Skill 12.

## Approved during the build

Nothing yet.

## The records

In the commit that saves this plan: its row in the plans' index (`README.md` here), its row in `TODO.md` under
*Next*, after F126, and its row in the work log's open table, with the count above that table.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [The guide's answer-a-message skill](../../../.claude/skills/answer-a-message/SKILL.md)
