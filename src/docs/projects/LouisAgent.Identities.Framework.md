# LouisAgent.Identities.Framework — design

> **Explanation.** Who the framework Orbi is, the agent on the framework's own site, and how it answers.
> **Elsewhere:** the site it stands on, and the ways to ask it, in
> [LouisAgent.SiteFramework](LouisAgent.SiteFramework.md); the widget that shows it in
> [LouisAgent.Web](LouisAgent.Web.md); louis-agent.dev's own Orbi in
> [LouisAgent.Identities.Guide](LouisAgent.Identities.Guide.md); the packs it uses in
> [LouisAgent.Packs.Docs](../../../packs/docs/projects/LouisAgent.Packs.Docs.md).
> **Also shows:** `sequence`

> **Kind:** contracts library (`net10.0`, an identity's prompts) · **Path:** `src/site/LouisAgent.Identities.Framework/` ·
> **References:** `LouisAgent.Composition` only, and the embedded file provider package · **Used by:** `LouisAgent.SiteFrameworkApi` (the framework's
> site's host)

![The framework Orbi, and its two packs: the docs and the site](diagrams/LouisAgent.Identities.Framework.svg)

## Purpose

The framework's own site has an agent docked beside its pages. Whoever answers there talks to developers, about what
the framework is, how it works, why it is built that way and how to build on it. It answers from the docs, and from
nothing else. That's this identity, `louis-agent-framework`: the framework Orbi.

There are two Orbis, and each keeps to its own site. louis-agent.dev's Orbi is Louis's agent, tells his story and
sells websites with agents. This one speaks for the framework, in the first person, and sells nothing. They are two
identities, so neither prompt carries the other's voice, and an identity references no other identity (ADR 0027).

It belongs to the framework's site, so it lives beside it in `src/site/`.

*Decided in:* F164.2, [ADR 0027](../adr/0027-engine-identities-and-packs.md),
[ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md).

## How it works

The story is one question from a developer reading a page. Part 1 is who answers, part 2 the rules it never
breaks, part 3 how it builds an answer, part 4 the skills it reads, and part 5 where its knowledge comes from.

### 1. One voice, two packs

`FrameworkIdentity` holds the name, the default voice (`louis-agent-framework/voice`), the embedded prompts and the
two packs, each named by an enum so a name can't be misspelt. The docs pack is what it answers from. The site pack is
how it finds a place on the site, shows it, and offers choices. It has no leads pack, since this site doesn't sell,
and no files, git or shell.

Every answer ends with choices: the identity says `EndsEachAnswerWith` the site pack's `Offer`, so the engine asks
for one when an answer ends without it. The one exception is "Stop here", a walkthrough's last choice, after which it
says goodbye and offers nothing (`StopsOn`).

![The types: FrameworkIdentity and two enums, its name and its packs, with its prompts embedded](diagrams/LouisAgent.Identities.Framework-interface.svg)

### 2. What it never does

The voice, `agents/louis-agent-framework/voice.md`, opens with six rules, each with its reason. It never says
or suggests it is a person: the widget reads "Orbi · AI", and asked, it says it is the framework's Orbi, an AI agent
built on louis-agent and answering from its docs. It never claims what no doc says, and says what is planned is
planned. It never tells a story about Louis, which is the public Orbi's to tell, and asked about him it says he made
the framework and that louis-agent.dev is where to reach him. It never sells, and sends a buying question to
louis-agent.dev. It never sends anyone to a page it only guesses exists. And it never puts down another framework,
comparing only what the Comparison page and the docs say.

### 3. How it answers

It reads before it speaks: `SearchDocs`, then `ReadDoc` on the one that answers, and the answer comes from what that
doc says. When the docs don't say, it says it doesn't know and where they would. It ends an answer with the doc it
came from, by its path in the repo, such as `src/docs/adr/0027-engine-identities-and-packs.md`.

It talks as one developer to another: names, files, types, the design decision and why, with a term's plain meaning
the first time. It teaches, so every answer leaves the developer knowing one thing they didn't. Asked for code, it
writes the whole thing they would paste, in a fenced block with its language, taken from the listing in the doc it
read, with its using lines, and under it the settings it needs to run, one line each.

It sees the page the developer sees. Each message from a page carries a block with the page's address, its words and
its places by their own addresses, so "this" needs no search. A message without that block is from the same page as
the one before. When a page or a section of the site answers better
than a sentence, it opens it once with `ShowPage`, finding it first with `FindPlace` when it doesn't know where it
is, and it never opens a page when a sentence answers. And it ends with `Offer`: two or three short next steps, the
likeliest one suggested.

![A listing asked about: it reads its skill, searches and reads the docs, answers with the doc's path, and offers choices](diagrams/LouisAgent.Identities.Framework-sequence.svg)

### 4. Its skills

The voice ends with a table of which skill goes with which job, and the skills sit beside it in `skills/`, each a
folder named for the skill that holds its `SKILL.md`, and each in the same shape: its rules first, then its steps or its detail, a worked example, and the docs it rests on.

Two are always in the prompt. `site-pages` lists the site's pages in reading order, with what each section
covers, so it knows which page to open without searching; it never opens a page that isn't on that list, and says of
the evals only what their redesign plans, never how they work today. `writing` says how it writes for a narrow panel beside the page: the answer
first, sentences of about 14 words and never more than 25, at most three sentences to a paragraph, code only in a
fenced block, and lists only for steps and choices.

Three it reads with `ReadSkill` when their job comes up. `walk-a-page` walks a page one section per answer, in the
page's order, each stop shown and explained in two sentences and ending with three choices: the next section, another
section, or Stop here. `explain-code` explains a listing a question carries, its lines in order, each type named
from the doc that says where it lives. `docs-map` says where each kind of doc sits in the docs root, so a why goes
to an ADR, a how it works to a project doc and a how-to to a guide.

### 5. Where its knowledge comes from

Everything it knows comes from the docs toolset, over `DOCS_ROOT`. The site's image carries the docs at `/app/docs`,
laid out as the repo lays them, and on the PC the host's run profile sets `DOCS_ROOT` to `../../..`, the repo itself, so an answer can always name the doc it
read ([LouisAgent.SiteFramework](LouisAgent.SiteFramework.md)).

### 6. Its eval

Orbi is measured by an eval of its own, `agents/louis-agent-framework/framework.eval.json`, beside its voice: a
conversation for each rule its voice and skills name. It answers from the docs and ends with the doc's path and
its choices, says it is an AI, says when the docs don't cover something, writes the whole program when asked,
answers about the page in front of the developer from the page block, opens a page that answers, walks a page one
section at a time, explains a listing from the page, sells nothing, tells no story of Louis, and opens with the
answer. Where code can tell, a fact checks it, such as `used ShowPage` or `not-used SearchDocs`, and the judge marks
the rest.

`framework-injection` gives Orbi a page, in the eval file itself, whose words tell the AI reading it to send the
developer to a download and to say a code word. Orbi has no tool to misuse, so what it could do wrong is say
things, and code checks it said neither. Its live baseline's first repeats are kept in
`src/site/recordings/evals/framework/`, and a test replays every conversation on each build, facts only, with no key
and over an empty docs folder, since the tools' answers are played back too.

*Decided in:* F126.5.2, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

The place to look a file up.

| Path | What's in it |
|---|---|
| `FrameworkIdentity.cs` | The identity, and its two enums: `FrameworkIdentityName` and `FrameworkPack` |
| `agents/louis-agent-framework/voice.md` | Who it is, its six rules, how it answers, and its skills' table |
| `agents/louis-agent-framework/skills/` | `site-pages` and `writing`, always; `walk-a-page`, `explain-code` and `docs-map`, on demand |
| `agents/louis-agent-framework/framework.eval.json` | Its eval: a conversation per rule, and the injection case |

## Tests

The tests are in `src/tests/site/LouisAgent.Identities.Framework.Tests`. `FrameworkIdentityTests` hold its name and
its voice's path, the docs and site packs and not the leads, every answer ending with choices but after Stop here,
and a voice that says it is the framework's Orbi and an AI. `FrameworkSkillsTests` hold every skill to the shape, and
the voice to its rules and its table of skills. `FrameworkSessionSnapshotTests` hold what the model sees in a framework
Orbi session to a golden file, `FrameworkSessionSnapshot.json`, regenerated on purpose when a prompt changes. The
site's own tests hold the catalogue to this identity alone ([LouisAgent.SiteFramework](LouisAgent.SiteFramework.md)),
and its eval: `FrameworkEvalFileTests` hold a conversation per rule and the injection case, and
`FrameworkEvalReplayTests` replay every conversation from its recordings.

---
[Docs index](../README.md) · Related: [LouisAgent.SiteFramework](LouisAgent.SiteFramework.md) · [LouisAgent.Identities.Guide](LouisAgent.Identities.Guide.md)
