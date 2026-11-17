# F160 The guide walks the demo

A plan: how a business's demo stops playing on its own, and how the guide in the site's bar explains the screen,
starts and stops the recorded talk a turn at a time, and at the end brings the visitor into a live chat with the
business's own agent. It has seven steps.

## Context

Louis looked at `/guesthouse-demo` on 2026-11-17 and asked for this, in his words: "do not make it autoplay, give
the ability to our agent to guide him through the experience", "the agent will explain the screen, be able to stop
and start the recording and guide the user through the chat experience", and "the AI must be the star of the show".
FX194 cleared the stage round the agent first. Then he added: "when it is done it can ask if you want to try and
then it guides the user into the chat with the other agent".

He picked how the guide drives the recording from three options: **a turn is a stop the guide shows**. Each
recorded turn gets an address of its own, and the guide's `ShowPage` to that address plays that one turn and
stops. The other two were two new tools (`PlayDemo` and `StopDemo`), or the guide only talking while the visitor
presses play.

What was looked up, in the code as it is on the branch:

- `RecordedChat` starts playing as soon as it is on the page (`OnAfterRenderAsync`), plays from any turn
  (`PlayFromAsync`), and waits while `Hold` is on. `BusinessDemo` turns `Hold` on while the guide talks.
- A place's address can already carry a number after its section. `GuidePlaces.Target` reads
  `/guesthouse-demo/the-demo/2` as the page and the place `the-demo/2`, and the bar's `showArea` lights the
  section and turns a paged band to that page. A section that isn't paged is just lit.
- The page tells the guide its places from the page itself (`pageText` in `ChatWidget.razor.js`): every section
  with an id, and a paged band's later pages as `id/2`, `id/3`. A turn not yet played has nothing on the page, so
  the turns have to be listed by the chat for the guide to see them.
- `AgentWindows` is already how the guide's bar reaches the demo: it hands the guide's `AskAgent` to the window
  that shows that business (Observer). The bar's `OnToolCalled` gives it each tool call first.
- The guide's walk of a page is in its voice (one stop an answer, `ShowPage`, two short sentences, `Offer`), and
  a demo page's own rules are in its `live-demo` skill, which already ends with Ask it live and `AskAgent`.

What could not be confirmed here: how well the model keeps to a turn-by-turn walk. That is checked on the live site
after the deploy, since a local run never sends the guide a question.

## Design

### 1. The demo waits to be started

`RecordedChat` gets a `Start` parameter, a choice of two (ADR 0024): `OnItsOwn`, today's behaviour and the default,
and `WhenAsked`. Started when asked, the window shows its first question and a Play button, the theme's main
button, with nothing playing. `BusinessDemo` asks for `WhenAsked`. A press of Play plays the whole talk at its pace,
as today.

### 2. One turn plays, then stops

`RecordedChat.PlayTurnAsync(turn)` shows the turns before it at once, as `PlayFromAsync` already does to catch up,
plays that turn at its pace, and stops after it, with Play on and Play it again under it. It keeps the hold: while
the guide talks, the turn waits, so the visitor reads the guide's line first and then watches the turn.

### 3. The turns are places the guide can see

The recorded chat draws a short list of its turns that the page doesn't show, each with the address the guide
uses: `the-demo/1` to `the-demo/4`, each named by its question. `pageText` lists a section's turns as it lists a
paged band's later pages, so the guide's page block names every turn by its question.

### 4. The guide's bar hands a turn to the demo

When the guide shows a place that is a section and a turn on the visitor's page, the bar hands it to
`AgentWindows.Turn`, which tells whoever listens (`Turned`). `BusinessDemo` listens, and when the section is its
own player it plays that turn. Any other page ignores it. This is the same Observer the guide's ask already goes
through, so the bar and the demo still know nothing of each other.

### 5. The guide walks it, and ends at the live chat

The guide's prompts say how a demo page is walked: the website first, in a line, then each turn, one an answer.
At a turn's stop it shows that turn with `ShowPage`, says in two short sentences what to watch for, and offers the
next turn (suggested), Ask it live, or Stop here. What each turn shows is in the demos doc the guide reads, one
line a turn. After the last turn it asks whether they want to try the business themselves. On a yes it offers two
or three questions to ask, as its live-demo skill does today, and calls `AskAgent` with the one they pick, so the
live chat takes the recording's place in the business's own window.

![The recorded chat waits to be started and plays one turn on its own; the windows hand it the turn the guide showed](diagrams/F160-the-guide-walks-the-demo-interface-design.svg)

![Nothing new runs: the demo page waits, the guide names a turn, and the page plays it](diagrams/F160-the-guide-walks-the-demo-architecture.svg)

![Each answer is one stop: the guide shows a turn, the turn plays once it has finished talking, and at the end it offers the live chat](diagrams/F160-the-guide-walks-the-demo-communication-the-guide-walks-a-demo.svg)

![One shared AgentWindows joins the guide's bar to the demo, which holds its recorded chat](diagrams/F160-the-guide-walks-the-demo-objects-in-memory.svg)

### 6. What is reused

| Piece | Reused from |
|---|---|
| Playing from a turn, catching up the turns before it | `RecordedChat.PlayFromAsync` |
| Waiting while the guide talks | `RecordedChat.Hold`, `GuideActivity` |
| An address with a number after its section | `SiteAddresses`, `GuidePlaces.Target` |
| Listing a section's numbered places for the guide | `pagesOf` in `ChatWidget.razor.js` |
| The bar reaching the demo | `AgentWindows` (Observer) |
| The live chat at the end | `AskAgent`, `MockLiveChat`, the live-demo skill |

No new package, no new tool, and no new ADR.

### 7. The types this plan adds or changes

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `RecordedStart` (new enum) | `src/web/louis-agent.web/Components/Recorded/` | `LouisAgent.Web.Components.Recorded` | 2 |
| `RecordedChat` | the same | the same | 2, 3, 4 |
| `RecordedChatTests` | `src/tests/web/louis-agent.web.tests/Components/Recorded/` | `LouisAgent.Web.Tests.Components.Recorded` | 2, 3, 4 |
| `ChatWidget.razor.js` (`pagesOf`) | `src/web/louis-agent.web/Components/Widget/` | | 4 |
| `AgentWindows` | `src/web/louis-agent.web/Components/Widget/Door/` | `LouisAgent.Web.Components.Widget.Door` | 5 |
| `ChatWidget` | `src/web/louis-agent.web/Components/Widget/` | `LouisAgent.Web.Components.Widget` | 5 |
| `AgentWindowsTests`, `ChatWidget`'s tests | `src/tests/web/louis-agent.web.tests/Components/Widget/` | mirrors the source | 5 |
| `BusinessDemo` | `src/site/louis-agent.site/Components/Showroom/` | `LouisAgent.Site.Components.Showroom` | 2, 5 |
| `BusinessDemoTests` | `src/tests/site/louis-agent.site.tests/Showroom/` | `LouisAgent.Site.Tests.Showroom` | 2, 5 |
| `voice.md`, `skills/live-demo.md` | `src/site/louis-agent.identity.guide/prompts/agent/louis-agent-guide/` | | 6 |
| `demos.md` | `demos/louis-agent/docs/` | | 6 |
| `GuideSkillsTests` | `src/tests/site/louis-agent.site.tests/Guide/` | `LouisAgent.Site.Tests.Guide` | 6 |

## Steps

Each step is one commit with its test, seen failing first. Stop after each for Louis's review.

### Step 1: the plan is kept

A docs-only step. This plan and its pictures are committed, with its rows in the plans' index, the TODO and the
work log's open list.

### Step 2: the demo waits to be started

`RecordedStart`, and `RecordedChat`'s `Start`: started when asked, nothing plays and the first question shows with
a Play button. `BusinessDemo` asks for it. Test first: a chat started when asked has no turn after the first render
and a Play button, and a press of it plays to the end. Looked at on this PC at 1920 by 1080 and on a phone.

### Step 3: one turn plays, then stops

`PlayTurnAsync(turn)`. Test first: asked for turn 2, the chat shows turn 1 whole, plays turn 2, and stops with
Play on; held, it waits until let go.

### Step 4: the turns are places

The chat's list of its turns, and `pagesOf` reading it. Test first, in bUnit: the chat draws one entry a turn, each
with its number and its question. The guide's page block is checked by eye on this PC, by reading what the page
sends, without sending it.

### Step 5: the guide's bar hands a turn to the demo

`AgentWindows.Turn` and `Turned`, the bar's hand-off, and `BusinessDemo` playing the turn. Tests first: the windows
tell a listener the section and the turn; a `ShowPage` of `the-demo/2` on the demo page plays turn 2; a place with
no turn, or on another page, plays nothing.

### Step 6: the guide walks it, and ends at the live chat

The guide's voice, its live-demo skill and the demos doc: a demo page's walk, turn by turn, what each turn shows,
and the ask to try it live at the end. `GuideSkillsTests` pins the new rules, and the golden files are made again
and their diff read.

### Step 7: the docs, the records and the pull request

The web's and the site's design docs, the guide's identity doc, the write-up's section, the TODO and work log rows,
and the plan folded and deleted. `main` merged in, the full unit suite, and the pull request on Louis's word.

## Verification

The tests named in each step, then the full unit suite. Each step a visitor sees is looked at on this PC with
`/check-a-page`, never sending the guide a question there. After the deploy, one walk of `/guesthouse-demo` on the
live site, start to end and into the live chat: a few guide answers and one business answer, a few cents, its cost
reported after.

## Concepts

Step 2 to 5 are an agent driving a user interface through tools it already has: the guide's `ShowPage` is now
also the remote control of a recording, which is tool design, skill 4 in [the goal](../GOAL.md), and an agent
working a screen, skill 19. The hand-off at the end, from the guide to the business's agent, is multi-agent
orchestration, skill 10. Step 6 is prompt design and context engineering, skills 3 and 7: what the model is told a
page holds, and the walk it follows.

## Approved during the build

- **2026-11-17, the conversation is its own place.** The site's address rule reads `/the-demo/1` as `/the-demo`,
  so the demo's first turn and the demo itself were one place. Louis picked, of three, "The conversation is its own
  place": the recorded chat's window is a section of its own inside the demo, `the-conversation`, which is its first
  turn, and `the-conversation/2` to `/4` the turns after it. Showing the demo explains the screen; showing the
  conversation plays a turn, and the spotlight lands on the chat window. Each demo's conversation is in the sitemap.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [the site's design doc](../projects/louis-agent.site.md)
