# F134: The showroom is a stage the agent runs

> **Plan (a parent with six children).** How the showroom is rebuilt: what it becomes, the rules it is built by, and
> the children in the order they are built. Each child gets its own plan when it starts.

**Status:** approved 2026-11-13 (Louis: "go, commit the plan and start step 1"). **Children's branches:** `feature/F134.n-name`, one pull request each.
**Mockups:** [F134-mockups/](F134-mockups/), plain HTML, opened in a browser from the repo so the photos load:
[f-centre-stage.html](F134-mockups/f-centre-stage.html) is the start Louis picked,
[j-the-agent-runs-the-room.html](F134-mockups/j-the-agent-runs-the-room.html) the start with the agent running it,
and [k-the-agent-runs-the-demo.html](F134-mockups/k-the-agent-runs-the-demo.html) a demo's page the same way. The
agent in J and K is simulated. The three older ones (A, B, C) are the looks of 2026-11-11, kept for the record.

## Context

F134 was approved on 2026-11-11 as five steps for look B, "showcase with photos". Nothing of it was built. On
2026-11-13 Louis asked for more: "can we modernise it even more", "make it very fancy", and picked **F, centre
stage**, of three new designs. He then widened it: "redesign each demo to fit the new fancy style", "you can look
for better images", "tour this page can also be redesigned".

My first tour mockups were a slideshow with Back and Next. Louis: "tour this page is currently done by an agent, how
will that work in this design?", "the agent must do it all", "we must use the framework in the design", "our agent
button needs a proper redesign", "think of ways we can integrate the AI into the demo". He said yes to two ideas
together, with an agent bar: the agent runs the room, and you can see how it did that.

Three more of his words shape how it is built: "things that can be shared, must go to framework", "like cool
components we could reuse later in other projects", and "we should have like chatwidget layout type".

This plan replaces the look B plan in this file. Git keeps that one (`66d969379`).

## Design

**What it becomes.** The start of the showroom is a stage: one demo at a time as a window in the light, the other
four waiting behind it in depth, a dock under it. A demo's own page is the same window grown to fill the stage, with
what to watch as chapters under it. The guide sits in a bar at the foot of every showroom page. When it talks about a
place it lights that place, its words stand beside it, and its choices are drawn there. Any answer turns over to show
what it called and what it cost.

**The rules it is built by** (each child keeps them, and the last child writes the lasting ones into an ADR):

1. **What can be shared is the framework's, as a component of its own.** A lit stage, a deck of things in depth, a
   framed window, a dock, line icons, the agent's orb: each is a general component of `louis-agent.web`, named for
   what it is and not for the showroom, so a later project uses it without the showroom. The showroom is the first
   place that puts them together. The site holds only its words, its photos and which demos it has.
2. **Nothing moves unless an agent's tool call caused it.** The light is `ShowPage`. The choices are `Offer`. A
   window comes forward because the guide showed its address. There is no scripted tour and no Back or Next.
3. **The box is always there.** The guide's choices are shortcuts, never the only way to answer: the visitor can
   type anything, at any time, and every set of choices says so.
4. **The widget is one engine with a layout.** The chat widget keeps its runner, its list and its composer, and
   gains a layout the site picks: `Corner`, as today, or `Bar`. No second widget.
5. **The page keeps clear of the bar.** The frame that draws the bar reserves its height, so no page ends under it.
6. **Only true numbers.** The work side shows what the framework recorded for that answer: the tools called, the
   time, the tokens and the cost. Progress says which places the guide has shown, never a promised count.
7. **Every word a demo's agent says on screen is in its recording**, held by a test.
8. **Every place has its address** (ADR 0107): a window on the start is `/demos/{its demo}`.

**Where the reusable parts live.** Three places were weighed:

| Option | What it means | Cost |
|---|---|---|
| **General folders in `louis-agent.web`** (picked for this plan) | `Components/Icons`, `Components/Staging`, and the agent's parts in `Components/Widget`. The showroom composes them. | A few more folders in the framework's web. |
| Inside the showroom's folder | As the look B plan drew: `Showroom/Icons`, `Showroom/Deck`. | A later project has to take the showroom to get a dock. |
| A project of its own, `louis-agent.web.kit` | The parts ship apart from the framework's web. | A new project and package for a need no plan names yet (ADR 0055). |

**Louis's picks for the start** (2026-11-13): a window's body is one of two the framework offers and the site fills
(a website with its chat, panes of work). The agent's words on a window are written in the site and held to the
recording by a test. The dock is on the start and the rail stays on a demo's page, as a slim dock of icons. On a
phone the windows are swiped one at a time, with no 3D.

### Architecture

![The site names its demos and fills their windows, the framework's general parts draw the stage and the bar, and the guide moves them with its tools](diagrams/F134-the-showroom-is-a-stage-architecture.svg)

### Communication (the guide walks the start)

![A visitor asks for a walk: the guide calls ShowPage and Offer, the deck brings a window forward, and the caption stands beside it](diagrams/F134-the-showroom-is-a-stage-communication-the-guide-walks-the-start.svg)

The interface and object pictures are per child: each child's plan carries its own four.

## The children

Built in this order, one pull request each. A child's plan is written and approved when it starts.

| Child | What it builds | Done when |
|---|---|---|
| **F134.1 The start is a stage** ([plan](F134.1-the-start-is-a-stage.md)) | The general parts (line icons, the lit stage, the deck, the framed window with its two bodies, the dock) and the start built from them, each window at its address, swiped on a phone. The site fills its five windows. | `/demos` is the stage at 1920×1080 and at 390 px, `/demos/restaurant-demo` opens with the restaurant forward, and a test holds each window's words to its recording. |
| **F134.2 A demo on the stage** | A demo's page as the window grown to fill the stage, what to watch as chapters under it, the rail as a slim dock of icons, and a body for an agent at work. | The five demo pages play as today in the new look, at both widths, and the guide's stops are the same places. |
| **F134.3 The agent bar** | The chat widget's layout (`Corner`, `Bar`), the orb with its four states, the suggestions as chips, the box that is always there, and the frame keeping the page clear of the bar. | The showroom's guide is the bar, a typed question is answered mid-walk, and no page ends under the bar. |
| **F134.4 The agent runs the room** | The guide's last answer as a caption beside the place it lights, its `Offer` drawn there, a window coming forward on `ShowPage`, the guide holding a demo's play while it talks about it, and the talk as a drawer. | A live walk of the start and of one demo page, by the real guide, moves the stage only through its tools. |
| **F134.5 See how it did that** | The caption's other side: the tools called, the time, the tokens and the cost of that answer, the drawer's total, and the pieces of a page ringed so they can be asked about. | Every answer of a live walk turns over to numbers that match the usage ledger. |
| **F134.6 Better photos** | The photos Louis picks from Pexels, with their record. | The three business demos show the new photos, each listed with its source and licence. |

Parked, a feature of its own: **see it as yours**, the visitor's own business on the stage, live.

## Verification

- Each child runs the tests of what it touches, and the full suite once before its pull request.
- By eye, in Chrome, at an exact 1920×1080 and at 390 px, every page and every window of the deck.
- F134.4 and F134.5 need live walks by the real guide. Those spend test credit, and their cost is reported.

## Concepts

Skill 18 (testing and shipping) for the parts and their markup tests. From F134.3 on, skill 4 (tool design): an
agent's tool calls as the only cause of what the screen does. In F134.5, skill 13 (cost and token economics) and
skill 17 (observability): the framework's own records of an answer, shown to the person who asked.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [F134.1](F134.1-the-start-is-a-stage.md)
