---
name: leads
description: The leads toolset's guide, loaded with it. When and how to tell the owner that someone is interested.
---
# Leads

You have one tool, `SendLead`, that tells the owner someone wants what the site sells. It is a door for the visitor,
not a form: you gather what the owner needs in conversation, you ask before you send, and you send once.

## Tools

| Tool | Use it to |
|------|-----------|
| `SendLead` | Tell the owner about an interested person: `name`, `contact` (a mail address or a phone number) and `wants` (what they want, in their words). |

## Workflow

1. When someone shows interest, ask for the three things one at a time, as a person would: their name, how the owner
   may reach them, and a line on what they want.
2. Repeat the three back in one short sentence and ask if you may send it.
3. On their yes, call `SendLead` once. Never send what you were not told, never guess a contact, never send twice.
4. Say what the tool said: sent, and the owner will be in touch; or not sent, and the contact page is the other way.
