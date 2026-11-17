# F154 The answer's motion

A plan: how the guide's answer moves. It grows out of the bar when it comes, its words arrive at a steady pace, and
it fades before it is taken away. Five steps.

## Context

Louis picked A of three for how the guide's answer should move: the answer grows out of where it comes from, leaves
with a short fade, and its words show at a steady pace. No new package.

The code was read for what is there. The answer is the bar's caption, `AgentCaption`, a card the bar draws while an
answer stands (`AgentBar`, `Caption`). It comes in with a CSS entrance today, `rise` over .45 s, and then its module,
`AgentCaption.razor.js` (243 lines), stands it beside the place the guide lit, or keeps it over the bar on a site whose
answers rise from underneath, as louis-agent.dev's do. It has no exit: when the visitor puts the answer away, or the
next answer comes, the bar stops drawing it and it is gone in one frame. Its words are the runner's words as they
stream, so they arrive in whatever chunks the model sends, sometimes a word, sometimes a sentence at once.

The sources looked at for the timings: Material's container transform (a card grows out of the thing that opened
it), Tobias Ahlin and Josh Comeau on motion that feels physical, and UI/UX Atlas on exits being shorter than
entrances, since a thing leaving needs less attention than one arriving.

What could not be confirmed by reading: that a pace of letters on a timer stays smooth in the WebAssembly app while
the stream is busy. Step 3 measures it in the browser, and stops for Louis if it is not.

## Design

### 1. The words come at a steady pace

`WordPace` is a small record that answers one question: given how many letters have arrived, how long ago the first
did, and whether the answer is done, how many should show now. Its rule is a steady pace of 90 letters a second, about
fifteen words. When it falls more than 240 letters behind what has arrived, it speeds up so the gap closes within 1.5 s,
so a long answer never trails far behind the model. When the answer is done, whatever is left shows within 1.2 s. It is
pure, so it is tested with times given to it, with no clock and no browser.

The caption asks it once a frame while it has words still to show, and draws that many. Once all have shown it stops
asking.

![One pure class paces the words, one small module moves the card in and out, and the bar holds a leaving answer](diagrams/F154-the-answers-motion-interface-design.svg)

### 2. It grows out of where it comes from

When a caption first draws, a new small module beside it, `AgentCaptionMotion.js`, grows it out of its origin: the
bar's orb on a site whose answers rise from underneath, or the card it is about to cover when it stands in a card's
place. It is a container transform: the card starts at the origin's size and place and grows to its own over 380 ms on
the web's spring. The module is its own because the standing module is already 243 lines, and growing is a different
job from standing.

### 3. It fades before it goes

When the answer is put away, or the next comes, the bar keeps the old caption drawn a moment longer, marked as leaving.
The caption asks the module to fade it: 200 ms, down to nothing and a few pixels lower. The module answers when the
fade has finished, and the caption tells the bar, which then takes it from the page. While a caption leaves, the next
may already be growing in, so for the length of a fade the bar can draw two.

![All of it runs in the visitor's browser, and only how the words are shown changes](diagrams/F154-the-answers-motion-architecture.svg)

![The card grows out of the bar, its words come at a steady pace, and it fades before it is taken away](diagrams/F154-the-answers-motion-communication-one-answer.svg)

![For the length of a fade the bar draws two captions](diagrams/F154-the-answers-motion-objects-a-leaving-answer.svg)

### 4. Less motion, and a phone

A visitor who asked for less motion gets none of it: the words show as they arrive, the card stands where it belongs,
and it goes at once. A phone gets the same motion as a wide screen, from the bar.

### 5. The namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `WordPace` | `src/web/louis-agent.web/Components/Widget/Bar/Caption/` | `LouisAgent.Web.Components.Widget.Bar.Caption` | 2 |
| `WordPaceTests` | `src/tests/web/louis-agent.web.tests/Components/Widget/Bar/Caption/` | `LouisAgent.Web.Tests.Components.Widget.Bar.Caption` | 2 |
| `AgentCaption` (changed) | `src/web/louis-agent.web/Components/Widget/Bar/Caption/` | the same | 3, 4 |
| `AgentCaptionMotion.js` | the same | a module | 3, 4 |
| `FrameworkScripts.CaptionMotion` (changed) | `src/web/louis-agent.web/Scripts/` | `LouisAgent.Web.Scripts` | 3 |
| `AgentBar` (changed) | `src/web/louis-agent.web/Components/Widget/Bar/` | `LouisAgent.Web.Components.Widget.Bar` | 4 |
| the caption's and bar's tests (changed) | `src/tests/web/louis-agent.web.tests/Components/Widget/Bar/` | as they are | 3, 4 |

### 6. What is reused

The runner's words as they stream, the caption and its standing module, `JsModule` for the new module, the web's own
ease and spring, and the browser's own animations (the Web Animations API), which say when they have finished. No new
package.

## Steps

Each step is one commit with its test, written first and seen failing. Stop after each for Louis's review.

### Step 1: the plan is kept

A docs-only step. This plan and its pictures, with its rows in the plans' index, the TODO and the work log.

### Step 2: the pace of the words

The tests come first: `WordPaceTests` holds the steady pace, the catch-up when far behind, the finish when the answer is
done, and that it never shows more than has arrived. Then `WordPace`.

### Step 3: the caption paces its words and grows in

The tests come first: a caption whose answer is still streaming shows fewer letters than have arrived at first and all
of them in the end; a caption asks the motion module to grow it from the bar when it first draws, and not again. Then
the caption and the module. Measured in the browser on :5080 while an answer streams.

### Step 4: it fades before it goes

The tests come first: an answer put away stays drawn as leaving until the caption reports the fade is done, and then
it is gone; a new answer may grow in while the old one leaves. Then the bar, the caption and the module's `leave`.

### Step 5: the docs, and the pull request

The web's design doc says how the answer moves, the write-up is written, this plan is folded and deleted, and the
records move to done. In the browser at desktop and at 390 px: an answer grows in, its words come steadily, it fades on
close, and nothing moves for a visitor who asked for less motion. `main` merged in, the full unit suite, and the pull
request on Louis's word.

## Verification

The tests named in each step, then the full unit suite. By hand on :5080, as step 5 says. A question to the guide to
watch an answer stream is a small paid call, and its cost is reported.

## Concepts

Step 2 is the one to study. Streaming shows the model's words as they come, but the model sends them in bursts. Pacing
them is a buffer between how fast something arrives and how fast a person reads, a small rule with a catch-up so the
buffer never grows without end. That is the agent loop as a person sees it, skill 6, and testing a rule apart from its
drawing, skill 18, in [the goal](../GOAL.md).

## Approved during the build

---
[Docs index](../README.md) · Related: [Plans](README.md), [louis-agent.web](../projects/louis-agent.web.md)
