# Louis Agent documentation

Read them in this order: each doc ends with a link to the next one. Start at the top if you are new; jump to the part
you need otherwise.

## 1. Use it

| Doc | Read it to… |
|---|---|
| [Project README](../README.md) | Get the overview: what Louis Agent is, the ways to use it, quick start, settings and endpoints at a glance |
| [Quick start](QUICK_START_AGENT.md) | Start the web app, Rider or the CLI, try example prompts, approve agent-built tools, troubleshoot |
| [Setup](SETUP.md) | Configure `config/.env` and `config/.env.secrets`: every setting, how they are loaded, keeping secrets safe |
| [Models](MODELS.md) | Choose and switch the model: Claude (default), Ollama, OpenAI-compatible servers; thinking and limits |
| [Paymo prompts](PAYMO-PROMPTS.md) | Log and review Paymo time in plain language (only if you use Paymo) |

## 2. Understand it

| Doc | Read it to… |
|---|---|
| [How a turn works](AGENT_INTERACTION.md) | Follow one message through the agent: tools vs skills, the tool loop, guards, what each host shows |
| [How the agents and endpoints talk](AGENT_COMMUNICATION.md) | See who connects to louis-agent and how: the HTTP API and its event stream, the web app, Rider (ACP), MCP, and the orchestrator ↔ louis-agent conversation |
| [Architecture](ARCHITECTURE.md) | Know the components: hosts, core abstractions, skills, tools, security, logging, deployment |

## 3. Change it

| Doc | Read it to… |
|---|---|
| [Development guide](CLAUDE.md) | Build and test, find your way around the code, add a tool, a skill, a theme or another agent; design decisions, known limitations, common errors |

## 4. See it work: the orchestrator demo

| Doc | Read it to… |
|---|---|
| [Orchestrator demo](../samples/README.md) | Run a second agent that watches a demo microservice, has louis-agent fix its ten bugs (one branch each), verifies every fix, and lets you merge them in the web app — all in Docker |
| [Runbooks](RUNBOOKS.md) | Understand how the orchestrator decides what each error needs, with real examples, and write your own runbook |
| [Sample output](../samples/sample-output/README.md) | See one real run without running it: the agents' full conversation, logs, branches and a recording of the merges |

## 5. What's next: plans and designs

Start with the TODO: it lists everything planned, next and in the backlog, and links to the rest.

| Doc | Read it to… |
|---|---|
| [TODO](../TODO.md) | See what's planned now (features to tick), next (the build-mode POC), the backlog by area, known limitations and what's done |
| [Features](features/README.md) | Follow the step-by-step plan for the usage and optimisation specs: 11 features in 5 milestones, with user stories, design, tasks, and what to update when one is finished |
| [Spec: usage, estimates and budgets](specs/USAGE_AND_BUDGETS.md) | Read the design for recording what every request costs, estimating runs before they start, and budgets that warn, ask or stop |
| [Spec: response optimisation](specs/RESPONSE_OPTIMISATION.md) | Read the design for cheaper, faster responses: prompt caching, smaller toolsets, clearing and summarising history, reliability |
| [Design: session architecture](specs/SESSION_ARCHITECTURE.md) | Read the longer-term idea for durable sessions, a context-building pipeline and memory (not planned yet) |
| [Design: orchestrators and self-healing](specs/ORCHESTRATOR_DESIGN.md) | See the orchestrator design decisions, what the POC proved, and what's still open |

---
Next: [Quick start](QUICK_START_AGENT.md)
