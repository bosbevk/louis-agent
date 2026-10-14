# Louis Agent documentation

Read them in this order: each doc ends with a link to the next one. Start at the top if you are new; jump to the part
you need otherwise.

## 1. Use it

| Doc | Type | Read it to… |
|---|---|---|
| [Project README](../README.md) | Tutorial | Get the overview: what Louis Agent and Puls are, the ways to use them, quick start, settings and endpoints at a glance |
| [Quick start](QUICK_START_AGENT.md) | Tutorial | Start the web app, Rider or the CLI, try example prompts, approve agent-built tools, troubleshoot |
| [Setup](SETUP.md) | Reference | Configure `config/.env` and `config/.env.secrets`: every setting, how they are loaded, keeping secrets safe |
| [Models](MODELS.md) | How-to | Choose and switch the model: Claude (default), Ollama, OpenAI-compatible servers; thinking and limits |
| [Paymo prompts](PAYMO-PROMPTS.md) | How-to | Log and review Paymo time in plain language (only if you use Paymo) |

## 2. Understand it

| Doc | Type | Read it to… |
|---|---|---|
| [How a turn works](AGENT_INTERACTION.md) | Explanation | Follow one message through the agent: tools vs skills, the tool loop, guards, what each host shows |
| [How the agents and endpoints talk](AGENT_COMMUNICATION.md) | Reference | See who connects to louis-agent and how: the HTTP API and its event stream, the web app, Rider (ACP), MCP, and the orchestrator ↔ louis-agent conversation |
| [Architecture](ARCHITECTURE.md) | Explanation | Know the components: hosts, core abstractions, skills, tools, security, logging, deployment |

## 3. Change it

| Doc | Type | Read it to… |
|---|---|---|
| [Development guide](CLAUDE.md) | How-to | Build and test, find your way around the code, add a tool, a skill, a theme or another agent; design decisions, known limitations, common errors |

## 4. See it work: the orchestrator demo

| Doc | Type | Read it to… |
|---|---|---|
| [Orchestrator demo](../samples/README.md) | Tutorial | Run a second agent that watches a demo microservice, has louis-agent fix its ten bugs (one branch each), verifies every fix, and lets you merge them in the web app — all in Docker |
| [Runbooks](RUNBOOKS.md) | Explanation | Understand how the orchestrator decides what each error needs, with real examples, and write your own runbook |
| [Sample output](../samples/sample-output/README.md) | Reference | See one real run without running it: the agents' full conversation, logs, branches and a recording of the merges |

## 5. See it work: Puls and its specialists

| Doc | Type | Read it to… |
|---|---|---|
| [Puls's web app](projects/louis-agent.puls.md) | Reference | Know Puls: the assistant in Afrikaans that hands each task to a specialist and shows the work in its Werk panel |
| [Delegation check](../samples/sample-output-puls-check-repeat/README.md) | Reference | See what delegating costs against one agent answering, three runs per mode, and what the fixes changed |
| [Multi-agent write-up](learning/10-multi-agent-orchestration.md) | Explanation | Read what building Puls taught: roles, routing, measured delegation (a draft) |

## 6. Project by project

| Doc | Type | Read it to… |
|---|---|---|
| [Project design docs](projects/README.md) | Reference | Before changing one project: its purpose, how it works, files, key types, settings, extension points, tests and limits — core, CLI, ACP server, API, web app, Puls, MCP server, orchestrator, usage probe, tests, and the demo service |

## 7. What's next: plans and designs

Start with the goal and the TODO: the goal says what the work is for and when it's done; the TODO lists everything
planned, next and in the backlog, and links to the rest.

| Doc | Type | Read it to… |
|---|---|---|
| [Goal](GOAL.md) | Explanation | See what the whole plan is for: the skills it teaches, when one counts as learned, and how the work is done |
| [TODO](../TODO.md) | Plan | See what's still open: planned now (milestones and features), next (the build-mode POC), the backlog by area, and the known limitations |
| [Changelog](../CHANGELOG.md) | Reference | See what was finished and when, newest first, each entry linking to its feature, fix or plan |
| [Features](features/README.md) | Plan | Follow the step-by-step plan for the usage and optimisation specs and for Puls: each feature with user stories, design, tasks, and what to update when one is finished |
| [Benchmarks](BENCHMARKS.md) | Reference | See every measured run: tokens, cost, cache hits and fixes confirmed, from the M1 baseline on |
| [Learning write-ups](learning/README.md) | Explanation | Read what each skill in the goal taught: what was measured before and after, the trade-offs, what didn't work |
| [Plans](plans/README.md) | Plan | Read the plan approved before each feature or fix was built: what it changed from the doc, the steps and how they were checked |
| [Fixes](fixes/README.md) | Explanation | See what broke, why and how it was fixed (symptom, cause, fix, how it was confirmed), and the fixes still open |
| [Spec: usage, estimates and budgets](specs/USAGE_AND_BUDGETS.md) | Explanation | Read the design for recording what every request costs, estimating runs before they start, and budgets that warn, ask or stop |
| [Spec: response optimisation](specs/RESPONSE_OPTIMISATION.md) | Explanation | Read the design for cheaper, faster responses: prompt caching, smaller toolsets, clearing and summarising history, reliability |
| [Design: session architecture](specs/SESSION_ARCHITECTURE.md) | Explanation | Read the longer-term idea for durable sessions, a context-building pipeline and memory (not planned yet) |
| [Design: orchestrators and self-healing](specs/ORCHESTRATOR_DESIGN.md) | Explanation | See the orchestrator design decisions, what the POC proved, and what's still open |

## Writing docs: one type per doc

Every doc is one of the four [Diátaxis](https://diataxis.fr) types, so a reader (a person or a model) opens only the
kind of doc its question needs:

| Type | Answers | Holds |
|---|---|---|
| **Tutorial** | Get me started | Steps that work the first time, nothing optional |
| **How-to** | How do I do X? | One task's steps; links for the why |
| **Reference** | What exactly is X? | Settings, endpoints, types, results: complete and plain |
| **Explanation** | Why is it like this? | Design, trade-offs, what was measured and learned |

The project's planning docs (the TODO, features, plans) are a fifth kind, **Plan**: what is still to be built. Each doc
opens with a quoted line or two: its type, what it answers, and where the rest is. A section that answers a different
question moves to the doc of that type, and the two link to each other.

---
Next: [Quick start](QUICK_START_AGENT.md)
