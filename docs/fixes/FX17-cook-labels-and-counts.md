# FX17 · The cook wrote "Serves" in Afrikaans, "Geprys" in English, a count before a photo, and a follow-up's card said "sub-1"

> **Status:** done · **Area:** prompt, `specialists/puls-cook.md` (F25, F34); API stream (`message_start`) and the web
> apps' delegation card (F5, F26) · **Found:** 2026-10-18 · **Fixed:** 2026-10-18 ·
> **Plan:** [plans/FX17](../plans/FX17-cook-labels-and-counts.md)

## Symptom

Three things, the first two from cook check `d94597a51d4d` (F34), the third from Louis's chat the same night:

- **The labels in the wrong language.** Every Afrikaans report's recipe line read *Serves 4 · ongeveer 1 uur*, and the
  English report (cook-en, Pretoria) closed its list with *Geprys: 5 van 5*.
- **A count before the photo.** One row of the "for six" follow-up read
  `2 × ![Miami Diced Tomatoes](…) Miami Italian Style Diced Tomatoes 410g`, its photo no longer first.
- **A follow-up's card named the handle.** When Puls continued the cook, its card said *Oorgedra aan sub-1*, and the
  line under it *BESIG*, where a new sub-session's says *puls-cook* and *Soek bestanddele* / *Bou resep*; the role
  only appeared once the cook answered.

## Cause

- **The labels:** the prompt gave each in one language only (`Serves {n} · {total time}`,
  `Geprys: {n} van {m} bestanddele`), and the model copied it whatever the task's language.
- **The count:** the photo rule said the photo comes first, then the name and pack, but not where a count of packs
  goes, so the model put it in front.
- **The card:** it names the role the call gives, else the handle (`Delegation.Role ?? Delegation.Handle`).
  `ContinueSubSession("sub-1", …)` names only the handle, and the role reached the card only in the answer's footer;
  the status line (`WorkStatus`) needs the role too, so it fell back to *BESIG*.

## Fix

- **The cook's prompt** gives each label in both languages and says to write the task's (*Dien* / *Serves*,
  *Geprys* / *Priced*), the way it already gives the no-catalogue and pantry lines, and it says where a count of packs
  goes: after the name and pack, and after the pack's price, never before the photo; the total counts every pack.
  Examples, not a rule alone: the model copies an example more reliably than it follows an instruction.
- **Each session's `message_start` carries its role** as `session_role` (`role` is already Claude's speaker,
  `assistant`), beside its toolsets (F34). The web apps keep it on the turn (`Turn.Role`), and `Delegation` takes it
  for a specialist when the call names none; the call's own role still wins. So a follow-up's card names *puls-cook*
  and its line the cook's phase while it works. A chat saved before this has no `session_role` in its stream, so its
  follow-up cards still show the handle.
- **To take effect:** the prompt is embedded in core and the card is the API's stream and the web apps, so every image
  that serves a chat is rebuilt: Puls (`puls`, `puls_agent`) and the code-review API (`louis_agent_api`).

## Tests

Written first and seen failing, each on the old text or code:

- `RoleCatalogTests.TheCooksLabels_AreInTheTasksLanguage`: failed on "`Dien {n} · {tyd}`" missing; also holds that the
  English-only recipe line is gone and both tallies are there.
- `RoleCatalogTests.ACountOfPacks_GoesAfterTheName_SoThePhotoStaysFirst`: failed on the rule missing; also holds the
  `× 2` example.
- `ContinuedSpecialistRoleTests` (three): the stream's start carries `session_role` (failed: no field), a follow-up's
  card names *puls-cook* (failed: null), and its line says *Soek bestanddele* (failed: *BESIG*).

The golden files didn't change: they pin Puls's prompt and tools, not the cook's (checked each step; only their line
endings moved, and were restored).

## Confirmed

- **The card:** by the tests so far. Puls is rebuilt at `72643ff`; a follow-up in a chat there is the hand check (it
  calls the model, so it waits for Louis's next chat), and is to be recorded here.
- **The labels and the count:** by the prompt's tests only so far; whether the cook follows them shows in the next cook
  check (paid, Louis's go), which also scores the follow-up for six. That check looks for:
  - *Dien* / *Geprys* in the Afrikaans reports and *Serves* / *Priced* in the English one (cook-en);
  - every count after the name and pack, the photo first in its cell;
  - **a count with a multi-buy** (review, should 3): when the recipe needs two packs of something on a multi-buy, how the
    row reads (`… 410 g × 2` and `[R24,99](…) × 2 · BUY 2 FOR R45`), and whether its total is the deal's price or two
    packs at one pack's price;
  - **how `[R24,99](…) × 2` reads** (review, should 4): one pack's price with its count, not R24,99 for both, and the
    total counting both packs.
- **A follow-up's card by hand:** after Louis's next chat with a follow-up to the cook, recorded here.

## Lesson

A label the model is meant to translate has to be shown translated: one literal form is copied as it stands, whatever
the task's language (skill 3, *Prompt design*). And a card that names who it's waiting on needs that name from the one
who's working, not only from the call that sent the work: a call can be a follow-up that never names it (skill 10,
*Multi-agent orchestration*).

## What the build changed

- **Step 1 (2026-10-18): the labels in the task's language.** `puls-cook.md` gives the recipe line as
  `Dien {n} · {tyd}` / `Serves {n} · {time}` and the tally as `Geprys: {n} van {m} bestanddele` /
  `Priced: {n} of {m} ingredients`, each "in the task's language, never the other"; the pantry line's example names
  both tallies.
- **Step 2 (2026-10-18): a count after the name.** When the recipe needs more than one pack, the count goes after the
  name and pack (`… Miami Italian Style Diced Tomatoes 410 g × 2`) and after the pack's price (`[R24,99](…) × 2`),
  never before the photo; the total counts every pack, as the run's follow-up already did.
- **A follow-up's card (2026-10-18, Louis: "this must say puls-cook?", then "put it in the FX17 branch").** Each
  session's `message_start` carries its role (`session_role`), so the card names *puls-cook* and the line the cook's
  phase while it works.

---
[Fixes](README.md) · Previous: [FX16](FX16-short-quote-match.md)
