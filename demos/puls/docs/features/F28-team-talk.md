# F28 · Team talk: Puls briefs its specialist, mood included, and the chat shows the exchange

> **Feature doc.** What F28 builds and why: Puls hands its specialist a note in a fixed shape that says how the user
> sounds, the user can switch that off, the chat shows the note and the answer as a talk between two people, and the
> checks score the note. **Status:** step 1 is built: the note's shape, the mood rule and the switch's marker are in
> Puls's prompts. The rest is the TODO's **F28 Team talk, the rest** under *Puls*: the switch in the API and the chat,
> the card as a thread, and the checks. **Depends on:** F5 (asking another agent), F21 (roles), F25 (the cook), F26
> (follow-ups), F27 (the checks the portal runs).
>
> **Plan:** [plans/F28](../plans/F28-team-talk.md). **In the TODO:** [F28](../../../../TODO.md).

![F28 team talk: the note Puls writes, the mood switch, the card as a thread, and the checks that score the note](../diagrams/F28-team-talk.svg)

Puls hands work to a specialist with `AskAgent(address, message)` and gets a report back. Before F28, the message was
whatever Puls wrote: sometimes the user's words, sometimes a line of its own, and nothing about the person asking.
Louis asked, on 2026-10-15, for two things. Puls should read from the way the person types how they're feeling, and
tell the specialist. And the chat should show that exchange, Puls telling its specialist what the person wants and the
specialist answering, the way you'd see a person with a team at work.

## User stories

**F28-S1 — Puls briefs the specialist like a colleague.** As a *user*, I want the specialist to know what I want, how
I sound and where I am, so that its answer fits me and not only my question.

*Given* I ask for a meal in a hurry, terse, with no greeting, *when* Puls hands it to the cook, *then* the message is
a note in a fixed shape: who it's for, what I want, how many it serves, the shop I'll buy at, how I sound, where I am,
and what I already said. "How I sound" says I'm rushed, so the cook picks a quick dish and keeps its answer short.
*Given* I write at length, relaxed, with time, *then* the note says so and the cook can suggest a Sunday dish. *Given*
my typing says nothing much, *then* the note says "neutral", never a guess dressed up as a fact. *Given* any mood,
*then* the report keeps its shape (F25): the mood changes the tone and the choices, not the sections. *Given* I switch
*Lees my stemming* off in the chat, at any point, *then* from my next message Puls doesn't read my mood and the note
has no "how I sound" line. Switched on again, it's back, in the same chat.

**F28-S2 — The chat shows the exchange.** As a *user*, I want to see Puls tell the specialist what I want and the
specialist answer, so that I can follow who did what, and correct Puls when it read me wrong.

*Given* a delegation, *when* its card is open, *then* it reads as a thread between two people: Puls's note as its
message to the specialist, with the mood line visible, then the specialist's work, then the specialist's reply as its
message, under the role's name. *Given* the Werk panel, *then* it shows the mood Puls read beside the task.

**F28-S3 — The checks measure it.** As *Louis*, I want the checks to say whether Puls briefed the specialist in the
shape, so that a prompt change that loses it is seen.

*Given* a run, *then* each conversation scores `briefed` (the note has its labels) and records the mood Puls read.

## Design

**A read of style, not of the person.** The mood is a guess from the typing: the length, a greeting or none, the
punctuation, the words' tone, a sharp reply after an answer that missed. Puls says it in the plain words a person uses
about a customer ("in a hurry", "has time, in good spirits", "frustrated after the last answer"), with what it read it
from, and its voice's word for *neutral* when the typing says nothing. It's never a diagnosis, it's never said back
to the user as a fact about them, and the specialist uses it for its tone and its choices only.

**A fixed shape, so three readers can use it.** The specialist reads the note, the card shows its lines and picks out
the mood, and the check scores the labels. The labels are in the task's language, so the note reads as a message
rather than a form. Each voice file shows them in an example note, and the routing in `voice.md` says what each line
holds.

| Line | Afrikaans | English | What it holds |
|---|---|---|---|
| For | `**Vir:**` | `**For:**` | The role, such as the cook |
| Wants | `**Wil hê:**` | `**Wants:**` | The request in one or two sentences, the dish in the user's own words |
| Serves | `**Vir hoeveel:**` | `**Serves:**` | How many people, as the user said it; left out for a recipe they gave no number for |
| Shop | `**Winkel:**` | `**Shop:**` | The one shop they'll buy at, or the voice's word for *any* |
| Sounds | `**Klink:**` | `**Sounds:**` | The mood, read from the typing |
| Where | `**Waar:**` | `**Where:**` | The town or suburb, or "not given" |
| Already said | `**Reeds gesê:**` | `**Already said:**` | What the user already told Puls that the specialist needs, or "nothing" |

A follow-up, `AskAgent` again to the same address, gets the same shape, shorter: what the user now asks, how they
sound now, and the people and the shop only when they change. Every specialist's prompt carries one line for its
side: match how the user sounds in your tone and your choices, and keep your report's shape.

**Nothing is simulated.** The note is the message Puls really sends, and the reply is the specialist's real answer.
The card draws what the chat's stream already carries. Before the call, Puls tells the user in one line who it's
handing the work to, and the specialist's report then reaches the user whole.

**The switch is the user's, per turn.** The system prompt can't change in the middle of a chat (FX4, history is
append-only), so the chat sends `read_mood` with each message. When it's off, the API puts a marker line,
`[stemming: af]`, before the user's text. The routing already knows the marker: a turn that starts with it gets a
note with no `Sounds` line, and Puls doesn't read or mention the user's mood anywhere in that turn. With no marker,
Puls reads the mood, so a host that sends nothing gets it. Nothing about the user is stored beyond that turn's
message.

**What it costs.** A note is a few dozen tokens longer than a one-line task, on the sub-session's first request only.
The system prompt doesn't change, so the cached prefix is untouched.

## Done when

F28-S1, the note: prompt tests on the routing, the voices and the roles (the shape, the mood rule, the guard rails),
and the golden request snapshot regenerated. This part is built, and `PulsLanguageFilesTests` holds it.

F28-S1, the switch: API tests on the prompt the engine gets with `read_mood` false and true, and the toggle checked by
hand mid-chat, with no 400.

F28-S2: the card and the Werk panel draw a note's lines and the mood, in the framework's shared `DelegationCard`, and a
free-text task still shows as before. Checked by hand in the chat.

F28-S3: the checks score `briefed` and record the mood, and the saved samples are re-reported (older runs score
false).

---
[Features](../../../../src/docs/features/README.md) · [Plan](../plans/F28-team-talk.md)
