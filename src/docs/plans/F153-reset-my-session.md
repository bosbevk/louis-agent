# F153 Reset my session

A plan: how the admin's home gains one button that puts this browser back to a first visit, so the owner can see
the site the way a new visitor does without opening a private window. It has four steps.

## Context

Louis asked for "Reset my session" in the admin, and picked what it resets: everything a first visit doesn't
have. Every chat this browser saved is ended on the API, every key in localStorage and sessionStorage is cleared
except the admin's own key, `louis-agent.adminKey`, and the page reloads. Of three shapes for the code he picked
a small reset class with a button component (option A), over the steps inside the button's own code and over a
script that calls the API itself.

The code was read for what is there. The browser keeps a site's chats as lists under three keys today:
`louis-agent.chats` for the chat page (`ChatStore`'s default), `louis-agent.widget-chats` for the guide in the
bar (`ChatWidget`) and `louis-agent.showroom-chats` for the showroom's live demos (`MockLiveChat`). Each saved
`Chat` carries its `SessionId`. `ChatStore.LoadAsync` already reads such a list, and gives an empty one for a
list it can't read. `AgentApi.DeleteSessionAsync` already ends a session, and treats one the server no longer has
as done. Beside the chats the browser holds `louis-agent.apiKey`, the look (`site.theme`, `site.style`) and the
chat's choices (`chat.*`); no code uses sessionStorage today, so clearing it only guards what comes later.

C# can't list the keys localStorage holds without a few lines of script, since `length` and the key list are
properties, not functions `IJSRuntime` can call. So the reset has a small module beside its component (ADR 0093),
loaded through `JsModule` and named in `FrameworkScripts`, like every other module. The site runs in the browser
on WebAssembly, so a reload also drops what the singleton `ChatStore` held in memory.

One consequence of the pick: the chat's API key is cleared too. On a site whose API needs one, the chat asks for
it again after a reset, as on a first visit.

What could not be confirmed by reading: that `NavigationManager.Refresh(forceReload: true)` reloads the page as
a browser reload does on our WebAssembly host. Microsoft's docs say it does from .NET 8; step 4 checks it in the
browser.

## Design

### 1. One class runs the reset, over what is there

`SessionReset` is a small Facade: one call, `RunAsync`, in front of the module, the chat store, the API and the
navigation. Inside it is a Transaction Script, one procedure in order. It asks the module for every key whose
name ends in `.chats`, so a list a later part adds is found without a change here. For each it builds a
`ChatStore` on that key and loads it, so a saved chat is read exactly as the chat reads it. It ends every
session through `AgentApi`, a failure for one not stopping the others. Then the module clears both storages and
puts the admin key back, and the navigation reloads.

![A small class runs the reset over the module, the chat store, the API and the navigation; the button and the module are new](diagrams/F153-reset-my-session-interface-design.svg)

The reset sits beside its button, in `Components/Admin/Reset/`, not in `Storage/` as option A first said:
`AgentApi` already depends on `Storage` (it takes `ChatSettings`), so a reset in `Storage` that calls `AgentApi`
would make the dependency run both ways (ADR 0010).

### 2. Where it runs

![The admin's home in the browser ends its sessions on the API it already calls, and clears its own storage](diagrams/F153-reset-my-session-architecture.svg)

Nothing changes on a server. The API's `DELETE /v1/sessions/{id}` is the one the chat's delete already calls.

### 3. One reset, in order

The sessions are ended before the lists that name them are cleared, so a reset that fails half way still has
the ids to try again.

![The sessions are ended before the lists are cleared, and the admin key is the one thing put back](diagrams/F153-reset-my-session-communication-one-reset.svg)

![The reset holds one module and one chat store per saved list until the reload](diagrams/F153-reset-my-session-objects-in-memory-one-reset.svg)

### 4. The button

`ResetSession` is a card-sized section above the area cards on `AdminHome`: a line saying what it does and one
press, "Reset my session", which reads "Resetting…" and is disabled while it runs. Its words are in its markup,
as `AdminGate`'s are. It wears the theme's press through the surface tools, with no colour of its own. One press
resets, with no second press to confirm: what it clears is this browser's visitor state, and the admin key stays.

### 5. The namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `SessionReset` | `src/web/louis-agent.web/Components/Admin/Reset/` | `LouisAgent.Web.Components.Admin.Reset` | 2 |
| `ResetSession.razor.js` | the same | a module | 2 |
| `FrameworkScripts.Reset` (changed) | `src/web/louis-agent.web/Scripts/` | `LouisAgent.Web.Scripts` | 2 |
| `SessionResetTests` | `src/tests/web/louis-agent.web.tests/Components/Admin/Reset/` | `LouisAgent.Web.Tests.Components.Admin.Reset` | 2 |
| `ResetSession` (`.razor`, `.razor.cs`, `.razor.scss`) | `src/web/louis-agent.web/Components/Admin/Reset/` | `LouisAgent.Web.Components.Admin.Reset` | 3 |
| `AdminHome` (changed) | `src/web/louis-agent.web/Pages/Admin/` | `LouisAgent.Web.Pages.Admin` | 3 |
| `AdminHomeTests` (changed) | `src/tests/web/louis-agent.web.tests/Pages/Admin/` | `LouisAgent.Web.Tests.Pages.Admin` | 3 |

### 6. What is reused

`ChatStore` reads every saved list, `AgentApi` ends every session with its address and its keys, `JsModule`
imports the module, `NavigationManager` reloads, and the surface tools draw the press. No new package.

## Steps

Each step is one commit with its test, written first and seen failing on an assertion. Stop after each for
Louis's review.

### Step 1: the plan is kept

A docs-only step. This plan and its pictures are committed, with its rows in the plans' index, the TODO and the
work log's open table.

### Step 2: the reset

The test comes first. `SessionResetTests` runs `SessionReset` on bUnit's JavaScript stand-in, with the module's
`chatKeys` set up to return two keys and `localStorage.getItem` to return a saved list for each, and an
`AgentApi` over a stub handler that records each request. It holds that every saved session gets one `DELETE`,
that a session the server answers 404 for doesn't stop the rest, that `forget` is called with the admin key's
name after the last `DELETE`, and that the page is reloaded.

Then `SessionReset`, its module and its line in `FrameworkScripts`.

### Step 3: the button on the admin's home

The test comes first. `AdminHomeTests` gains: the home draws the reset above the cards, and a press runs it,
shown by the stub's `DELETE`s and the module's `forget`.

Then `ResetSession` and its place in `AdminHome`. The site is rebuilt on :5080 for Louis to look at.

### Step 4: the docs, and the pull request

The web's design doc says, in its part on the admin, what the reset clears, what it keeps and in which order, and
its *Structure* row names the folder. The write-up is written, this plan is folded and deleted, and the records
move to done. In the browser, at 1920 by 1080 and at 390 px: a chat with the guide, then a reset from `/admin`,
then the landing opens as a first visit, and the admin opens without asking for its key again. `main` is merged
in, the full unit suite passes, and the pull request is opened on Louis's word.

## Verification

The tests named in each step, then the full unit suite. By hand in the browser on :5080, as step 4 says. A
question to the guide before the reset is a small paid call, and its cost is reported.

## Concepts

Step 2 is the one to study. An agent's conversation lives in two places, the server's session and the browser's
copy, and resetting it means ending both, in the right order: the server first, while the browser still holds the
ids. That is sessions and state, skill 9, and the test that proves it without a browser or a server is testing and
shipping, skill 18, both in [the goal](../GOAL.md).

## Approved during the build

---
[Docs index](../README.md) · Related: [Plans](README.md), [louis-agent.web](../projects/louis-agent.web.md)
