---
name: research-the-market
description: Research the brand and the market before any words, slogan, positioning or offer are proposed. Use it when Louis asks for a slogan, a tagline, a heading, a brand audit, a competitor comparison, who a page is for, or "do market research", and before any copy for a site is written. It holds the order of the research, the sources that count, how rivals are compared, how options are put, and what is never claimed.
---

# Research the market

Louis, 2026-11-17, turning down five quick slogans: "no do proper market research". Then "if you are unsure
about something research it", and "be a researcher then". Four stops come first.

1. **Read what we have before reading anyone else.** Every claim about our brand comes from `main` after a fetch
   (`git fetch origin main`, then `origin/main`), never from memory or a screenshot. Louis, 2026-11-17: "you must
   compare against what is on main".
2. **No proposal without sources.** Every option names what it rests on: a rival's page read today, a study, a
   law. A quick list from memory is not research.
3. **Nothing invented, nothing unmeasured.** A number on our site comes from the ledger or an eval run. A
   statistic from the web is used only when its source can be traced. A vendor's untraced figure is named as
   weak and left out.
4. **Price is out** unless Louis brings it back (2026-11-17, of the audit's price row: "this is remove").

## The order

1. **Inventory.** List what the brand holds today: the name, mark, colours, type, voice, every slogan in use and
   where, the page's sections in order, the metadata a link shows (`index.html`, `PageHead`, the social card),
   contact, privacy and proof. The brand guide is `src/docs/brand/README.md`; the site is `src/site/`.
2. **Who it is for.** April Dunford's five parts, in her order: the alternatives a customer would use instead
   (often doing nothing), what only we have, the value that brings, who cares most, the category. A part we
   can't answer is a gap, not a guess.
3. **Rivals.** Two to four direct rivals plus one or two strong brands of the kind (NN/g's competitive review).
   For each, read the home page itself and quote the hero, the subhead and how it earns trust: logos, numbers,
   reviews, prices, badges, a founder, local presence. Note what nobody does, since that is where we can stand.
   For South Africa, look for a local rival too.
4. **Technique.** What the evidence says about how people read a page: NN/g on the first 10 seconds and the first
   two screens, the F-pattern, plain words (Unbounce's reading-level data), interactive demos (Navattic),
   social proof (CXL), and the copy structures (PAS, AIDA, the 4 Ps, StoryBrand). Use only what applies.
5. **Gaps, ranked.** High, medium, low, each with what it costs us and the source that says so.
6. **Proposals.** Each decision on its own, with three worked options (rule 10 in `src/docs/CLAUDE.md`): the
   words themselves, what changes on the site, a real company that works that way, the pros and the cons, and the
   one recommended with why. Decisions that build on each other go in order: who it is for, then the flow, then
   the one line, then the rest.

## Sources that count

| Kind | Use | Examples |
|---|---|---|
| A rival's own page, read today | What they say and how they earn trust | Their home page, quoted short |
| Usability research | How people read and decide | NN/g, Baymard |
| Conversion data | What converts, with its sample | Unbounce benchmark, CXL, Navattic (vendor data, say so) |
| Positioning and copy | How to frame it | April Dunford, Copyhackers, StoryBrand |
| Law and rules | What we must do | The Information Regulator and POPIA, the EU AI Act's Article 50 |

When a page won't load, say so and name it in the sources as not read. Never fill its place from memory. A legal
point is research, not advice: say plainly that it should be checked.

## What a slogan or heading must pass

- **Clear before clever** while nobody knows the name. It says what the thing does when the name doesn't.
- **Works out of context** (NN/g): read alone in a link preview or a search result, it still makes sense.
- **Plain words**, at about a grade 5 to 7 reading level, with no command aimed at the reader.
- **True today.** It promises only what works now (the brand guide's voice).
- **Ours.** Not a near copy of a rival's line. Search the exact phrase before proposing it.
- **One voice.** It sits with the slogans already in use, or the proposal says which of them it replaces.

## What is handed over

An HTML page published as an Artifact, in the brand's own look (Geist, Ultraviolet), with the inventory, the
rivals, the technique, the gaps and the sources, and a second page for the proposals. Each claim is linked to its
source. The reply in the thread gives the picks in one line each and links the pages.

## After a pick

The picked words go in on their own branch and pull request, wording only, through `/ship-it`. A slogan that
changes what the model reads (the guide's prompts) regenerates its golden files. Save the picks to project memory
so a later thread doesn't ask again.
