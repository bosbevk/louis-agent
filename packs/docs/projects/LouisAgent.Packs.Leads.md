# LouisAgent.Packs.Leads — design

> **Explanation.** How the guide tells the owner someone is interested (F112): one tool, a sender behind a seam, and
> what each setting does. **Elsewhere:** the guide that calls it in
> [LouisAgent.Identities.Guide](../../../src/docs/projects/LouisAgent.Identities.Guide.md); how a pack is
> shaped in [Build a pack](../../../src/docs/guides/build-a-pack.md).

> **Kind:** library (`net10.0`) · **Path:** `packs/tools/LouisAgent.Packs.Leads/` · **References:**
> `LouisAgent.Composition` only · **Used by:** `LouisAgent.Coding.Catalog`, for the guide

![The leads pack: SendLead, a sender behind a seam, and failures as text](diagrams/LouisAgent.Packs.Leads.svg)

## Purpose

The guide on louis-agent.dev sells a hosted site with an agent and custom agents. When a visitor is interested, the way
to Louis was a mail link on the Contact page. A tool when it improves the prompt (ADR 0080): the guide gets `SendLead`,
so it asks the visitor for their name, a way to reach them and what they want, and sends Louis the lead itself, once,
after the visitor's yes (the rule is in the guide's voice).

## How it works

### 1. Sending a lead

`Tools/LeadTools.SendLead(name, contact, wants)` is the one tool. A lead with a part missing is refused, and the
answer says which part, so the guide asks for it. A whole lead is sent with the clock's UTC time. If the sender fails,
the failure comes back as text with its reason, never as an exception thrown into the loop. The tool's texts are in
`Messages/LeadsToolMessage`.

*Decided in:* ADR 0005.

### 2. The sender behind a seam

`Sending/ILeadSender` is the seam, a Strategy with a Null Object. `LogLeadSender` writes the lead to the log, so the
PC and the tests never send anything. `ResendLeadSender` posts a `ResendMessage` to Resend's API, with the key as a
bearer token. Each sender says whether it `Mails`, and the tool tells the model when a lead only reached the log, so
the guide never says Louis has it when he doesn't.

### 3. The pack and its guide

`LeadsPack` holds the toolset `leads`, which is off without `LEADS_TO`. `RESEND_API_KEY` picks the sender. The guide,
`skills/leads/SKILL.md`, says to ask for the three things one at a time, repeat them back, and send on the
visitor's yes, once.

*Decided in:* [ADR 0101](../../../src/docs/adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md).

### 4. A demonstration's lead

The turn carries who it runs as, `TurnIdentity`, an ambient value from `LouisAgent.Composition`. When that identity
is a demonstration, one of the showroom's businesses, the lead names it, the mail's subject is
`[demo] {Identity}: {Name}` (`LeadsMailText`), and the reply tells the model it was a demonstration (`SentDemo`,
`LoggedDemo`), so the model tells the visitor. It's still one pack with one `SendLead`.

*Decided in:* F127.2,
[ADR 0116](../../../src/docs/adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md).

![How it works: a lead is sent once, on the yes, and the key picks Resend or the log](diagrams/LouisAgent.Packs.Leads-flow.svg)

![One lead sent: a missing part is asked for, a whole one is mailed, and a failure comes back as text](diagrams/LouisAgent.Packs.Leads-sequence.svg)

![The types: one tool, a lead record and the sender seam with its two senders](diagrams/LouisAgent.Packs.Leads-interface.svg)

## Settings

| Setting | Means |
|---|---|
| `LEADS_TO` | Louis's address. Unset, the toolset is off and the guide has no tool. |
| `LEADS_FROM` | The verified sender, `guide@louis-agent.dev` by default. |
| `RESEND_API_KEY` | In the secrets. Unset, the log sender. |

## Tests

`packs/tests/LouisAgent.Packs.Leads.Tests`: the tool's refusals, its send and its truth about the log, the Resend
sender's request and its failure, the pack's settings and the sender the key picks, and a demonstration's lead: its
`[demo]` subject and body, and the reply that says it was a demonstration.

---
[Docs index](../../../src/docs/README.md) · Related: [the docs pack](LouisAgent.Packs.Docs.md), [the projects](../../../src/docs/projects/README.md)
