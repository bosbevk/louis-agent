# 9 · Sessions and state: a chat that survives a restart

> **Skill:** #9 in the [goal](../GOAL.md)'s map · **Stage:** F43.1 Durable sessions (2026-11-25), the first child of
> F43 · **Next:** accounts and tenants, F43's later children; FX4, append-only history

Until this feature, a chat with louis-agent.dev lived in the API's memory. A restart, a release or the site falling
asleep ended every conversation, and the visitor's next message was told the chat was gone. In an outside comparison
with Microsoft Agent Framework, LangGraph and OpenAI's Agents SDK, that was the biggest gap: each of them keeps a
conversation somewhere a restart can't reach. This write-up says how we closed it, what it cost, and what it taught.

![Sessions and state: one seam with two stores, a turn appended whole, a chat resumed on its next message, kept 30 days idle, on MongoDB locally and DocumentDB in Azure](diagrams/09-sessions-and-state.svg)

## Before and after

Before, a session was an object in a dictionary, dropped after four hours idle or on any restart. After, the same
object is still what runs a turn, but how it opened and every finished turn are written to a store as they happen.
When a message comes for a chat the API doesn't hold, it reads the chat back, opens it again exactly as it first
opened, puts its turns back after the system prompt, and answers. On the local copy of the site we checked it by
hand: a visitor told the guide a name and a bakery, the container restarted, and the next answer was "You told me
your name is Pieter and you run a bakery in Paarl." The two model calls of that check cost $0.0036 together. Then
the same on louis-agent.dev itself, once the free cluster was deployed: a chat told the same name, the app's running
revision restarted, and the answer was "You're Pieter, and you run a bakery in Paarl.", for $0.0036 again.

The store keeps a chat for 30 days after its last message, then MongoDB deletes it by itself. Photos and PDFs are
never written down, only text. The privacy page and the trust page say so, and a test holds them to it.

## What changed

The heart of it is one seam, `ISessionStore`, with four calls: save how a chat opened, append a finished turn, load a
chat, delete one. Two stores sit behind it. The in-memory one is what tests and a developer's machine use. The MongoDB
one is what the site uses. A setting picks one when the host starts, so the session and its turn never know which.
Both pass the same contract, a set of checks written once and run against each store, so a third store later only
has to pass the same checks.

Saving follows the rule the history already had: it is append-only (ADR 0008). A chat is one document for how it
opened and one document per finished turn, written once and never rewritten. So no document grows with the chat, and
nothing is lost if a turn fails half way: the turn simply isn't there, and the chat goes on from the one before.

Resuming reuses the code that opens a new chat, given the old id. That matters more than it looks. A chat opened with
a coding agent's toolsets must come back with exactly those, and the safest way to make sure is to go through the
same checks it passed the first time, not a second path that could drift from the first.

## What it cost to get right

The first draft of the resume would have opened a hole. The API's open door lets a visitor talk to the guide without
a key, and after a restart it let any unknown chat id through, so the handler could say "gone". With resume, "gone"
became "here it is", for any id, a keyed coding chat included. So the store now also keeps whether a chat came in
through the open door and as whom. A visitor's chat is let in again; a chat opened with the key still needs the key.
A test holds the keyed case shut.

The money side was the other surprise. Azure's free DocumentDB tier isn't listed for South Africa North in
Microsoft's docs, though the region's price list has free meters for it. We put the free tier in the Bicep file
anyway, with North Europe as the fallback, since a refused deploy creates nothing and costs nothing. The smallest paid
cluster would have been about R430 a month, a fifth of what the cost fixes just before this had saved.

## What didn't work

A setting that can't be read used to fall back quietly, and for the open door that is right: a door that can't read
its cap stays shut. For the store it would be wrong. A typo in `SESSION_STORE` falling back to memory would lose every
chat at the next restart without a word, so a store setting that can't be read stops the host as it starts.

Our first plan named only the privacy page as the place that talks about what is kept. The trust page said it twice
more, in a fact and in its backups line, and its table of who handles data said Azure saw no words. All three would
have gone on saying the old thing.

## What it taught, in one line each

A session is state, and state outlives the process that holds it. Resume through the same door a session first came
in by, so it can't come back as something it wasn't allowed to be. Append, never rewrite: a document per turn keeps
every write small and every failure harmless. A setting that keeps data fails loud. When what is stored changes,
search the whole site for every sentence about storing.

## What's left

A sub-session whose parent was dropped is still looked up in memory only. The plan gave only louis-agent.dev the
store; FX250 gave the framework's site the same database the same day, since a session's id is a GUID neither site can
repeat. The free cluster has no backup, and pauses after 60 days with no use. Accounts and tenants come next in F43,
on a relational database of their own.

---
[Docs index](../README.md) · Related: [Learning write-ups](README.md) ·
[LouisAgent.Api](../projects/LouisAgent.Api.md#8-sessions) · [Session architecture](../specs/SESSION_ARCHITECTURE.md)
