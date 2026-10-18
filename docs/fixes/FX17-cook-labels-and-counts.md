# FX17 · The cook wrote "Serves" in Afrikaans, "Geprys" in English, and a count before a photo

> **Status:** in progress · **Area:** prompt, `specialists/puls-cook.md` (F25, F34) · **Found:** 2026-10-18 ·
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

## What the build changed

---
[Fixes](README.md) · Previous: [FX16](FX16-short-quote-match.md)
