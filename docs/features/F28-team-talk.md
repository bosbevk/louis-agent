# F28 · Team talk: Puls briefs its specialist, mood included, and the chat shows the exchange

> **Status:** planned (2026-10-15), on `claude/project-thread-381vmw` · **Milestone:** — · **Depends on:** F5
> (sub-sessions), F21 (roles), F25 (the cook), F26 (follow-ups), F27 (the checks the portal runs)
>
> **In the TODO:** **F28 Team talk, the rest** under *Puls* (step 1 built; the mood switch, off by default) ([TODO](../../TODO.md)) ·
> **Plan:** [plans/F28](../plans/F28-team-talk.md)

Puls sends a specialist a task and gets a report back (F5, F26). The task is whatever Puls writes: sometimes the
user's words, sometimes a line of its own, and nothing about the person asking. Louis's ask (2026-10-15): Puls should
read from the way the person types how they're feeling, and tell the specialist; and the chat should show that
exchange, Puls telling its specialist what the person wants and the specialist answering, the way a person with a
team would be seen working.

## User stories

**F28-S1 — Puls briefs the specialist like a colleague.** As a *user*, I want the specialist to know what I want, how
I sound and where I am, so that its answer fits me and not only my question.
- *Given* I ask for a meal in a hurry, terse, no greeting *when* Puls hands it to the cook *then* the task is a note
  in a fixed shape (for whom, what I want, how I sound, where I am, what I already said) and "how I sound" says
  rushed, so the cook picks a quick dish and keeps its two sentences short.
- *Given* I write at length, relaxed, with time *then* the note says so and the cook can suggest a Sunday dish.
- *Given* my typing says nothing much *then* the note says "neutral", never a guess dressed as a fact.
- *Given* any mood *then* the report keeps its shape (F25): the mood changes tone and choices, not the sections.
- *Given* I switch *Lees my stemming* off in the chat, at any point *then* from the next message Puls doesn't read
  my mood and the note has no "how I sound" line; on again, it's back, in the same chat.

**F28-S2 — The chat shows the exchange.** As a *user*, I want to see Puls tell the specialist what I want and the
specialist answer, so that I can follow who did what and correct Puls when it read me wrong.
- *Given* a delegation *when* its card is open *then* it reads as a thread between two people: Puls's note as its
  message to the specialist, with the mood line visible, the specialist's work under it, the specialist's reply as its
  message, under the role's name; then Puls's own line in the chat ("my kok is terug…", F25).
- *Given* the Werk panel *then* it shows the mood Puls read beside the task.

**F28-S3 — The checks measure it.** As *Louis*, I want the cook and delegation checks to say whether Puls briefed the
specialist in the shape, so that a prompt change that loses it is seen.
- *Given* a run *then* each conversation scores `briefed` (the note has its labels) and records the mood Puls read.

## Design notes

- **A read of style, not of the person.** The prompt says what the mood is: a guess from the typing (length, greeting,
  punctuation, tone of the words), named in plain words a person would use about a customer ("klink haastig", "het
  tyd, lekker in die gees", "gefrustreerd na die vorige antwoord"), and "neutraal" when the typing says nothing. It
  is never a diagnosis, never stored outside the chat, and the specialist uses it for tone and choices only.
- **A fixed shape, so three readers can use it:** the specialist (it reads the note), the card (it shows the fields
  and picks out the mood), and the check (it scores the labels). The labels are in the task's language, Afrikaans or
  English, so the note reads as a message and not as a form: `**Vir:**`, `**Wil hê:**`, `**Klink:**`, `**Waar:**`,
  `**Reeds gesê:**` / `**For:**`, `**Wants:**`, `**Sounds:**`, `**Where:**`, `**Already said:**`.
- **Nothing is simulated.** The note is the message Puls really sends (`StartSubSession`'s `message`, or
  `ContinueSubSession`'s), and the reply is the specialist's real answer; the card renders what the stream already
  carries (F5's live view). A continue's note has the same shape, shorter: what the user now asks and how they sound.
- **The switch is the user's, per turn.** The system prompt can't change mid-chat (FX4), so the chat sends
  `read_mood` with each message and the API puts a marker line before the text when it's off; the prompt's default
  is on. Nothing is stored about the user beyond that turn's message.
- **The specialists' shared base** gets one line: the note from Puls says how the user sounds; match it in your tone
  and your choices, and keep your report's shape.
- **Cost:** a note is a few dozen tokens longer than today's task line, on the sub-session's first request only; the
  system prompt doesn't change, so the cached prefix is untouched.

## Done when

- **F28-S1:** prompt tests on the router, the agents guide and the roles (the shape, the mood rule, the guard rails);
  the golden request snapshot regenerated.
- **F28-S1, the switch:** API tests on the prompt the engine gets with `read_mood` false and true; the toggle checked
  by hand mid-chat, no 400.
- **F28-S2:** the card and the Werk panel render a note's fields and the mood, in both apps' shared component, with
  the old free-text task still shown as before; checked by hand in the chat.
- **F28-S3:** both checks score `briefed` and record the mood; the saved samples re-reported (older runs score false).

---
[Features](README.md) · [Plan](../plans/F28-team-talk.md)
