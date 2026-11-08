# ADR 0101: A lead is a tool, sent once on the visitor's yes

**Status:** Accepted (2026-11-08, F112)

## Context

The guide on louis-agent.dev sells a hosted site with an agent and custom agents (FX72). When a visitor was
interested, the only way to Louis was a mail link on the Contact page: the guide could point at it and nothing more.
Louis asked for the agent to "send me the lead" ("it is just another entry point and good selling point"). The
question was where the sending lives: as a rule in the prompt that tells the visitor what to do, or as a tool the
guide calls.

## Decision

A tool when it improves the prompt (ADR 0080): the guide gets one tool, `SendLead(name, contact, wants)`, in a pack
of its own (`louis-agent.pack.leads`), and the rule that governs it lives in the voice.

- **The tool checks what it is given.** It refuses a lead with a part missing and says which, so the guide asks for
  it instead of sending half a message. A failure of the mail service comes back as text with its reason, never a
  throw into the loop (ADR 0005).
- **Consent is in the prompt.** The voice says when it may be called: after the visitor gave their name, a way to
  reach them and what they want, after the guide repeated the three back and the visitor said yes, and once. Never
  what the visitor did not say, never a guessed contact, never twice.
- **The sender is a seam** (Strategy with a Null Object): Resend's HTTP API through our own `HttpClient` with a key,
  the log without one. No package sits between us and the wire; the PC and every test send nothing.
- **The tool tells the truth about the log.** A lead that only reached the log was not mailed to anyone, and the tool
  says so, so the guide never tells a visitor that Louis has it when he doesn't.
- **Settings:** `LEADS_TO` turns the toolset on, `LEADS_FROM` is the verified sender, `RESEND_API_KEY` sends for
  real. A pack names no identity (ADR 0027); the expert names the pack.

## Consequences

- A visitor can leave a lead in the conversation, and the guide can show, not tell, what an agent on a site does.
- One more setting on the deployed site, and one DNS record on louis-agent.dev for Resend to verify the sender.
- A lead lands in Louis's mailbox and nowhere else: there is no list of leads in the admin, and the chat keeps no
  copy beyond the session. A list is a later feature, if the leads warrant one.
- The same shape serves another site: a pack with one tool and one sender, the owner's address a setting.

## Links

- [ADR 0080: a tool when it improves the prompt](0080-a-tool-when-it-improves-the-prompt.md)
- [ADR 0005: guard the context](0005-guard-the-context.md)
- [ADR 0027: engine, identities and packs](0027-engine-identities-and-packs.md)
- The pack's design: [louis-agent.pack.leads](../../../packs/docs/projects/louis-agent.pack.leads.md)

---
[Docs index](../README.md) · [ADRs](README.md)
