---
name: fix-a-bug
description: Fix something that broke, from its first commit to its record. Use it when a bug is found or reported, when Louis asks for a fix, or when an FX id is given or needed. It holds the fix's own branch, the test that reproduces the bug, proving the cause, bundling small fixes, and the list a fix finishes by, with its rows and its lesson.
---

# Fix a bug

A fix repairs something that didn't work, and leaves a record that the next person finds by the error they see.
Four stops come first.

1. **A fix is on its own branch from its first commit**: `fix/FXnn-short-name`. Louis, 2026-10-29: "this fix
   should have been on branch fx26". Make the branch before the plan or the first step is committed.
2. **The test that reproduces the bug comes first, and you see it fail** (ADR 0014). Then the fix.
3. **The cause is proved, not guessed.** The record says how it was found.
4. **Never rewrite pushed history to move a fix.** If its first commits went onto the wrong branch, ask Louis where
   it goes from here.

## Where the branch starts

- **From `main`**, after a fetch, when the broken code is on `main`.
- **From the feature branch's tip**, when the code it fixes exists only on a feature branch that is not merged.
  The fix then merges back into that feature, or into `main` after it.
- **Not clear which: ask Louis**, with `/put-a-finding`. Where an earlier fix was built is not a rule to copy.

## Small fixes share a branch

Louis, 2026-11-08: "can we start bundling small fixes". A batch of small fixes shares one branch, such as
`fix/FX94-96-site-touches`, and one pull request. Inside it every fix keeps its own commit, its own FX id, its own
row in the fixes table and in the work log, and its own test.

A fix goes alone when it changes behaviour people depend on, or when it needs its own deploy to check.

## Starting it

1. **Its id** is `FXn`, the next number, in the order the problem was found. Fetch first, since `main` may have
   taken a number. The fixes table in `src/docs/fixes/README.md` and the TODO show the ones taken.
2. **Its row in the fixes table**, with *Status* open, and the symptom as it was seen: the exact error, where there
   was one, so a search for the error lands on the row.
3. **Its doc**, `src/docs/fixes/FXnn-short-name.md`, with *Symptom* (what was seen, with the exact error), *Cause*
   (proved, and how it was found), *Fix* (what changed and why this way), *Tests*, *Confirmed* (the real run that
   shows it works) and *Lesson* (the concept behind it and its skill in the goal's map). A fix that is found and
   finished in one pull request is folded at once, by step 5 of the list below, so its row and its lesson are
   what is left.
4. **An open fix has its row in the TODO too**, and so in the work log's open table, with their counts. A fix that
   removes a known limitation names it.
5. **A plan**, only when the fix takes more than one step. Use `/plan-a-feature`.

## Building it

Each step is built with `/do-a-step` and finished with `/finish-a-step`. Its commits are
`fix(fx14): what changed`.

## Finishing a fix

In the commit that fixes it, or the one right after:

1. **The fixes folder** (`src/docs/fixes/`): *Status* to **done** in its README's table and in the doc; fill in *Fix*, *Tests* (the test that reproduced the bug,
   written and seen failing before the fix) and *Confirmed*; add the commit.
2. **TODO and the work log:** remove its TODO row, and add its row (id, date, what in a few words) under *Fixes*
   in the [work log](../../../src/docs/work-log.md), newest first, with the count above (`WorkLogTests` holds the counts and that
   every fix in the fixes table has a row).
3. **Limitations:** if it removes a known limitation, delete it from the TODO's
   [Known limitations](../../../TODO.md#known-limitations) (FX4 removes #9).
4. **Docs:** the design doc and the how-to of every project the fix touched, read against the code as it is now, and
   a picture that no longer shows it redrawn from its JSON ([Draw a diagram](../../../src/docs/guides/draw-a-diagram.md)); anything
   the fix changes for users (settings, MODELS, SETUP); and an [ADR](../../../src/docs/adr/README.md) if the fix made a lasting
   decision (FX1's append-only rule is [0008](../../../src/docs/adr/0008-history-is-append-only.md)).
5. **Fold and delete** (Louis, 2026-10-29): the fix's row in the fixes table keeps its exact symptom (the error as
   it was seen) and its cause in a sentence, so a search for the error still finds it; the lesson goes under the
   fix's own heading in [Lessons from the fixes](../../../src/docs/learning/fixes-lessons.md), every fix, however small (Louis,
   2026-11-13; `WriteUpsTests` fails for a done fix with no heading there, and the lessons owed on that day are
   listed in it), and at more length in its skill's write-up where it has one; anything lasting about the code goes
   into its project's design doc; then the fix's doc and its plan are deleted. The docs folded on 2026-10-29 are at
   the tag `pre-cl24`.

## Then

`/ship-it`. `main` is merged in and the full suite of unit tests passes before the pull request.
