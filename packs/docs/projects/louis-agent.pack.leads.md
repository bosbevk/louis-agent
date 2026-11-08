# louis-agent.pack.leads — design

> **Explanation.** How the guide tells the owner someone is interested (F112): one tool, a sender behind a seam, and
> what each setting does. **Elsewhere:** the guide that calls it in
> [louis-agent.identity.expert](../../../demos/louis-agent/docs/projects/louis-agent.identity.expert.md); how a pack is
> shaped in [Build a pack](../../../src/docs/guides/build-a-pack.md).

> **Kind:** Class library (`net10.0`) · **Path:** `packs/tools/louis-agent.pack.leads/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.coding-catalog`, for the guide

## Purpose

The guide on louis-agent.dev sells a hosted site with an agent and custom agents. When a visitor is interested, the way
to Louis was a mail link on the Contact page. A tool when it improves the prompt (ADR 0080): the guide gets `SendLead`,
so it asks the visitor for their name, a way to reach them and what they want, and sends Louis the lead itself, once,
after the visitor's yes (the rule is in the guide's voice).

## How it works

- `Tools/LeadTools.SendLead(name, contact, wants)`: refuses a lead with a part missing and says which, so the guide asks
  for it; sends a whole one with the clock's UTC time; a sender's failure comes back as text with its reason, never a
  throw into the loop (ADR 0005). Its texts are `Messages/LeadsToolMessage`.
- `Sending/ILeadSender` is the seam (Strategy with a Null Object): `LogLeadSender` writes the lead to the log, so the PC
  and the tests never send; `ResendLeadSender` posts a `ResendMessage` to Resend's API with the key as a bearer
  token. A sender says whether it `Mails`, and the tool tells the model when a lead only reached the log, so the
  guide never says Louis has it when he doesn't.
- `LeadsPack`: toolset `leads`, off without `LEADS_TO`; the sender picked by `RESEND_API_KEY`; the guide
  (`Prompts/toolsets/leads.md`) says to ask for the three things one at a time, repeat them back, and send on yes, once.
  The decision is [ADR 0101](../../../src/docs/adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md).

## Settings

| Setting | Means |
|---|---|
| `LEADS_TO` | Louis's address. Unset, the toolset is off and the guide has no tool. |
| `LEADS_FROM` | The verified sender, `guide@louis-agent.dev` by default. |
| `RESEND_API_KEY` | In the secrets. Unset, the log sender. |

## Tests

`packs/tests/louis-agent.pack.leads.tests`: the tool's refusals, its send and its truth about the log, the Resend
sender's request and its failure, the pack's settings and the sender the key picks.

---
[Docs index](../../../src/docs/README.md) · Related: [the docs pack](louis-agent.pack.docs.md), [the projects](../../../src/docs/projects/README.md)
