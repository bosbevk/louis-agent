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
| [TODO](../TODO.md) | See what is open: known limitations, the session-architecture plan, backlog ideas |
| [Features: usage and optimisation](features/README.md) | The step-by-step plan for both specs: 11 features in 5 milestones, each with user stories, design, tasks and a definition of done |
| [Spec: usage, estimates and budgets](specs/USAGE_AND_BUDGETS.md) | The plan for recording what every request costs, estimating runs before they start, and budgets that warn, ask or stop |
| [Spec: response optimisation](specs/RESPONSE_OPTIMISATION.md) | The plan for cheaper, faster responses: usage tracking, prompt caching, smaller toolsets, clearing and summarising history |

## 4. See it work: the orchestrator demo

| Doc | Read it to… |
|---|---|
| [Orchestrator demo](../samples/README.md) | Run a second agent that watches a demo microservice, has louis-agent fix its ten bugs (one branch each), verifies every fix, and lets you merge them in the web app — all in Docker |
| [Runbooks](RUNBOOKS.md) | Understand how the orchestrator decides what each error needs, with real examples, and write your own runbook |
| [Sample output](../samples/sample-output/README.md) | See one real run without running it: the agents' full conversation, logs, branches and a recording of the merges |

---
Next: [Quick start](QUICK_START_AGENT.md)
