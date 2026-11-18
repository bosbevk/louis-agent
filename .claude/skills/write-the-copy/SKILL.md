---
name: write-the-copy
description: Works as the writer, a copywriter with a screenwriter's craft, for every word a visitor reads. Use whenever a heading, a summary, a tile, a button, a link, a page's title or description, an alt text, a footer line, or what the guide says about a page is written or changed. Holds where each word lives, the rules for web text with their sources, the tests that pin words, and how a change is shown. What to say comes from research-the-market, where it goes from design-a-page.
---

# Write the copy

The writer turns a picked message into the words on the page, as a copywriter and a screenwriter would (Louis,
2026-11-18: "people that know how to write"). Four stops come first.

1. **Say only what is true today.** A claim the code, a test or the ledger doesn't back stays out. No number
   nobody measured, no price (Louis, 2026-11-17: "this is remove"), and nothing the brand guide's voice rules
   out ("revolutionary", "magical", "next-gen").
2. **The message is picked first.** Slogans, headings and the promise come from `/research-the-market` and
   Louis's pick. The writer writes them well, never invents a new message on the way.
3. **One word for one thing.** The agent, the mark and the business keep one name each across every page, the
   metadata and the guide's own prompts. A new name is a finding for Louis.
4. **The test changes first.** Most words on the site are pinned by a test. Change the test to the new words,
   see it fail, then change the page (`/do-a-step`).

## How people read a page, and what follows

People scan before they read. In NN/g's study of a site written five ways, concise text measured 58% more usable,
scannable text 47%, objective text (no boasting) 27%, and all three together 124% (Morkes and Nielsen). Long
sentences lose readers: at an average of 14 words they understand more than 90%, at 43 words less than 10%
(GOV.UK, citing Ann Wylie). Pages written at a grade 5 to 7 reading level converted at about twice the rate of
professional-level writing (Unbounce, 2024). So:

| Rule | How | Source |
|---|---|---|
| Short sentences | Split any sentence over 25 words; aim near 14 | GOV.UK, *Use clear language* |
| Short paragraphs | At most five sentences, one idea each | GOV.UK |
| Plain words | "buy" not "purchase", "help" not "assist"; no buzzwords; a needed term explained the first time | GOV.UK |
| Active voice | "It answers your customers", not "Customers are answered" | GOV.UK |
| Front-load | The word that matters starts the heading, the tile and the line | NN/g, F-pattern |
| No boasting | Facts over adjectives; show it working instead of praising it | NN/g, objective writing |
| Speak to the reader | "you" and "your business"; the reader is the hero, we are the guide | StoryBrand |

## The craft: how good writers work

These are the habits of copywriters and screenwriters, used on a web page.

- **The headline does most of the work.** Ogilvy found that, on average, five times as many people read the
  headline as the body (*Confessions of an Advertising Man*). Write the heading and the summary first, and many of
  them; pick the best one after.
- **Meet the reader where they are.** Eugene Schwartz's five levels of awareness (*Breakthrough Advertising*):
  unaware, problem aware, solution aware, product aware, most aware. A business owner who has never heard of an
  agent on a website is problem aware at best, so the page opens on their problem ("Your doors close at five"),
  not on our product's name.
- **Steal the customer's words.** Copyhackers' voice-of-customer research: the best lines are often what
  customers already say in reviews, forums and interviews. Look for how a South African business owner says the
  problem, and use those words over ours.
- **Show, don't tell.** A screenwriter shows what a character does instead of saying what they are. On our site
  the demos and the guide do the showing; the words point at them ("Watch it answer") instead of praising them
  ("powerful", "smart").
- **Know the logline.** A screenplay can be said in one sentence before it is written. So can a page: who it is
  for, what changes for them, and what they do next. Write that sentence before the page; every section serves it.
- **Enter late, leave early.** A scene starts as late and ends as early as it can. A section starts at its point,
  with no warm-up ("In today's world…"), and stops once it has made it.
- **Every line earns its place.** Read each line and ask what it adds. If cutting it loses nothing, cut it.
- **Read it aloud.** Dialogue is tested by being said. A line you trip over aloud is rewritten.
- **Write, then rewrite.** The first draft finds what to say, and the rewrite says it well. Show Louis the
  rewrite, not the draft.

## Each kind of text

- **A heading** is in sentence case, says the idea of its section, and ends with no mark except a question
  mark, unless the site's style uses a full stop for its band titles (it does: "Ask the website."). Keep that.
- **A button** starts with a verb and says what happens: "Ask for yours", "Watch one work". Never "Submit",
  "Learn more" alone or "Click here" (Mailchimp, *Web elements*).
- **A link** names where it goes. No "click here" or "read this", no "the" or "our" inside the link (Mailchimp).
- **A page's title and description** make sense alone in a search result or a link preview, name the reader's
  need, and are unique per page (`PageTitlesTests`, `PageDescriptionsTests`, `get-a-site-found.md`).
- **Alt text** says what the picture shows, in a short sentence; a picture that only decorates says nothing.
- **A summary** under a heading is one to three short sentences: what it is, then what it means for the reader.
- **Spelling** is British English, as the brand guide and the site write it ("colour").
- **Requirements** use "must" only for law, "need" for a step, "can" for an option (GOV.UK).

## Where the words live

| Words | File |
|---|---|
| A page's sections and tiles | `src/site/louis-agent.site/Pages/*.razor` and `Components/` (a page holds only framework controls, ADR 0125) |
| A page's title and description | `<PageTitle>` and `<PageHead Description=…>` in the page |
| The tab, the link preview and the social card | `src/site/louis-agent.site/wwwroot/index.html` |
| The header tagline and the footer | `src/site/louis-agent.site/Layout/SiteLayout.razor` |
| What the guide knows about the pages | `src/site/louis-agent.identity.guide/prompts/agent/louis-agent-guide/skills/site-pages.md` and `site-map.md` |
| How the guide speaks | `…/louis-agent-guide/voice.md` |

A change to what the guide reads regenerates its golden files (`LOUIS_UPDATE_GOLDEN=1`, `.claude/rules/prompts.md`),
and `site-map.md` must say what the page now says. `WhatIsTrueTests` keeps stale claims off every page; a claim
that stops being true joins its list.

## Checks before it is shown

- Every sentence on the change under 25 words; count the longest and say it.
- Read each heading and button alone: does it still make sense?
- Read the first screen in ten seconds: is it clear what this is and what to do?
- Search the site and the guide's skills for the old words, so nothing still says them.
- Look at it at 1920 by 1080 and 390 wide (`/check-a-page`): a line that wraps badly is rewritten, not squeezed.

## Handing it over

Show the words before and after side by side, with the rule or the research each change follows. Wording goes
on its own branch and pull request, wording only (another thread owns the styles), through `/ship-it`.
