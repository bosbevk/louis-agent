# FX228 · Orbi still knows the two free websites after their page went

> **Status:** open · **Area:** the guide, its voice and its skills (`LouisAgent.Identities.Guide`) · **Found:** 2026-11-23

A fix's record: what was seen, why it happens, and what is still to do. It answers why Orbi can still offer a free
website when the site no longer does.

## Symptom

FX227 took the offer of two free websites off the site: its page at `/free-websites`, the line under the landing's
buttons and its sitemap entry. Orbi was not changed with it. Its voice still holds the paragraph that opens "Two
free websites, and you take the entries", its index of pages still lists `/free-websites` as a page it may open, its
map of the site still describes that page, and its `free-website-entry` skill still finishes an entry and mails it.
So a visitor who asks Orbi about a free website can still be told there is one, and be sent to an address that no
longer answers.

## Cause

The offer was written into what Orbi reads, in four files under `agents/louis-agent-guide/`: `voice.md`, and the
skills `site-pages`, `site-map` and `free-website-entry`. The `walk-a-page` skill uses the page as its example.

They could not be changed in the same pull request as the page. The guide's eval is replayed from recordings kept
in git, and a recording answers only the request it was made with. With the offer taken out of the voice, the
replay stopped at its first turn with "No recorded model answer for this request in trial
'20261123-071526-guide/guide-what-is.1': the system prompt changed. The model was not called." A recording is made
again only by a live run, which calls the model with a real key and so costs money, and the thread that removed the
page had no key. So what a visitor sees was shipped first, and Orbi's part waits for the live run.

## Fix (to do)

The change is written and kept as a patch, `/mnt/project-files/plans/FX228/orbi-forgets-the-offer.patch` in the
Claude project's files. It takes the paragraph and its row in the skills' table out of the voice, deletes the
`free-website-entry` skill, takes the page out of the index and the map, and gives the three examples that used
the page another one, the trust page. Its test replaces the one that held the offer in place: nothing Orbi reads
names the offer, and the entry skill is gone.

What is left is to apply the patch, run the guide's eval live once so its recording is made again
([Run an eval](../guides/run-an-eval.md)), and commit the new recording with the change.

## Tests

`OrbiVoiceTests.TheFreeWebsites_AreClosed_OrbiNoLongerKnowsThem`, in the patch, was seen failing against today's
prompts. `GuideEvalReplayTests` is the test that turns red until the recording is made again.

---
[Fixes](README.md) · Previous: FX227 in [the table](README.md#fixes)
