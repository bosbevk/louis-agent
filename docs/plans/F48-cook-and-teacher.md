# Puls's cook: best home cook, best teacher

> **What this answers:** what we can take from [cooklang-skills](https://github.com/cooklang/cooklang-skills) (MIT)
> to make `puls-cook` the best home cook and the best cooking teacher, how each piece fits our engine, and what it
> costs. Research and plan only (2026-10-26); Louis builds it in Rider. **Lands after F18**, next to F35 (recipe
> sources) and the twelve Puls specialists. **F48; every decision settled by Louis on 2026-10-26** (below). The page
> with the diagrams drawn: <https://claude.ai/artifact/B6wBUZEJRSegNH36N7nzWB>. What comes after it:
> [PULS_COOK.md](../specs/PULS_COOK.md).
>
> **Updated 2026-10-26 17:45, Louis's pick: no Cooklang.** The recipe stays F35's own `Recipe` record (read from the
> page's schema.org data), and a small C# scaler and shopping list work on that record. Cooklang stays below as the
> background the idea came from; nothing from cooklang-skills is reused: we rewrite its ideas in our own words and code (Louis, 18:13).
> The steps and decisions below are updated to match.

## The short answer

I read all 14 skills, the Cook MCP server they call (`cook-mcp`, its tool list and its code) and the Cooklang spec.
Here's the honest picture:

- **cooklang-skills is a toolkit for keeping a folder of recipe files on your own laptop.** It's very good at that.
  It has nothing about *teaching*. So the "best teacher" half we design ourselves (further down), from cooking-school
  practice and learning research.
- **Six ideas are worth taking now**, two later, and the rest we skip. We take the *ideas*, not their code: our code
  stays C#, and their server is Rust and built for one person's folder, not a hosted chat with many people.
- **The biggest win is one move:** the cook writes its recipe as **Cooklang** (a tiny plain-text recipe format with
  the amounts inside the steps), and **code does all the maths**: scaling to the number of people, the shopping list,
  the total. Their rule, word for word, is "never do the arithmetic by hand". Today our model does every sum itself
  and our graders guess at its tables with regexes. After the move, the maths is right by construction and a grader
  can check the recipe like data.
- **It's cheap to run** (less than a tenth of a cent more per answer on Haiku 5.5) and about **9 small steps** to
  build. The paid part is one before-and-after eval run, on your go.

## What cooklang-skills actually is

Two things in one repo:

1. **14 skills**, one `SKILL.md` each: step-by-step guides an agent follows (write a recipe, check it, scale it,
   import it, plan meals, make a shopping list, track a pantry, nutrition...).
2. **cook-mcp**, the MCP server the skills call (an MCP server is a program that offers tools to any agent over a
   standard protocol). Rust, MIT, run with `npx @cookmd/mcp`. 14 free tools that work on local files, plus paid tools
   behind a cook.md account (nutrition, unit conversion, photo import).

A Cooklang recipe looks like this. The `@` marks an ingredient, `#` a pot or pan, `~` a timer, and the amount sits in
`{}` right where the step uses it:

```
---
title: Hoenderkerrie
servings: 4
locale: af_ZA
---
Braai die @uie{2}(fyngekap) in @kookolie{2%eetlepel} in 'n #groot pot{} tot sag en goudbruin, ~{10%minute}.

> Hoekom: die uie word soet as hulle stadig bruin word; te warm, en hulle brand bitter.

Voeg die @hoenderdye{1%kg} by en bruin hulle in twee rondtes, sodat die pot nie te vol raak nie.
```

From that one text, a program gets the ingredient list, the amounts, the pots, the timers and the steps. Nobody types
the ingredient list twice. That's the whole trick.

Things I found that the quick read didn't show:

- **"Everything runs locally" isn't the whole story.** `import_recipe` sends the URL or text to cook.md's own cloud
  (`/api/cookify/url` in `src/tools/cloud.rs`). That's a third party reading what our users ask for.
- **Nutrition is paid and English-only.** The nutrition service resolves English ingredient names only; Afrikaans
  names fail.
- **No Windows build**, and it works on *one folder per person*. Puls is many people in one hosted app, so we'd need a
  folder per chat. Workable in Docker on Linux, but it's the wrong shape for us.
- **CookLangNet**, the only C# Cooklang parser on NuGet, is from 2021 (.NET 5, 9K downloads, an older spec). Too stale
  to build on. The spec is small, so our own parser of the part we need is a few hundred lines with tests.

## Skill by skill

Every one of the 14, what it does, and what we do with it.

| Skill | What it does | Verdict | What we take, and where it goes |
|---|---|---|---|
| `cooklang-editing` | Write or fix a `.cook` file | **Take** | The cook writes its recipe as Cooklang. Its writing rules (one step per paragraph, prep notes in `()`, `>` for notes) go in `puls-cook.md`. |
| `cooklang-validation` | 7 rules a recipe must follow | **Take** | The 7 rules become code (`CooklangCheck`) and a grader. Rule 1, "no separate ingredient list", is the rule we already wrote by hand: ingredients once, in the shopping table. |
| `metadata` | The frontmatter fields | **Take, small** | A subset: `title`, `servings`, `time`, `source`, `locale` (`af_ZA` / `en_ZA`), `difficulty`. `source` is our *Link*. |
| `scale-recipe` | Scale to N people | **Take** | A `ScaleRecipe` step in code. Plus its best part, *what scaling doesn't cover*: a pinch stays a pinch, 4.5 eggs becomes "4 large", baking soda and salt don't scale straight, a double stew needs a bigger pot. Those become teaching notes. |
| `shopping-list` | Merge, group by aisle, minus the pantry | **Take** | A `ShoppingList` step in code: merges duplicates, groups by an SA supermarket's aisles, leaves out the pantry staples our prompt lists today. The total is summed in code, so "check the sum before you write it" leaves the prompt. |
| `report-authoring` | Jinja reports, a `db()` price lookup | **Take the idea** | Its cost example reads each ingredient's price from a data file. Ours: a dated estimates table (`as_of`, like `prices.json`), so an estimate comes from a table we keep, never from the model's memory. Unknown is `null`, shown as "?", never R0 (ADR 0007's rule). |
| `export-recipe` | Recipe to Markdown, JSON, HTML | **Take the idea** | "Render, don't retype": the tables in the answer are printed by code from the Cooklang, so the model never copies a number. |
| `recipe-import` | URL or text to Cooklang | **Take the rules** | F35 already reads the page's own recipe data (schema.org) for free, locally, which beats sending it to cook.md. We take its rules: keep the source, never invent servings, check the amounts survived, say what was unreadable. F35's `Recipe` record gets a mapper to Cooklang. |
| `recipe-search` | Find recipes by word or tag | **Take, small** | Its search tactics go into F35's guide: drop a word when nothing is found, try a shorter stem, try the other name. Our SA list: baby marrow / courgette, brinjal / eggplant, mielies / corn, mince / ground beef, *koljander* / *dhania*. |
| `pantry` | Track what's in stock, what's expiring, what can I cook | **Later, lite now** | Full tracking needs memory between chats (skill 8/9). Lite now: "I have chicken, rice and an onion" in the question counts as the pantry for that answer. A new eval scenario. |
| `meal-planning` | A week's plan | **Later** | Its *Choosing well* list is gold for SA households: quick dishes on weeknights, reuse the half bunch of coriander, perishables early in the week, cook 4 portions for 2 people on purpose. "R900 for a week for 4" is a strong Puls ask, after the single meal is solid. |
| `nutrition-reports` | Calories, macros, checks | **Skip (keep the rule)** | Paid, English-only. We keep its rule: **never state nutrition from memory**. The cook makes no calorie or health claims until we have SA data (the SAMRC food tables, licence to check). A should-not eval case. |
| `nutrition-goals` | Change a recipe to hit a number | **Skip** | Same reasons. Its "tiered edits" idea (change amounts first, then swap, then add or remove) is a good shape for budget swaps, though: we rewrite that idea for "make it cheaper". |
| `organize-collection` | Tidy a recipe folder | **Skip** | Folder housekeeping. Nothing for a chat. |

And one technique that runs through all of them:

- **Excuse and rebuttal tables.** Each skill ends with a small table: the shortcut the model will be tempted to take
  ("the edit is tiny, I'll skip validation") and why not. It's a prompt-design pattern: you name the rationalisation
  before the model makes it. We have real ones from the F18 runs: the dish over budget "because the page was good",
  a photo taken from another site, "nearest branch" with no place. They go in `puls-cook.md` as one table.

## The big move: Cooklang, and code does the maths

How it would work, in four parts:

1. **The cook calls a tool, `WriteRecipe(cooklang, serves)`, instead of typing the recipe into its answer.** The tool
   parses the Cooklang, runs the 7 checks, scales it to *serves*, builds the shopping list (pantry left out), prices it
   from the estimates table, and hands back the finished *Recipe* and *Shopping list* sections as markdown. A check that
   fails comes back as text with its reason (ADR 0005: failures carry their reason), and the model fixes it.
2. **The answer shape stays the same for the user.** Same five sections, same tables. Only who writes the numbers
   changes.
3. **The graders read the tool call, not the prose.** `ServesAsAsked` today hunts for "vir 4 mense" with a regex. With
   the recipe as data, a grader reads `servings: 4` and compares every amount to the page's recipe × the factor.
4. **F35 hands the cook a draft.** F35's `ReadRecipe` already plans to read a page's schema.org recipe into a
   `Recipe` record. A mapper turns that into a Cooklang draft, so the model *edits* a recipe (adds the cues, the whys,
   the SA names) rather than retyping one.

**The AI-engineering concepts:** this is **structured outputs** (skill 5: the answer as a schema, checked in code)
and **tool design** (skill 4: give the model a tool for what it's bad at, arithmetic). It also feeds **evaluation**
(skill 12): a fixed-rule grader on data beats a regex on prose, and it frees the model grader for what only a model
can judge, like "does this teach well".

## Where the pieces live in our engine

How this keeps our rules:

- **Engine, identities and packs (ADR 0027).** The recipes pack and the where-to-buy pack name no identity. The role
  file in Puls's identity is the only place that says "the cook writes Cooklang". The braaimaster, the hanyman and
  four more of the twelve specialists reuse the where-to-buy pack, which the specialists brainstorm already wanted
  first.
- **Packs stay convertible to MCP (ADR 0028).** `WriteRecipe` takes plain text and returns text; a failure is text
  with its reason. So the recipes pack could be served as an MCP server one day too.
- **Reuse before building.** Considered: cook-mcp (Rust, folder per person, cloud import), CookLangNet (stale), and
  F35's planned schema.org reader (ours, reused). We write only the Cooklang parser, checker, scaler and list, which
  nothing in C# offers today.
- **Choices are enums.** Units (`g`, `kg`, `ml`, `l`, `koppie`, `eetlepel`, `teelepel`) are an enum parsed once, with
  aliases (`cup` = `koppie` = 250 ml, the SA metric cup; `tbsp` = `eetlepel` = 15 ml).
- **No message text inline.** Every check's failure text is a message enum member (ADR 0009).

## The best teacher: what good cooking teaching looks like

cooklang-skills has none of this, so here's what cooking schools and learning research say, turned into rules the
cook can follow and graders can check.

1. **Teach the process, not just the recipe.** Recipes are examples; what transfers is knowing *what should be
   happening now* (the onions going soft, the sauce coating a spoon). Learners who only follow steps stall when their
   oven, pan or tomatoes differ.
   ([From recipes to processes](https://abcdsofcooking.com/pgs/from_recipes_to_processes__helping_learners_think_like_cooks.html))
2. **Cues, not just timers.** Every heat step says what to look, listen, smell or feel for: "until the edges go
   golden and it smells nutty, about 3 minutes". In Cooklang that's a timer plus a cue in the same step. *Grader:*
   every step with a timer also has a sense cue.
3. **One "why" at the decision points, not a lecture.** "Brown the chicken in two rounds: a full pot steams the meat
   grey instead of browning it." One line, at the step where a beginner goes wrong. In Cooklang, a `>` note.
4. **A few principles explain most dishes.** Samin Nosrat's four (salt, fat, acid, heat) and Michael Ruhlman's ratios
   (bread is about 5 parts flour to 3 water; a vinaigrette 3 oil to 1 vinegar) let someone fix and invent, not just
   follow. ([Salt, Fat, Acid, Heat](https://www.ift.org/food-technology-magazine/mastering-the-elements-of-good-cooking),
   [Ruhlman's *Ratio*](https://www.npr.org/2009/07/26/107019060/throw-away-the-recipe-learn-the-ratio-instead))
5. **Match the level, and fade the help.** Beginners learn best from a full worked example; for someone who already
   knows, the same detail gets in the way (the *expertise reversal effect*). So a beginner gets "dice: cut into 1 cm
   blocks" and one pan; a confident cook gets the ratio and two variations.
   ([Expertise reversal effect](https://en.wikipedia.org/wiki/Expertise_reversal_effect))
6. **Map a mistake to its cause.** "My bobotie was dry" leads to: too long in the oven, or too little egg custard,
   and how to tell which. That's a troubleshooting answer, a mode of its own.
7. **Small, safe variations.** "Make it yours: swap the raisins for chopped apricots, or leave them out." Two or three
   options, never an open "experiment".
8. **One lesson to keep.** End with the one thing that carries over: "Browning in rounds works for every stew, bredie
   and potjie." That's retrieval practice in one line.
9. **Safety is taught, not hidden.** Chicken cooked through (74 °C in the thickest part, or the juices run clear;
   to cite USDA's table in the step), rice not left out warm for hours, a pan fire gets a lid, never water.

### South African context

- **Measures:** the SA cup is 250 ml, the tablespoon 15 ml, the oven in °C (gas mark only if asked).
- **Words:** Afrikaans kitchen words used and explained once for beginners (*fyngekap*, *smoor*, *braai*,
  *geurmiddels*); local names for produce (baby marrow, brinjal, mielie meal, Rama, boerewors).
- **Power:** no load shedding for 408 days by June 2026 (Gauteng still has local cuts), so a no-power option (gas, a
  potjie, a Wonderbag to finish a stew) is offered when the user asks, not by default.
- **Budget first:** cheaper cuts and how to make them tender (that's the teaching moment: slow, wet heat).
- **Seasons:** southern hemisphere; what's cheap now.
- **Diet:** halaal, no pork, vegetarian, asked once and respected.

### A skill ladder with SA dishes

A ladder gives the teacher a "next step up". Each rung teaches one process, through dishes people here actually cook.

| Rung | The process it teaches | Dishes |
|---|---|---|
| 1 · Basics | Knife (dice an onion), boiling, simmering, eggs | Rice, *stywe pap*, scrambled eggs, *slaphakskeentjies* |
| 2 · Heat in a pan | Sweat vs brown, don't crowd the pan, deglaze | Mince curry, *frikkadelle*, chakalaka |
| 3 · Slow and wet | Braising, cheap cuts made tender, thickening | Tomato *bredie*, oxtail potjie, *hoenderpastei* |
| 4 · Sauces and custards | Starch and egg sets, gentle heat | Bobotie's topping, melktert, white sauce |
| 5 · Dough and fire | Gluten, rising, fire zones, resting meat | *Roosterkoek*, *vetkoek*, braai with hot and cool sides |

### How the level reaches the cook

Puls already passes the user's mood as a `Klink` / `Sounds` line. The level works the same way: a `Vlak` / `Level`
line (*beginner*, *home cook*, *confident*), which Puls fills from what the user said ("I've never cooked", "my first
potjie") or leaves out. No line means *home cook*. Until there's memory, nothing is stored.

### How we'd measure "teaches well"

- **Fixed-rule graders (free):** every timer step has a sense cue word; at least one `>` why-note on a dish with a
  known pitfall; no calorie or health claim.
- **A model grader with a rubric** (the F18 judge, calibrated against Louis's labels like 6a): explains the pitfall
  step, fits the level, no lecture for a confident cook, ends with one lesson.
- **Should-not cases** (6b's pattern): a confident cook asking for a quick dinner gets no basics; a "what's 200 g in
  cups" question gets an answer, not a lesson.
- **Before and after:** the same scenarios, today's cook vs the teaching cook, quality and cost, in the write-up.

## Decisions for you

Four choices this plan doesn't settle. Each has three options; I recommend one.

### Decided · Agent-written skills come later (Louis, 2026-10-26)

Louis asked whether the agents could just get the `CreateSkillFile` tools instead. Picked: **the maths stays C# with
tests (F48), the Cooklang and teaching know-how goes in `puls-cook.md`, and the cook writes its own skills only after
FX4 and ADR 0033's scopes land.** Why: a skill file is prose plus an untested bash, PowerShell or Python script; Puls
chats don't load the skills toolset (shared `/approve`, mid-chat 400s until FX4); and a skill the agent adds changes
the prompt, so before-and-after evals would drift.

### Decided · No Cooklang: the maths works on F35's record (Louis, 2026-10-26)

Louis: "I do not want to emulate Cooklang, I want to know what we can reuse." Options were cook-mcp through a small
MCP client, CookCLI as a command, or no Cooklang. Picked: **no Cooklang.** F35's `Recipe` record (name, serves,
ingredient lines, steps, photo, link) is the data; a C# scaler and shopping list work on it. Nothing from cooklang-skills is
reused, neither code nor text nor sample recipes (Louis, 18:13: "we will rewrite their ideas"). We take the ideas and
write them ourselves: *what scaling doesn't cover*, *choosing well*, the import rules, the search tactics, "never state
nutrition from memory", the excuse tables; our test recipes are our own SA dishes. D2 below is settled by this.

### Decided · Function skills follow Agent Skills (Louis, 2026-10-26)

Louis asked what the international standard for prompt files is. There's no ISO or W3C one; the open formats are
**Agent Skills** (`SKILL.md`, agentskills.io; read by Claude, Codex, Gemini CLI, Cursor, VS Code Copilot), **AGENTS.md**
(project instructions, Linux Foundation's Agentic AI Foundation since December 2025) and **MCP prompts**. Picked:
**function skills (what the agent writes and `/approve`s, `Skills/agent/functions/`) move to `SKILL.md`**: a folder
per skill, frontmatter `name` and `description`, body loaded when used. Roles, modes and guides stay as they are, so
their cached prompts don't change. Not part of F48: its own cleanup with an ADR (next free: 0042), best placed with
Phase 7's "skills as tool results", which is where loading on demand lands.

### D1 · How the recipe travels as data (decided: A, Louis 2026-10-26)

| Option | What it changes | Cost |
|---|---|---|
| **A · A `WriteRecipe` tool** (recommended) | The model sends the recipe it read (F35's record, by its link), the people, and its cues and whys; code scales, lists, prices and prints the sections. | One more model request per answer (cached prefix), under a tenth of a cent. |
| B · A data block in the answer | The model writes the recipe as a JSON block; code reads it afterwards. | Free, but the model still types the tables, so the maths isn't fixed. |
| C · Keep markdown | Graders keep parsing tables. | Nothing to build; the regex graders stay fragile. |

*Why A:* it's the only one where the numbers are right by construction, and it's skill 5 done properly.

### D2 · Whose parser (decided: A, our own C#, Louis 2026-10-26)

Read with *no Cooklang*: our own C# reads F35's ingredient lines (step 1), with no outside parser and no Cooklang.

| Option | What it changes | Cost |
|---|---|---|
| **A · Our own C# parser of the subset** (recommended) | Ingredients, amounts, units, prep notes, pots, timers, sections, notes, a small frontmatter. Tests from the spec and cook-mcp's sample recipes. | 2 steps; we own it. |
| B · CookLangNet | A NuGet package. | Stale (2021, .NET 5, old spec); its licence to check; we'd likely fork it. |
| C · cook-mcp through F36 | The real Rust parser, as an outside MCP pack. | Waits for F36 (Phase 8); a folder per chat; Node in the image; import goes to cook.md. |

*Why A:* small, C# (your rule), and ready when F35 is. cook-mcp stays useful as a **test oracle**: run the same
`.cook` files through both once and compare (differential testing), without shipping it.

### D3 · Where the estimates come from

| Option | What it changes | Cost |
|---|---|---|
| A · The model's estimate, as today | Nothing. | Free; numbers drift run to run. |
| **B · A dated estimates table we keep** (recommended) | `estimates.json` per pack size (`beef mince 500 g: R65`), `as_of`, `null` when unknown; summed in code. | Someone updates it monthly; about 100 rows for the common list. |
| C · Stats SA's published average prices | Official monthly average prices for common foods, if they still publish them (to check). | A data source to parse; the rows don't match every pack. |

*Why B:* it stays inside "no shop's prices are read", gives the same number every run, and C can feed it later.

**D3 decided (Louis, 2026-10-26: "we can use this for price estimates"): Stats SA first, our own rows for the gaps.** Louis: "if C then B". Stats SA's
*CPI Average Prices* spreadsheets (P0141, monthly, from January 2017) hold **real average rand prices per product and
pack size**: 391 rows nationally (about 230 of them food), and per province (Western Cape: 266 rows). August 2026
examples: beef mince R124.92/kg, chicken portions fresh R87.77/kg, maize meal 2.5 kg R38.53, eggs 18 R60.29,
onions R29.49/kg, sunflower oil 2 l R84.70, curry powder 50 g R17.91, stock cubes 120 g R18.40. Terms: "Users may apply
the information as they wish", provided Stats SA is acknowledged as the source and our processing is named as ours.

- **Covered:** rice, flours, maize meal, samp, bread, pasta, every common meat cut (mince, stewing beef, chicken
  portions, boerewors, lamb, pork), fish, milk, cheese, yoghurt, eggs, oil, butter, margarine, the common vegetables
  (onions, tomatoes, potatoes, carrots, cabbage, spinach, peppers, pumpkin, garlic, mushrooms), sugar, jam, chutney,
  salt, vinegar, curry powder, mixed spices, stock, yeast, baking powder, baked beans, dried beans.
- **Missing (our own rows, B):** tinned tomatoes, tomato paste, coconut milk, lentils, chickpeas, fresh ginger,
  chillies, fresh coriander, lemons, cream, raisins, butternut by name, green beans, peas, spring onions.
- **Mind:** averages of urban shops, not one shop's price; a month old when published; some rows are empty
  (`None`); two rows for one product (`Per Kilogram` and `1 Kilogram`) can differ (mince R124.92 vs R155.37).
- The files: `data/statssa-average-prices/` in the Claude project.

### D4 · Teach in the cook, or a separate teacher (decided: A, Louis 2026-10-26)

Cues and whys in every cook answer; no separate teacher role is planned.

| Option | What it changes | Cost |
|---|---|---|
| A · Cues and whys in every cook answer | Every recipe teaches a little. | A few hundred output tokens; no new role. |
| B · A separate teacher specialist now | `puls-kookskool` runs lessons and the ladder. | Without memory it can't know your rung; weak until skills 8/9. |
| **C · A now, B later** (recommended) | The cook teaches a little in every recipe now; the teacher role comes with memory. | A now; B after durable sessions. |

*Why C:* a little teaching in every answer already beats every SA supermarket's chef bot, and the teacher is only good
once it can remember you.

## The pantry (Louis, 2026-10-26)

Moved up from *Later*: the cook knows what the user already has, so it picks a dish built from it, leaves it off the
shopping list and out of the total, and stops asking "do you have oil and salt?". It never changes the recipe itself.

- **Decided: a global catalogue, stock per person.** One shared catalogue of pantry items for everyone, by category
  (fridge, freezer, cupboard, spices) with aliases (*mielie meal* = *maize meal*), started from Stats SA's product list
  so its prices plug straight in. The stock is per person and outlives a chat.
- **Decided: it fills two ways.** The user uploads it, or the cook learns it as they talk ("I bought milk", "I've got
  rice and onions"): the cook calls `PantryUpdate` and says what it changed. Nothing is subtracted behind the user's
  back.
- **Tools, from cook-mcp's pantry skill:** `PantryList` before every change (so *Milk* and *milk* stay one item),
  `PantryUpdate` (add, update, remove), and "what can I cook" ranking recipes by how much is in stock.
- **Store:** Mongo, already in our stack (the benchmark and review stores). Personal data, so POPIA: say what's kept,
  let the user clear it.
- **Decided: a browser id now, the account later.** A random id in a cookie keys the stock across chats on one
  device; it moves to the account when logins come. Needs a short notice and a "clear my pantry".
- **Decided: paste a list now, a photo later.** "2 kg rice, 6 eggs, mielie meal" is matched to the catalogue; the
  cook says what it added and what it couldn't match. A till slip photo (bought dates, real prices) comes with vision.
- **Decided: the Mongo code lives in the recipes pack** (Louis, 18:17): the pack owns `IPantryStore`, a Mongo store
  and an in-memory one for tests; a setting turns it on. The pack stays convertible to MCP.

## The steps (F48, proposed)

Each step is one commit with its test, about ten minutes to review. Number taken from the TODO's next free one.

| # | Step | Test first |
|---|---|---|
| 0 | Plan and feature doc on `feature/F48-recipe-maths` | Docs only |
| 1 | Ingredient line reader: amount, unit enum (SA cup 250 ml), name, prep note, from F35's `recipeIngredient` lines | Lines from recorded SA pages, `½ koppie`, `2-3 uie` |
| 2 | Scaler on the `Recipe` record: factor, "to taste" stays, round counts, "doesn't scale" notes | Bobotie for 4 → 6, eggs rounded |
| 3 | Shopping list: merge, units, pantry staples, SA aisle order | Two mentions of onions merge |
| 4 | Estimates table in the where-to-buy pack: Stats SA's average prices (the user's province, else national; credited, `as_of` its month) plus our own rows for the gaps; totals in code, `null` shown as "?" | A missing row never makes R0; a Western Cape row wins over national |
| 5 | `WriteRecipe` tool, prints the *Recipe* and *Shopping list* sections; golden files | Tool result for a known recipe |
| 6 | `puls-cook.md`: use `WriteRecipe`, cues and whys, the level line, the excuse table, all in our own words; golden files | Role golden file |
| 7 | Graders: scales right, list matches, total adds up; the teaching rubric and should-not cases | Each grader on recorded transcripts |
| 8 | Paid before/after run (your go), the write-up in `docs/learning/` | Numbers in the feature doc |

**Order:** after F18 closes, F35's steps 1 to 4 first (the `Recipe` record and recorded pages), then F48: every step
works on F35's record, so F48 follows those F35 steps. The where-to-buy pack (step 4) is the one the twelve
specialists want first anyway.

## What it costs

- **Running it** (Haiku 5.5, `config/prices.json` as of 2026-10-10: $0.10 per million input, $0.50 output, $0.01
  cache read): one extra `WriteRecipe` round is about 15K cached prefix ($0.00015), 1.5K new input ($0.00015) and
  600 output ($0.0003): **about $0.0006 per answer**, against roughly 1 cent a trial today. Cues and whys add about 200
  output tokens ($0.0001). So about **+5% a trial**.
- **Building it:** 9 steps (0 to 8), about three days at your three-a-day pace.
- **The eval run:** a `--repeat 3` on 20 scenarios is about $0.60 (6b's figure), twice for before and after: about
  **$1.20**, on your go.
- **Upkeep:** the estimates table, monthly.

## Later, in order

1. **The pantry** moved up (its own section above); a till slip photo to fill it comes with vision.
2. **A week on a budget** (meal-planning's *Choosing well*): one shopping list for five dinners, leftovers on purpose.
3. **The teacher specialist** with memory: the ladder, your rung, the lessons you've had.
4. **Photo feedback**: "send a photo of your pap" once vision lands after F35 (skill 19).
5. **Nutrition** only with SA data (SAMRC food tables, licence to check), never from memory.
6. **cook-mcp as F36's first outside MCP server**, as a learning exercise, if F36 wants a real server to test against.

## Where this sits in the skill map

| GOAL skill | What this adds |
|---|---|
| 3 · Prompt design | Excuse and rebuttal tables; a level line like the mood line |
| 4 · Tool design | A tool for what the model is bad at (arithmetic); fails with its reason |
| 5 · Structured outputs | The recipe as checked data (F35's record), not prose |
| 12 · Evaluation | Fixed-rule graders on data; a teaching rubric; should-not cases |
| 16 · Human in the loop | You calibrate the "teaches well" judge with labels, as in 6a |
| 8, 9 · Memory, sessions | Named as the gate for the teacher role and the pantry |

## Sources

- [cooklang-skills](https://github.com/cooklang/cooklang-skills) (MIT): all 14 `SKILL.md` files, read 2026-10-26
- [cook-mcp](https://github.com/cook-md/cook-mcp) (MIT): README, `src/tools/cloud.rs` (`import_recipe` calls cook.md)
- [Cooklang spec](https://cooklang.org/docs/spec/)
- [CookLangNet on NuGet](https://www.nuget.org/packages/CookLangNet/0.1.1)
- [From recipes to processes](https://abcdsofcooking.com/pgs/from_recipes_to_processes__helping_learners_think_like_cooks.html)
- [Salt, Fat, Acid, Heat (IFT)](https://www.ift.org/food-technology-magazine/mastering-the-elements-of-good-cooking)
- [Ruhlman's *Ratio* (NPR)](https://www.npr.org/2009/07/26/107019060/throw-away-the-recipe-learn-the-ratio-instead)
- [Expertise reversal effect](https://en.wikipedia.org/wiki/Expertise_reversal_effect)
- [Cup (unit)](https://Www.Wikipedia.org/wiki/Cup_(unit))
- [How does the Wonderbag work (IOL)](https://iol.co.za/lifestyle/food-drink/2025-08-07-how-does-the-wonderbag-work)
- In the repo: `puls-cook.md`, the F18 cook graders (`ServesAsAsked`, `Servings`), `docs/plans/F18-evals.md` 6b,
  `plans/F35-recipe-sources.md` in the Claude project, `plans/puls-ten-specialists-brainstorm.md` in the Claude project

---
[Docs index](../README.md) · Related: [Plans](README.md) · [Puls cook, next](../specs/PULS_COOK.md) · [TODO](../../TODO.md)
