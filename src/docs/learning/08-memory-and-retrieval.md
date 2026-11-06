# 8 · Memory and retrieval: what the recipe library taught

> **Skill:** #8 in the [goal](../GOAL.md)'s map · **Stage:** F35.3 The recipe library (2026-11-05) · **Next:** search by
> meaning (F35.6), and a counter on the admin page (F35.7)

This skill is usually about embeddings and vector stores. The first lesson came before any of that: what you keep, and
whether anyone uses it. We built the simplest memory there is, a list of recipes and keyword search, and measured it.

## Before and after

| | Library off | Library on |
|---|---|---|
| Judge passed | 43 of 50 (86%) | 40 of 50 (80%) |
| Time per answer | 56 s | 55 s |
| Cost per answer | $0.00633 | $0.00649 |
| Biggest site's share | 20% | 14% |
| Pages new to the run, judge | 29 of 36 | 29 of 36 |

And the 9 answers built from a library copy, against the same dishes with the library off:

| | From the library | Same dishes, library off |
|---|---|---|
| Time | 45 s | 56 s |
| Cost of the cook's work | $0.00556 | $0.00617 |
| Judge passed | 8 of 9 | |

So it failed the win test overall, and helped everywhere it was used. Both are true, and the gap between them is the
lesson: the cook read 10 of the 69 recipes it was offered. We could have pushed it to read more: our links in the first
two places were read 24% of the time, and 8% lower down. Louis chose not to, because finding new recipes matters more
than the saving. A win test measures what you said you wanted, and sometimes the honest answer is that you want
something else more.

Then a full library changed the picture, with no push at all:

| | Library off | Library on, from empty | Full library |
|---|---|---|---|
| Judge passed | 43 of 50 | 40 of 50 | 42 of 50 |
| Time per answer | 56 s | 55 s | 52 s |
| Cost per answer | $0.00633 | $0.00649 | $0.00628 |
| Answers built from a copy | — | 9 | 31 |

The cook read a copy once there were copies worth reading: a memory earns its keep when it's full, and a test on an empty one
mostly measures the filling.

## What changed

- **Each new read from a site in our table is kept for good, as a proper recipe:** field by field, from the page's data or
  its plain headings. Its link stays the site's, so the credit does too.
- **The library is a source, not a cache.** A search hands the cook our API's address, and it reads the recipe over HTTP as
  anyone could. The open feed's first customer is ourselves.
- **Sites are still asked every time,** and 3 of a search's 10 places stay for pages from outside the library, so new
  recipes keep arriving: 10, 5, 8, 7 and 6 new pages across the five repeats.
- **A page with no recipe is excluded, in Mongo:** opened once, refused, and never offered or opened again.
- **Pap en sous** taught that a dish can go by names a site's search can't guess, so a search for it now says what else to try.

## What didn't work

- **A memory nobody reads saves nothing.** Offers grew faster than reads: the cook picks on how well a title fits, and can't
  tell that ours is quicker. Storing was the easy part. We measured "kept" for an hour before asking whether it was used.
- **Trying the reader on made-up pages missed three bugs.** Real pages showed a cookie notice kept as a step, an oven step
  stored as a part's name, and an instruction stored as an ingredient. The second had already reached the real library, and
  a memory that never goes stale keeps its mistakes for good, so we had to delete two records by hand.
- **Only the cook's pages had been tested.** A check of every site, a few pages each, found more in an hour than the runs had:
  a reader too strict on wordings a real page uses, labels stored as steps, a crash on an attribute written twice, and a site
  that gives no text at all. Counting what saved wasn't enough either: reading the results found the wrong ones.
- **Comparing two counts isn't checking.** "10 pages and 10 kept" looked like every read saved. It wasn't: 13 of 50 answers
  were on pages never kept.
- **The first read of the numbers was unfair.** A running 77% against a finished 86% looked like the library hurting. At the
  same point the runs were two answers apart, and the split by source showed only 1 of 10 failures on a library copy.

## What it taught, in one line each

- Measure use as well as storage: "saved" and "served" are different numbers, and only one of them pays.
- Split a result by what actually differs: the overall table said *worse*, and the split said *better where it applies*.
- A memory that never expires needs a stricter gate than a cache, since nothing ever corrects what it holds.
- When in doubt, keep nothing: a page left out costs one read, and a wrong recipe costs every read after it.
- Check what a source's terms say before keeping its pages: the site that did best in our runs says no to republishing.

## What's left

- About 11 of the 16 excluded pages are real recipes the reader can't see, so they want a better reader, and the list cleared.
- New pages fell from 10 to 3 a repeat: 10 dishes run out, and real chats are what will grow it.
- The cook's scaling, which is its most serious accuracy problem and has nothing to do with the library.

---
[Docs index](../README.md) · [Goal](../GOAL.md) · [All write-ups](README.md)
