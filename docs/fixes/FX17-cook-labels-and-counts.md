# FX17 · The cook wrote "Serves" in Afrikaans, "Geprys" in English, and a count before a photo

> **Status:** done · **Area:** prompt, `specialists/puls-cook.md` (F25, F34) · **Found:** 2026-10-18 · **Fixed:** 2026-10-18 ·
> **Plan:** [plans/FX17](../plans/FX17-cook-labels-and-counts.md)

## Symptom

Cook check `d94597a51d4d` (F34): every Afrikaans report's recipe line read *Serves 4 · ongeveer 1 uur*, the English
report (cook-en, Pretoria) closed its list with *Geprys: 5 van 5*, and one row of the "for six" follow-up read
`2 × ![Miami Diced Tomatoes](…) Miami Italian Style Diced Tomatoes 410g`, its photo no longer first.

## Cause

The prompt gives each label in one language only (`Serves {n} · {total time}`, `Geprys: {n} van {m} bestanddele`), and
the model copies it whatever the task's language. The photo rule says the photo comes first, then the name and pack,
but not where a count of packs goes, so the model put it in front.

## Fix

The cook's prompt gives each label in both languages and says to write the task's (*Dien* / *Serves*, *Geprys* /
*Priced*), the way it already gives the no-catalogue and pantry lines, and it says where a count of packs goes: after
the name and pack, and after the pack's price, never before the photo. Prompt only; tested on the prompt's text
(`RoleCatalogTests`). Whether the cook follows it shows in the next cook check.

## What the build changed

- **Step 1 (2026-10-18): the labels in the task's language.** `puls-cook.md` gives the recipe line as
  `Dien {n} · {tyd}` / `Serves {n} · {time}` and the tally as `Geprys: {n} van {m} bestanddele` /
  `Priced: {n} of {m} ingredients`, each "in the task's language, never the other"; the pantry line's example names
  both tallies. The golden files didn't change: they pin Puls's prompt, not the cook's.
- **Step 2 (2026-10-18): a count after the name.** When the recipe needs more than one pack, the count goes after the
  name and pack (`… Miami Italian Style Diced Tomatoes 410 g × 2`) and after the pack's price (`[R24,99](…) × 2`),
  never before the photo; the total counts every pack, as the run's follow-up already did.

---
[Fixes](README.md) · Previous: [FX16](FX16-short-quote-match.md)
