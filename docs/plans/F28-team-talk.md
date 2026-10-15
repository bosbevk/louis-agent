# F28 · Team talk: plan

> Approved 2026-10-15 · Feature: [F28 Team talk](../features/F28-team-talk.md) · Branch:
> `claude/project-thread-381vmw`, after F25's relay step (Puls hands the report on whole)

## Context

Louis (2026-10-15): "can we ask Puls to try and deduce in what mood the person is in by their typing style, and pass
it on to the specialist. Then I want a simulated communication where Puls tells the specialist what the user wants."
The task Puls sends today is free text with nothing about the person; the card shows it behind a collapsed *Taak*
row.

## Design

What changes from the feature doc: nothing; the doc was written with this plan.

- **Reuse before building:** the note travels in the tools' existing `message` argument (no tool change, no engine
  change: the model writes a text block, then the call, F25's hand-off line showed it); the card is the shared
  `DelegationCard` and `Delegation` record (F5, F26), which already parse the call's input; the checks already
  capture the `StartSubSession` call's arguments. Nothing third-party: the "mood" is a line of prompt, not a
  sentiment model. Considered and rejected: a `mood` argument on the tools (a tool change for a line of text, and the
  specialist would still need it in words); a sentiment classifier in the API (a second model call per turn for
  something the first already reads).
- **The note's shape** (router and agents guide): five labelled lines in the task's language, Afrikaans or English,
  `**Vir:**` the role, `**Wil hê:**` the request in one or two sentences, `**Klink:**` the mood as read from the
  typing with the guard rails (a guess from the style, in a customer's words, `neutraal` when nothing shows; never a
  diagnosis; tone and choices only), `**Waar:**` the town or suburb (or "not given"), `**Reeds gesê:**` what the user
  already said that the specialist needs (the dish, the number of people, the budget; "nothing" when none). A
  continue's note is the same shape with what's new. The roles' shared base: match the note's tone, keep the shape.
- **The card as a thread:** `Delegation` reads the note into fields (`Note` with its lines; `Mood` from the
  `Klink`/`Sounds` line) and keeps `Task` as the raw text; `DelegationCard` renders Puls's note as its message
  (a bubble on Puls's side, the labels as small captions, the mood as a chip), the work as today, and the reply as
  the specialist's message under the role's name. A task without the labels renders as today. The Werk panel shows
  the mood chip beside the task.
- **The switch** (Louis, before the build: "add an option for it, if the user wants it; they should be able to put
  this on or off during a session"): the system prompt can't change mid-chat (FX4, history is append-only), so the
  switch travels with the turn, as the user's place does (F22). `POST /sessions/{id}/messages` takes
  `read_mood` (default true); when false the API puts one marker line before the user's text
  (`ApiModelText.MoodOff`: the mood isn't read this turn and the note has no `Klink` line), and the router
  documents the marker. On is the prompt's default, so a host that sends nothing gets the mood. The chat has a
  toggle, *Lees my stemming*, per chat, changed any time and sent with every message; the chat shows the text the
  user typed, never the marker. Rejected: a session setting changed by a second endpoint (it would have to edit the
  system prompt or be re-sent anyway) and a user command (`/stemming af`: a hidden switch, and a command the model
  never sees).
- **The checks:** `briefed` (the message of the first `StartSubSession` has at least `Wil hê`/`Wants` and
  `Klink`/`Sounds`), `mood` (the `Klink`/`Sounds` line's text), in both checks' scores and tables; the portal's
  tree shows the mood on the conversation.

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; TODO line; index rows | — |
| 1 | Prompt: the note's shape, the mood rule and the off marker in `default.md` and `agents-skills.md`; the roles' shared line | prompt tests first; golden files |
| 2 | API: `read_mood` on the message endpoint; the marker line when it's off | API tests first (the prompt the engine gets, with and without) |
| 3 | The chat's *Lees my stemming* toggle, sent with every message; `Delegation.Note`/`Mood`; the card as a thread; the Werk panel's chip | the shared project has no test project: the parsing is checked by hand in the chat and by step 4's scorer on the same text |
| 4 | Both checks score `briefed` and `mood`; the portal's tree shows the mood; saved samples re-reported | `--report` on the saved samples |

## Verification

- A cook question typed tersely and one typed at length, by hand: the card shows the two notes with different moods,
  the cook's dish and tone differ, the report keeps its shape.
- The toggle off, then the same question: the card's note has no mood line; on again mid-chat: it's back, with no 400
  (the system prompt never changed).
- A run of each check from the portal (Louis's go; about R1 each): `briefed` 12/12 in the delegation check's
  delegated mode, 6/6 in the cook check's.

## Approved during the build

(none yet)
