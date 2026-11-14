# CL64: Our prompts in the skill shape

> **Plan.** How the prompts our own agents read (an identity's voice, its specialists and minions, a pack's guide)
> get the shape of a good skill file: a description that says when, the rules that are never broken first, short
> numbered steps, and the detail loaded only when it is needed. Two prompts are rewritten and measured, the cook and
> the guide's voice; the rest follow as they are touched.

**Status:** approved 2026-11-13 (thread "Make identity prompts follow the skill format", Louis: "CL on it's own plan
thoroughly and merge", his pick of three: the rule, then the worst prompts first).
**Branch:** `refactor/CL64-prompts-in-the-skill-shape`. **Starts after:** CL63, which writes the rules file for
prompt files that step 1 fills. Four choices are still open and go to Louis one at a time, three options each, when
their step starts (*Open choices*).

## Context

Louis, 2026-11-13, looking at the first Claude Code skill of CL63 (`.claude/skills/draw-a-diagram/SKILL.md`): "why
are our prompts not following this? I'm asking our identity prompts etc."

That skill has four things. Its header says what it does **and when to use it**. The three things that are never
done come **first**. The work is **four short numbered steps**. And the detail is **not in the file**: it points at
a guide that is read when needed.

Our own prompts have the first of those and little of the rest:

- **The header is there.** Every prompt opens with `name` and `description` since CL27
  ([ADR 0063](../adr/0063-a-prompt-says-what-it-is.md)).
- **Loading on demand exists, for one identity.** The guide keeps skills beside its voice, read with `ReadSkill`
  (F129, [ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md)). No other identity uses it, and a
  specialist can't: the engine gives skills only to the agent a person talks to.
- **The rest was planned for later.** The TODO's F103, *Function skills in the Agent Skills format*, waits for
  Phase 7. Meanwhile the prompts grew one fix at a time: each failed check added a sentence where the failure was.

CL63 does not cover this. It rewrites what **Claude Code** reads when it works on the repo. This plan is about what
**our agents** read when they answer a person. They meet in one file: CL63 adds `.claude/rules/prompts.md`, the
rules for whoever edits a prompt, and this plan's rule goes there.

### What the prompts look like today

Measured on `main` at `89d32d9b`: 48 prompt files, 29,868 words in their bodies. The numbers come from a script
over `**/prompts/**/*.md`, the tests' own prompts left out.

| Kind | Files | Biggest | Opens with its never-rules | A procedure as numbered steps |
|---|---|---|---|---|
| A voice (the agent a person talks to) | 10 | the guide's `voice.md`, 3,883 words | none | not a procedure |
| A specialist | 8 | `puls-cook.md`, 3,217 words | none | 4 of 8 |
| A minion | 3 | 527 words | none | 2 of 3 |
| An identity's skill | 7 | `site-map.md`, 938 words | not needed, it is reference | not a procedure |
| A toolset's guide | 19 | `site.md`, 1,197 words | none | 15 of 19 |
| The principles | 1 | 184 words | yes, it is only that | not a procedure |

The two that hurt most:

- **The cook** (`puls-cook.md`): 3,217 words, all of it sent every time the cook runs, with five guides after it,
  about 5,200 words in all before the user's question. About 30 lines hold a *never* or a *don't*, spread from line
  10 to the end. Its whole procedure, steps (1) to (6), is inside one bullet of 36 lines, and its longest line is 677
  characters. It reads like a list of every case that ever went wrong, because that is how it was written.
- **The guide's voice** (`louis-agent-guide/voice.md`): 3,883 words on every message, after F129 already took two
  maps out of it. About 40 lines with a *never* or a *don't*, none of them first.

One more is over the size this plan proposes: Puls's routing (`puls/voice.md`, 1,631 words).

### What the published guidance says

Each of these was read on 2026-11-13, not quoted from memory.

- **Say what it does and when to use it.** Anthropic's skill authoring guide: the description "should describe what
  the Skill does and when to use it", in the third person, in at most 1,024 characters
  ([Skill authoring best practices](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices)).
- **Only what the model doesn't know.** The same guide: "Only add context Claude doesn't already have", and keep a
  skill's body under 500 lines, splitting into files one level deep when it nears that.
- **Sections, at the right height.** Anthropic's context engineering guide recommends "organizing prompts into
  distinct sections" with headings, and names two ways to get the height wrong: "hardcoding complex, brittle logic"
  and "vague, high-level guidance"
  ([Effective context engineering for AI agents](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)).
- **Examples, not a list of cases.** The same guide on stuffing "a laundry list of edge cases into a prompt": "We
  do not recommend this." It asks for a few varied examples that show the behaviour instead. The cook is the
  laundry list.
- **Load it when it's needed.** Progressive disclosure: a name and a line up front, the body fetched by a tool,
  with what every turn needs kept in (the same guide's hybrid, and ADR 0114).
- **Rules first is ours, not Anthropic's.** No page above says to put the never-rules first. Claude Code's docs say
  only the start of a skill is sure to survive a long session, which is why CL63 puts them first. For our prompts
  the reasons are different: models use the start and the end of a long text better than its middle (Liu and
  others, [Lost in the Middle](https://arxiv.org/abs/2307.03172), 2023), and a person reviewing a prompt finds the
  hard rules in one place. Whether it changes the cook's answers is what step 3 measures.

A voice is not a skill, and the plan keeps that apart. A skill is a procedure loaded for a job. A voice is who the
agent is on every turn. So the shape fits a specialist, a minion, a guide and an on-demand skill fully, and a voice
in part: its never-rules first, its size, and what it can hand to a skill.

## Design

### The shape of a prompt

Six parts, in this order. Step 1 writes them as the rule.

1. **The header** (ADR 0063, unchanged): `name`, and a `description` that says what the prompt is and **when it is
   loaded or read**. For a skill read on demand, the when is the trigger: "Read it before you…".
2. **The opening**, two or three lines: who the agent is, or what the guide is for.
3. **`## Never`**, straight after the opening: the rules that are never broken, seven at most, one line each, the
   rule and then its reason. A rule earns a place when breaking it once is a real failure (a made-up price, a
   shop's page read, a private detail told). Each is said here and nowhere else in the file.
4. **`## Steps`**, where the prompt is a procedure: numbered, one action a step, three lines at most. A step that
   needs more points at its own section. A voice has no procedure and skips this part.
5. **The detail**, one section per step or subject, under a heading the step can name. A case that is about one
   tool belongs with the tool: in what the tool returns, or in its toolset's guide
   ([ADR 0080](../adr/0080-a-tool-when-it-improves-the-prompt.md)), not in the role's file.
6. **The answer's shape, last**, with one whole example instead of a sentence per case.

And one size rule: **a prompt's body holds at most 1,500 words** (open choice 2). A file over it is split: what
every turn needs stays, reference goes to a skill read on demand, a tool's cases go to the tool.

**What the shape looks like on the cook.** A sketch of the headings, written from today's file. Step 3 settles the
words, with every sentence of today's file accounted for.

```
# Puls specialist: Local cook
You turn "what should I cook?" into a meal the user can buy for and make today, where they live.

## Never
1. Never a recipe from memory: the recipe is one print from ReadRecipe, from one page.
2. Never a price of your own: every price is EstimatePrices', copied as it gives it.
3. Never read a shop's page, with any tool: no shop has given us its prices.
4. Never name a shop or a branch a tool didn't list.
5. Never a link or a photo the print didn't give.
6. Never sums in your head: ScaleRecipe or Calculate.
7. Never answer without a recipe while a hit, another search or WebSearch is left.

## Steps
1. First round, together: GetToday, FindRecipes, and the shop lookup when the task names a town.
2. Read the hit whose title is the dish with ReadRecipe. Three prints at most.     -> Finding the recipe
3. Scale it to the task's people with ScaleRecipe.                                  -> Scaling
4. Write the shopping list from the print's lines.                                  -> Your report
5. Call EstimatePrices once, with every row.                                        -> Prices
6. Write the report, in its five sections.                                          -> Your report

## Finding the recipe        ## Scaling        ## Prices        ## Shops
## How every Puls specialist works
## Your report to Puls
```

**The patterns.** *Progressive disclosure* for what loads on demand (Anthropic's Agent Skills, ADR 0114; in code
Fowler's *Lazy Load*). *A ratchet* for the test: the files that don't fit yet are on a list that can only shrink,
as `WriteUpsTests` and the size rules already work, so the rule is held from the first day without rewriting 48
files at once. *An A/B test* for each rewrite: the same eval on the prompt before and after, the prompt's hash
saying which is which.

### What changes, and what doesn't

- **The rule, and a test that holds it** (step 1). An ADR for the why, the how in `.claude/rules/prompts.md`, a
  section in [Build an identity](../guides/build-an-identity.md) for someone writing a new prompt, and
  `PromptShapeTests`.
- **Every description says when** (step 2). Only the header changes, and a role's or a guide's header never reaches
  the model: the loader strips it. The model sees a description in one place, the list of on-demand skills, and
  those five already say when. So this step is for the person on the portal's *Prompts* page, and for the day a
  prompt is handed to another agent in this format (ADR 0028). It changes no answer.
- **The cook, in two steps** (3 and 4). First the same words in the new order, so the measure shows what order
  alone does. Then smaller, by moving detail out (open choice 3).
- **The guide's voice, in two steps** (5 and 6), the same way (open choice 4 for what moves out).
- **Everything else stays as it is**, on the test's list, and is brought into the shape when its file is next
  touched. That includes Puls's routing, the other roles and the guides.
- **Out of scope:** what Claude Code reads (CL63), the function skills as `SKILL.md` folders (F103, still Phase 7),
  the tools' own descriptions, and the principles, which are already rules and nothing else.

### How a rewrite is measured

A prompt is behaviour. Moving a sentence can change an answer, and F129 showed it: with the guide's page map on
demand it stopped opening pages, until an index went back in. So no rewrite is kept on a test's word alone.

- **Free, at every step.** The word count before and after. The golden file of what the model reads, its diff
  reviewed. The word-level tests that pin a prompt (`LouisVoiceTests`, the cook's eval tests), changed in the same
  commit where a pinned phrase moved. And an inventory in the pull request: every sentence of the old file, and
  where it is in the new one.
- **Paid, on Louis's go each time.** The identity's eval, live, on the prompt before and on the prompt after: the
  same samples, the same model, three runs of each, since one run of a small model wobbles. The cook's eval today is
  `puls-cook` (live only: its replays miss since F35.4). The guide has no eval yet: its measure is F129's six
  questions on the local site, and F126's guide eval once that exists. The cost of the last run is read from the
  usage ledger and put to Louis before each go.
- **What is compared:** each check's pass count, the cost and the tokens of a run, the tool calls a turn takes, and
  the fixed part of the prompt in tokens.
- **Kept only if quality holds.** A rewrite whose checks fall is not merged as it is: the failing answers are read,
  the prompt fixed or the change dropped, and the write-up says which.
- **F126 may land in between.** It deletes today's evals. A step that starts after that uses the new `.eval.json`
  for its identity, and waits for it if it isn't written yet.

### Open choices (asked when their step starts, one at a time)

1. **Where the test lives, and who holds its list** (step 1). One scan of every `prompts/` folder in the repo from
   core's tests, with one list, the way `ClaudeMdSizeTests` scans. Or a shared check each owner's test project runs
   over its own prompts, each with its own list, so no framework test names a demo's file. Or the engine's own
   reader refusing a prompt at start-up, which allows no list, so every prompt must fit first.
2. **What the size rule counts** (step 1). Words, 1,500 a file: lines mislead when one line holds 677 characters.
   Or lines, 200, as `CLAUDE.md` has. Or no rule per file, and a budget in tokens for a whole assembled prompt.
3. **Where the cook's detail goes** (step 4). Into the tools and their guides, case by case, by ADR 0080. Or into
   skills a specialist reads on demand, which is an engine change: today only the agent a person talks to has
   skills. Or nowhere: the cook stays whole in its new order, if step 3's measure says the size does no harm.
4. **What leaves the guide's voice** (step 6). *What we sell today* and *Your story* to on-demand skills, with one
   line each kept in. Or the same two as always-loaded skills, which only tidies the file. Or the selling part to a
   tool that returns the offer as data, as F143 does for the CV.

### Interface design

No type of the engine changes unless open choice 3 picks skills for a specialist. The picture shows what the rule
reads and the two types step 1 adds to the tests.

![The engine's prompt types as they are, the test and the record step 1 adds, and the one engine change that depends on a choice](diagrams/CL64-prompts-in-the-skill-shape-interface-design.svg)

```csharp
// Step 1, in the tests (its home follows open choice 1). A prompt's body, read into the parts the rule names.
public sealed record PromptShape(int Words, IReadOnlyList<string> Never, IReadOnlyList<string> Steps,
    string? FirstHeading)
{
    public static PromptShape Of(PromptFile file);
}

public class PromptShapeTests
{
    [Test] public void EveryPrompt_FitsTheShape_OrIsOnTheList();   // the size, Never first, seven at most
    [Test] public void TheList_HoldsNoPromptThatFits();             // so the list can only shrink
    [Test] public void AStep_IsAtMostThreeLines();
}
```

### Architecture

![From a prompt file to the model: the files, the catalogue, the loader and the prompt, with the step that changes each part and what checks it](diagrams/CL64-prompts-in-the-skill-shape-architecture.svg)

### Communication (one cooking question, after step 4)

Drawn as it would be if open choice 3 puts a tool's cases with the tool.

![A cooking question after the rewrite: the cook's prompt is built once, the steps are followed in order, and a tool's own cases arrive with its result](diagrams/CL64-prompts-in-the-skill-shape-communication-one-cooking-question.svg)

### Objects (what the cook holds in its prompt)

![The cook's system prompt in memory: the principles, the cook's own file in its six parts, and the five guides](diagrams/CL64-prompts-in-the-skill-shape-objects-the-cooks-prompt-in-memory.svg)

### File map

No project or folder is added, so this is a map of files.

| File | Change | Step |
|---|---|---|
| `src/docs/adr/{next free}-a-prompt-is-written-in-the-skill-shape.md` | New | 1 |
| `.claude/rules/prompts.md` (CL63's file) | The shape's how | 1 |
| `src/docs/guides/build-an-identity.md` | A section, *The shape of a prompt* | 1 |
| `PromptShapeTests`, `PromptShape` | New | 1 |
| The 48 prompts' headers | The description says when, where it doesn't | 2 |
| `demos/puls/identity/louis-agent.identity.puls/prompts/specialists/puls-cook.md` | Reordered, then smaller | 3, 4 |
| The recipes, shop-prices and where-to-buy packs' guides or tool texts | What the cook hands them, if choice 3 says so | 4 |
| `src/site/louis-agent.identity.guide/prompts/agent/louis-agent-guide/voice.md` and its `skills/` | Reordered, then smaller | 5, 6 |
| The golden files, `LouisVoiceTests`, the cook's eval tests | Follow each prompt | 2 to 6 |
| `src/docs/learning/03-prompt-design.md` | New: the first write-up of skill 3 | 7 |

### Namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `PromptShapeTests` | `src/tests/engine/louis-agent.core.tests/Skills/Headers` | `LouisAgent.Core.Tests.Skills.Headers` | 1 |
| `PromptShape` | the same | the same | 1 |
| `SkillLoader.SkillsOf`, `IdentitySkills` (changed) | `src/engine/louis-agent.core/Hosting/Prompts`, `…/Skills/Headers/Identities` | `LouisAgent.Core.Hosting.Prompts`, `LouisAgent.Core.Skills.Headers.Identities` | 4, only if choice 3 picks skills for a specialist |

The test's home follows open choice 1; the row above is its recommended one.

## Steps

Each step is one commit and one pull request. A step that changes code has its test first, seen failing. A step
that only changes a prompt has no new test: the golden file's diff and the measure are its check. Stop after each
for Louis's review.

1. **The rule and its test.** Open choices 1 and 2 first. Then the ADR, the how in `.claude/rules/prompts.md`, the
   section in *Build an identity*, and `PromptShapeTests` with its list. *Test:* written first and seen failing on
   today's prompts, then green with the list filled; a prompt taken off the list while it doesn't fit fails, and one
   left on the list after it fits fails too.
2. **Every description says when.** The 48 headers read one by one; the ones that only say what the file is get
   their when. *Check:* the header tests stay green, and the golden file's diff shows only the on-demand list, if
   anything.
3. **The cook, in the new order.** The same sentences: the never-rules pulled to the top and said once, the
   procedure as numbered steps, the detail under its own headings, the report last. The inventory is in the pull
   request. *Check:* the golden diff, the cook's tests, and the paid measure, before and after, on Louis's go.
4. **The cook, smaller.** Open choice 3 first. Then the detail moves where the pick says, and the body comes under
   the size rule and off the list. *Test:* for a tool's new text, the tool's own test first; for skills below the
   agent, the loader's tests first. *Check:* the paid measure again, against step 3's numbers.
5. **The guide's voice, in the new order.** As step 3, for `voice.md`: its never-rules first and said once, *How
   you answer* in short points. *Check:* `LouisVoiceTests`, the golden diff, and the six questions on the local
   site, before and after.
6. **The guide's voice, smaller.** Open choice 4 first. Then what the pick names moves out, an index line kept in
   for anything the guide must know exists (ADR 0114). *Check:* as step 5, and the fixed prefix in tokens before
   and after.
7. **The write-up, and done.** `03-prompt-design.md`, the first write-up of skill 3: the numbers of each rewrite,
   what order did and what size did, what didn't work. The guide and the design docs of the projects the steps
   touched. The work log's row, the TODO's row gone, F103's row reworded to what is left, this plan deleted.
   *Test:* none (docs).

## Verification

- **Per step:** `louis-agent.core.tests` for the shape test, and the tests of the project whose prompt changed
  (Puls's identity and eval tests, the site's tests); the golden files regenerated and their diff read.
- **Once, after step 6:** the full suite.
- **Free manual checks:** the portal's *Prompts* page shows every description; the cook asked for bobotie for six in
  Malmesbury on the local site; the guide asked F129's six questions.
- **Paid, each on Louis's go:** the cook's eval live, three runs before and three after, at step 3 and again at
  step 4. Nothing else in this plan spends money.

## Concepts

- **Prompt design** (GOAL.md skill 3): a prompt has a structure a model and a person can both find their way in;
  the right height between brittle rules and vague ones; an example instead of a list of cases. And skill 3's
  missing part, *A/B comparison of prompt changes on the evals*: steps 3 to 6 are exactly that, with the prompt's
  hash telling the two apart on the trend.
- **Context engineering** (skill 7): what is in the prompt on every turn is paid for on every turn, so reference is
  loaded when a question needs it, and what the agent must know exists stays in.
- **Evaluation** (skill 12): a change to a prompt is kept on a measure, with repeats, not on how the file reads.
- **Tool design** (skill 4): a case about one tool is told by the tool, where the model is when it needs it.

## Approved during the build

(empty)

---
[Docs index](../README.md) · [Plans](README.md) · Related: [ADR 0063](../adr/0063-a-prompt-says-what-it-is.md) ·
[ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md) ·
[ADR 0080](../adr/0080-a-tool-when-it-improves-the-prompt.md)
