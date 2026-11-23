# 10 · Multi-agent orchestration: what Puls taught (draft)

> **Skill:** #10 in the [goal](../GOAL.md)'s map · **Stages:** F5 sub-sessions (live delegation), F21 personalities and
> roles, F22 role tools, F20's Werk panel (2026-10-13), F127.3.1 one address for every agent call (2026-11-11) ·
> **Status:** a first draft by Claude for Louis to edit

![Multi-agent orchestration: a router and its specialists, what delegation cost, a triage team, one address for every agent call, and minions](diagrams/10-multi-agent-orchestration.svg)

The parts below come in the order they were built, and some names changed on the way. The router's table that was
`default.md` is now the routing half of a mode's file (Puls's `agent/puls/voice.md`, after its voice). The Werk
panel is the chat's work panel. And `StartSubSession` gave way to `AskAgent` by address (F127.3.1, below).

## Before and after

**Before,** one agent did everything: every tool and every guide in one prompt, about 28,600 tokens of fixed prefix
(109 tools), in one voice ("Louis's Minion") whatever the host.

**After,** Puls is a router that does no work itself. It reads the request, picks the toolsets and the **role** from one
table (`default.md`), and hands the task to a specialist on a peer (`StartSubSession` then, `AskAgent("puls.cook")`
since F127.3.1, below), which runs with only those tools and a personality that is its job description. The user
watches the work beside the answer.

| Prompt (fixed prefix, estimated) | Tools | Tokens |
|---|---|---|
| Every toolset, `personality.md` (before) | 109 | ~28,600 |
| Puls: `agents`, `puls.md`, the router | 2 | ~2,200 |
| A researcher: `web`, `puls-researcher.md`, no router | 3 | ~1,900 |

F5's short benchmark measured the same idea on the orchestrator demo: the prefix about 48.5k → 24.4k tokens, the run
$0.0488 → $0.0378, 4/4 fixes.

## Measured: delegation against one agent (2026-10-13/14)

A delegation check (a script under `samples/`, since removed with the other samples) asked the same ten questions twice, each in its own session:
once to one agent with every toolset (the baseline), once to Puls with its specialists. It priced both from the usage
ledger, by token kind.

| Run | Baseline | Delegated | What it found |
|---|---:|---:|---|
| [First, five questions](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check/README.md) | $0.0167 | $0.0218 (+31%) | Fewer tokens (−60%), but dearer kinds: each specialist writes its own prefix to the cache, and the answer is written twice. Puls passed a role 1 time in 4 |
| [Roles derived on the peer](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-roles/README.md) | $0.0324 | $0.0509 (+57%) | Every delegation had a role. Three refused calls (a required `agent`), an empty answer retried from scratch, a blocked `.env.example` |
| [After FX10–FX12](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-fixes/README.md) | $0.0279 | $0.3128 | One counting task went to a files-only reader, which read 74 files into a 298k-token prompt at the long-prompt price: $0.278 |
| [Three runs, after FX10–FX14](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-repeat/README.md) | $0.0276 mean | $0.0363 mean (+31%) | All 24 delegations routed by the table; the dearest question $0.0174; runs varied +6%, +11%, +86% |

What the numbers say:
Fewer tokens isn't cheaper. The baseline's tokens are mostly cache reads, the cheapest kind; a specialist starts cold
and writes its prefix, and output (the dearest kind) roughly triples.

Delegation pays for chat and some research, not for short lookups. Chat and the first web question (the newest .NET)
were cheaper delegated in every run. The second web question (the prime rate) was dearer delegated in the roles and
FX10–FX12 runs ($0.0104 against $0.0052, $0.0086 against $0.0059) and about even in the repeat run ($0.0078 against
$0.0081 on the means). Short files, git and counting questions were 1.2–3.5× dearer on the repeat run's means (files-2
and git-2 were cheaper once, in the roles run). Whether Puls delegates too often is still open (see *Next*).

The tail matters more than the average. One badly routed task cost eight times the other nine together. The fix was
two layers: a prompt that routes and tells every agent what tool calls cost (FX13), and a limit in code, a prompt
budget per sub-session under the long-prompt price step (FX14).

One run per mode isn't enough to decide: the same ten questions varied from +6% to +86%.

## What changed, and why this way

A role is a personality (F21). The specialist's system prompt starts with its function and how to report
(`puls-researcher`: sources with dates; `puls-historian`: commits; `puls-analyst`: the method and the script), on a
shared base: one task, never the user, verify, say what's missing, can't delegate. Considered: one generic specialist
prompt (simpler, but every report came back in a different shape), and roles as toolset guides (but a guide describes
tools, not a job).

The router names toolsets and role together. The decision starts from the request ("the world" → `web` +
`puls-researcher`), so both live in one table row; a separate toolset → role table was one more lookup to forget.

Sub-sessions get no router (`default.md`): their toolsets were chosen for them and they can't delegate, so the router
only cost tokens and could mislead them.

Context crosses the boundary as data, not through the model. Attachments go by name (the session supplies the
content), the user's place and time zone are set per turn (`UserPlace`); the model never copies a file into a tool
call.

Tools per role (F22): what each job needed and lacked (today's date, documents, blame and history search, tables,
currency), on maintained packages, not our own parsers.

Transparency is part of the design (F20): the Werk panel shows each specialist's steps, sources and cost, so
delegation isn't a black box, and the chat's cost includes the specialists. F24 made it the landing page's thesis: the
page plays a delegated turn out of the app's own pieces (the card busy, the Werk steps ticking, the cost counting), so
a visitor sees the design before the first question; and in the app a cost that counts up and a card that opens from
its header are motion carrying that same information, not decoration.

## Trade-offs and what didn't work

**The roll-up came after.** At first the chat's usage left out its sub-sessions, and the app added them up client-side
and said so. F5's follow-up (2026-10-14) rolled a sub-session's usage into its parent's turn, so the totals hold it.

**A role in the prompt wasn't enough.** Puls passed one in 1 delegation of 4. The peer then derived a role from the
toolsets when none was passed (`roles.json`), so code, not the prompt, made sure every specialist had one. Today the
role is part of the address (`puls.cook`), so there is nothing left to forget, and `roles.json` is gone.

Routing is still a prompt, and a wrong route can be expensive (the $0.278 reader). Each role now says what is not its
job (the reader hands counting back), and a prompt budget limits what a wrong route can cost.

**Contracts between agents need the same care as tools.** A parameter the description said to leave out, but the schema
required, was refused before the tool ran (FX10); an empty answer needs a follow-up in the same session, not a new
specialist (FX11).

**Two models' worth of latency.** Every task costs Puls's turn plus the specialist's; worth it for tool-heavy work, not
for chit-chat (Puls answers those itself).

**Mistakes found by looking.** `AGENT_FUNCTION=louis` loaded every skill file into Puls's prompt (fixed: skill files come
with the `skills` toolset); the model guessed a peer name from an example (fixed: an empty name means the only peer).

## A triage team, recorded by hand before the workflow exists (F106.3.4, 2026-11-09)

**What it means.** The site's `/try` page plays one real triage: a sorter routes an error, a manager decides, an
investigator finds the cause, a coder fixes it on a branch with a test, code checks the fix, and a reviewer keeps it.
The workflow that will run that in code (F106.3.3) waits on the engine's typed delegation, so a short script did the
workflow's part by hand, once: it asked each agent in turn with the brief the workflow will hand it, and each agent
ended its turn with a typed tool call the triage pack saved as a step.

**What the five tries taught.** Orchestration fails at the joins, not in the agents:
The container's git saw line endings as changes, so the coder never committed, and the script still recorded a fix. A
recording, like a workflow, must check what happened in code before it says so; the script now stops instead. The
coding agent leaves commits to a person, by its own voice. That is right for a chat and wrong for a pipeline, so the
commit became the workflow's job: deterministic steps belong in code, judgement in the agents. A reviewer couldn't
switch branch on the container's older git (FX107), and an investigator left out an argument three times because the
refusal never named its choices (FX108). Both are tool problems an eval of one agent wouldn't show; a run across
agents found them in an afternoon.

**Where it sits in the skill map:** multi-agent orchestration (skill 10), a hub of roles with typed results; and
testing and shipping (skill 18), since the same run file a live workflow writes is what the page plays.

## One address for every agent call (F127.3.1, 2026-11-11)

**What it means.** An agent now asks another agent the way you'd phone someone: by name. `AskAgent("seemeeu-guesthouse",
"may I bring my dog?")` asks a business in its own voice, and `AskAgent("puls.cook", …)` asks one of Puls's roles.
The asker never picks the other side's tools, model or prompt: the address says who answers, and that agent's own
files decide how. Asking the same address again carries on the same conversation. Under every answer sits a line,
`[Answered by seemeeu-guesthouse, did: SendLead]`, and `ReadAgentWork` gives the steps behind it when someone asks.
The old `StartSubSession`, where the model chose a role and toolsets itself, is gone ([ADR 0117](../adr/0117-one-address-for-every-agent-call.md)).

**Why this way.** A tool with fewer choices is safer and easier for a model to use well (skill 4). On a public site
the guide must not be able to open a session with any toolset it likes, so the choices sit in code: who an identity
may ask is its `Calls`, checked before anything is sent (skill 15). What the asker's model reads is kept small on
purpose (skill 7): the answer and one line of facts, and the detail only when asked (Lazy Load). The session wire
became shared records, so core, the API and the web send and read one shape (skill 11).

**What the build taught.**
An allow-list in code isn't enough on its own. The first live run had the guide say it "had no line" to the business:
the code would have let it ask, but its prompt never said so. Now the prompt lists the same `Calls` the code checks,
so what the model reads and what the code allows can't drift apart.

Who answers decides its prompt, not how deep it sits. A business asked by the guide opens one level down, but in its
own voice, because a role's level now comes from its file's folder. Depth only stops it asking further.

Say what was done from facts. The line under an answer is built from the agent's own tool calls, so the guide says
"Seemeeu sent your booking" only when `SendLead` really ran, never because the answer sounded like it.

A choice per identity, not per instance. Puls shows its cook's answer whole and ends its turn (ADR 0079); the guide
keeps the last word. They share an instance, so the switch moved from a setting into each identity.

Recorded data follows a rename honestly. Puls is paused, so its 109 eval transcripts were rewritten onto `AskAgent` by
one checked script, every other line byte-for-byte as recorded, and the before-and-after eval waits for Puls's return
(F135). Two live guide runs cost $0.0053 together.

**Where it sits in the skill map:** multi-agent orchestration (skill 10), one way to name every agent call; tool
design (4), context engineering (7), safety (15) and protocols (11) as above.

## Handing the conversation over (F127.4.2, 2026-11-12)

The guide could ask a demo business and pass its answer on. Louis wanted more: the guide asks the business for the
visitor, the page shows that talk in the business's own window, and the visitor carries on there, while the guide
can still say what happened.

**What it means.** One agent opens a conversation with another for a person, then steps aside. The business's
session is the same one throughout: the guide's ask, the visitor's clicks and words in the window, the booking. Two
parties talk to one agent, and both can see the whole of it.

**What the build taught.**
The handover needs the conversation's id, not its words. The business's session id was already in the stream (its
`message_start`), so the page could take the conversation over without a new event or the guide relaying it.

The asker must read the whole session, not its own copy. What the visitor typed in the window never streamed through
the guide. So the server serves a session's work, and `ReadAgentWork` reads it there. On the PC the guide retold the
visitor's booking it never saw.

Count the person, not the server. An agent's ask opens the business's session from the server's own address, which the
door counted against the server. Now it counts against the visitor whose session asked, under their cap, and admits it
for them.

Facts beat names in the did line. "SendLead" says a tool ran; "SendLead failed: Error: not sent" says what happened.
Each call's first result line is enough to retell it right.

Look on the device it runs on. At 390 px the guide's panel covered the very window the answer was in. On a narrow
screen the guide now folds and the window scrolls into view. No test of ours could have seen it.

**Where it sits in the skill map:** multi-agent orchestration (10), a conversation handed from one agent to a person
and another agent; observability (17), the work of one agent readable by another; safety (15), the cap follows the
person; human in the loop (16), the visitor takes the wheel. Live checks for the day cost $0.0284.

## Minions on the address (F127.5, 2026-11-12)

The last call off the address: an agent asking its identity's minion. It was first built as its own tool,
`StartMinion(address, …)`, and Louis corrected it: "it should not startminion it should use askagent also". So one tool
asks everything: `AskAgent("puls.find-recipe", task, tool: "FindRecipes")`.

One list, one tool. An identity's `Calls` hold its agents and its minions. The address says who; handing a tool says
it's a minion, since only a minion takes one. A tool sent to anyone else is refused, and so is a minion without one.

Each kind keeps its own limits. A minion still runs a local model on this instance (ADR 0081): its model comes from
`MINION_LLMS`, at most 16 a user turn, never kept, never one of the turn's three asks. An address on another server is
fine for an agent (F127.3.2) and refused for a minion.

Who may ask whom is a rule, not a depth. Louis: "everyone should be able to call everyone, if it is setup that way
only minions can't". So depth no longer decides the kind of agent: a session's level is the folder its role is in, and
depth only counts how far down it is.

Loops are stopped two ways. A chain of asks never holds the same agent twice ("a cook can't call a cook. but it could
call a recipe specialist"): each session remembers the chain it was asked through, and `AskAgent` refuses an agent
already in it before anything is sent. And `AGENT_MAX_DEPTH` (default 4) caps how deep any chain can go, as a setting.
The first rule catches the loop you can name; the cap catches the long chain you didn't foresee.

No live run. Puls, the only identity with minions, is paused, and no voice asks one today; the tests carry it.

## Subagents inside their agent (CL68.2, 2026-11-22)

Reading the engine closely showed something the word "specialist" hid. A specialist runs on exactly the same loop as
the agent you talk to. What makes it different is only what it's given: its own instructions, only the tools its
work needs, and a fresh conversation. Louis put it in four words, "So it is actually a SubAgent", which is also what
Claude Code calls an agent with its own instructions file. So the level below the agent is now a subagent, in the
code, the headers, the prompts and the pages.

Where it lives changed with the name. A subagent used to sit in a `specialists/` folder of its own, apart from the
agent it belongs to, as `specialists/triage-sorter.md`. Now it lives inside its agent, a folder each:
`agents/triage/subagents/sorter/voice.md`. You can see who owns it by looking at the tree, and the folder has room for
the subagent's own skills one day. ADR 0133 records it, and it replaces ADR 0017's folder per level.

The best part is the name. A subagent used to have two: its address, `triage.sorter`, which a caller writes, and its
role, `triage-sorter`, which was its file's name, with a step in the engine that turned the dot into a dash. Now the
address is its only name, in a call, a session's `role`, `ROLES_ENABLED`, an eval and the logs, and the engine reads
it as the path. The triage team lost a whole enum that only existed to hold the second spelling.

The level moved into the header. ADR 0017's folders stopped a role opening at the wrong level. With subagents inside
their agents there's no folder per level any more, so a prompt's level is the `level` its header already named, and a
rule test refuses a header whose level isn't the one its place stands for. I proved that it bites by setting the test
subagent's header to `agent`: the test named the file at once.

Three things surfaced during the build. The plan's order didn't work: renaming a subagent before its file moved
needed something to find the old file from the new name, so for one step only the catalogue knew it. Louis also asked
"should this not be shared enums?" of the folder names, and he was right by our own rule, ADR 0043: the prompt
folders and files are now `PromptFolder` and `PromptFileName`, in Composition, because where a prompt sits is the
contract every identity and pack follows. And a test that forbids a lowercase `prompts/` folder fired on Windows for
the new C# folder `Prompts/`, because Windows ignores case when it looks a folder up; it now compares the name exactly.

What wasn't done: no live run (only names and paths moved; the coding agent's prompt changed by one word, which its
golden file shows). The framework's web library still called its chat code by the old word (`SpecialistAnswer` and
others) until CL69, and the eval protocol's `specialist_calls` waits for the evals to be rebuilt. Puls follows when it moves to
this version.

## Lookup agents beside the subagents (CL68.3, 2026-11-22)

The smallest helper got the same treatment as the subagents. A minion was a worker on a cheap model that does one
task with one tool its asker hands it; Louis's words for it were "lookup-agents sub-agents", and that's now its name
everywhere but the evals: in the code (`RoleLevel.Lookup`, `LookupAsk`), the setting (`LOOKUP_LLMS`), the API's roles
reply, the chat's card ("Lookup agent", with an "l"), and every word the model reads, down to the footer under its
answer, `[Lookup acme.lookup on ollama with WebSearch]`.

It lives inside its agent too, but in a folder of its own: `agents/{identity}/lookups/{name}/voice.md`, beside
`subagents/`, with the base every lookup agent loads first at `agents/{identity}/lookup.md`. The plan had put it in
`subagents/` and told the two apart by the header's level. Building that showed `RoleCatalog` would have to read the
headers, which core's architecture rule keeps it from. Louis picked a folder of their own over bending the rule: "B
will read better thinking of big identities which does alot". So the folder says the kind, the header says it too,
and the header rule test holds that they agree.

Two smaller things came out of it. The prompt folders and files became the shared enums CL68.2 started
(`PromptFolder.Lookups`, `PromptFileName.Lookup`), so nothing spells `lookups` or `lookup.md` by hand. And every message
`LookupAsk` and `AskAgent` built from an anonymous object now has a named holes record, in a holes folder split by
concern. One honest slip: step 7 claimed the model's words were done, and a grep for the old word found five more
places it reads, fixed in the next step with their golden files. The lesson for the procedure is to grep every prompt
and every `[Description]` for the old word after a rename, not to trust the step's own list.

That closes CL68. The prompt tree now reads the way the engine reads it: `agents/{identity}/` holds a voice, its
skills, its subagents and its lookup agents, each named by an address that is also its path. What's left of the old
words in code comments and test names is CL69, below. `AgentSessions.Create`, which Louis found ugly on the way, became
a method object in CL70 (in the [extensibility write-up](20-extensibility.md)).

## One word for one thing, in the code too (CL69, 2026-11-22)

CL68 renamed the levels where the model and the visitor read them. The code kept the old words: a class in the web
library (`SpecialistAnswer`), about forty test names, a test subagent called `testprod.specialist`, and comments in a
hundred more files. That matters for the same reason the prompts did. A reader, or a model reading the code, meets two
names for one idea and wonders whether a specialist is still a level. A search found far more than the TODO said, 517
lines in 133 files outside the evals, so the scope went to Louis first. He picked everything outside the evals, the
test fixtures included; the eval code waits for its rework, which he has said will start over.

The work started with a guard, not a rename. `OldWordsTests` reads the repo and holds two things: no `[Description]`
says either word, since a word the model reads steers what it does, and only the files on a list may still say them.
The list began with today's 134 files, and each step took its own off, so the change could land in nine reviewable
steps and never slide back. That is a ratchet, the same shape as `WriteUpsTests`. At the end the list was empty and the
test became a plain rule. Its first run found something the plan had missed: a fourth text the model reads,
`VerifyQuote`'s own description, which still said "every quote a minion returns".

Most of the rest was renaming, and the lessons are about how a mass rename goes wrong. Symbols went through Rider's
rename, which follows every reference; the words in comments went through a small word map, and every step's diff was
read. The map made four kinds of mistake that the tests or the reading caught. It rewrote two of Louis's quotes, so the
map and the test now leave a quote of his alone. It changed tests that say the old words on purpose, to prove the old
level names are refused, so those tests are kept places. It turned a local variable called `minion` into two words,
because a name collected once per name rather than once per file slipped past Rider. And it broke an ADR's link, whose
file name keeps the old word. Names like that, and the old setting `MINION_LLMS` that a test proves sets nothing, are a
short list the guard doesn't count.

Two things grew out of the rule that a touched file is fixed whole. Changing one word in the web pack's quote tool
touched the web toolset's messages, whose 37 texts had no holes records; Louis chose to do all of them in a step of
their own, 48 records with about 70 call sites, held by a new check in `MessageEnumTests`. And a long tool description
joined with `+` looked bad in Rider, so Louis asked how to lose the `+`. A tool's description is now a raw string. A raw
string's line breaks are its file's, and git was turning them into Windows line endings here and plain ones on Linux,
so the model would have read different text on each; `.gitattributes` now keeps every C# file on plain line endings,
and the rule is in the C# rules.

What it cost: nothing was paid for. The usage probe, which has no tests, was checked by running the old and the new
build on two real ledgers: the same output, line for line.

## A fixed order of agents: sequences (F176, 2026-11-23)

Until now every agent decided for itself whom to ask, with `AskAgent`. That is what Anthropic's *Building effective
agents* (2024-12-19) calls an agent: a system where the model "dynamically direct[s] its own processes and tool
usage". The same article names the other kind a workflow, where models and tools "are orchestrated through
predefined code paths", and its first workflow is prompt chaining: a task cut into steps, each model call working on
the output of the one before, with a programmatic check, a gate, between steps. A sequence is that idea on our
addresses. An identity lists a few addresses from its own `Calls` in an order, and code walks them. The model can't
skip a step, ask them the other way round, or forget the reviewer.

The interesting choice was where a sequence starts. It isn't an address of its own. The engine forces it on the ask:
when the model asks the address that is a sequence's first step, code runs the whole sequence and hands back one
answer, for one of the turn's three asks. A later step can't be asked alone, and the refusal says which address to
ask. So the model's habit stays the same (it asks an agent), and the order is held where the model can't change it.
The gate is in code too. Before any step opens, the sequence's rules are checked (at least two steps, each in
`Calls`, none twice, an address in one sequence only, no lookup agent), and between steps each ask is held to the
same checks as any other: the allow-list, the depth cap and the chain of asks.

What a step is given turned out to be a real design question, so it is a choice per sequence. Microsoft's Agent
Framework, in its sequential orchestration (the page dated 2026-10-26), passes each agent the previous agent's full
conversation by default. Ours defaults to less: the first message with the answer of the step before. That keeps
each step's context small and its job clear, which matters because every token a step reads is paid for again on
each of its requests. The full conversation and the bare previous answer are the other two values. What a failed
step does is a choice the same way: stop and say where (the default, so nothing silently half-happens), ask it once
more, or skip it, and a skipped step is always named in the answer.

A trade-off worth knowing: the sequence's own name joins the chain of asks only inside the runner, where its steps
are checked. The API builds each child session's chain from its parent's, so a step's own asks see the agents above
them but not the sequence's name. They are still held by the chain of agents and by `AGENT_MAX_DEPTH`, so a loop
can't run away, but a loop is stopped there rather than at the sequence.

What it cost: nothing was paid for. The tests play both servers with a pretend handler, so every path (all three
inputs, all three failures, a step on another server, the limits) ran without a model.

## Next

Decide how often Puls should delegate (keep research and long work, answer short read-only questions itself?) and
measure it the same way; Puls on `AskAgent`, measured when Puls resumes (F135); more evals per role for quality, not
only cost; and parallel subagents for a task with independent parts.

---
[Learning](README.md) · [Goal](../GOAL.md)
