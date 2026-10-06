# Orchestrator demo: one agent watching a microservice

A proof of concept for the "one orchestrator per microservice" design in [TODO.md](../TODO.md#design-one-orchestrator-per-microservice):
an orchestrator agent watches a service's errors, decides what each one needs from a runbook per API method, hands
code fixes to louis-agent, and then checks louis-agent's work itself.

```
order-service ──errors.jsonl──▶ orchestrator ──POST /sessions (SSE)──▶ louis-agent.api ──▶ fix branch + commit
 (samples/)                     (louis-agent.core,                     (WORKSPACE_ROOT =
                                 runbook per method)                    the service repo)
                                      │
                                      └─ verifies: commit in git, replay the request, run the tests
```

## Run it

```powershell
.\samples\run-demo.ps1            # add -KeepApi to leave louis-agent.api running afterwards
```

Needs the .NET 10 SDK, git, and `ANTHROPIC_API_KEY` in `config/.env.secrets`. The script:

1. copies `samples/order-service` to `.demo/order-service` (git-ignored) as a fresh git repository on `main`;
2. replays `data/requests.txt` against the service, which logs four exceptions to `logs/errors.jsonl`;
3. starts `louis-agent.api` on `http://127.0.0.1:5081` with that repository as its workspace;
4. runs the orchestrator once over the new errors, prints the commits and decisions, and stops the API.

## The pieces

| Piece | Where | Role |
|---|---|---|
| order-service | `samples/order-service` | A console stand-in for a microservice: three API methods over `data/orders.csv`. Unhandled exceptions go to `logs/errors.jsonl`, standing in for Exceptionless. |
| Orchestrator | `src/louis-agent.orchestrator` | An `AgentEngine` from `louis-agent.core` whose system prompt is the runbooks and whose only tools are `ServiceTools` (no file, git or shell tools). |
| Runbooks | `src/louis-agent.orchestrator/Skills/` | `orchestrator.md` (workflow), `order-service/service.md` (owner, deploy policy) and one file per API method: what it does, which exceptions are expected, auto-fixable or escalated, and the correct behaviour. |
| louis-agent | `src/louis-agent.api` | Does the fix with its usual tools: branch, edit, regression test, build, test, commit, then a `FIX-RESULT` line. |

## What the demo traffic triggers

| Request | Exception | Runbook says | Orchestrator does |
|---|---|---|---|
| `order-total 1003` | `KeyNotFoundException` (retired discount code `SUMMER25`) | auto-fixable: unknown codes give no discount | `CallLouisAgentFix`, then confirms |
| `refund 1002` | `InvalidOperationException` "already been refunded" | expected | `AcknowledgeError` |
| `refund 1005` | `ArgumentOutOfRangeException` (no card reference) | payments: never auto-fix | `EscalateToHuman` (payments) |
| `get-order 9999` | `OrderNotFoundException` | expected (404) | `AcknowledgeError` |

## How a fix is confirmed

louis-agent's `FIX-RESULT` (status, commit, branch, tests) is a claim. The orchestrator only reports `fixed` when all
three of its own checks pass:

1. `VerifyFixCommit` — in the service's git repository, the commit exists, is on the reported fix branch (checked
   out), is **not** on `main`, changed files, and the working tree is clean; it shows the author (`louis-agent`).
2. `ReplayRequest` — the failing call now answers `200` (replays write to their own error log, not production's).
3. `RunServiceTests` — the whole suite passes, including louis-agent's regression test.

Anything else is escalated. Nothing is merged or deployed: the fix waits on `fix/<method>-<error id>` for a human.

Outputs land in `.demo/orchestrator-state/`: `decisions.jsonl`, `fixes.jsonl` (louis-agent's reply),
`escalations.jsonl`, `acknowledged.jsonl` and `processed.txt` (so an error is triaged once). louis-agent.api's log
is `.demo/louis-agent-api.log`.

## Running the parts yourself

```powershell
# louis-agent.api on the service repository
$env:WORKSPACE_ROOT = "$PWD\.demo\order-service"; $env:ASPNETCORE_URLS = 'http://127.0.0.1:5081'
dotnet run --no-launch-profile --project src/louis-agent.api

# the orchestrator, polling every 15 s (ORCHESTRATOR_POLL_SECONDS), or once with --once
$env:ORCHESTRATOR_SERVICE_ROOT = "$PWD\.demo\order-service"; $env:LOUIS_AGENT_URL = 'http://127.0.0.1:5081'
dotnet run --project src/louis-agent.orchestrator
```

| Setting | Default | Meaning |
|---|---|---|
| `ORCHESTRATOR_SERVICE_ROOT` | (required) | The service's git repository: the same folder as louis-agent.api's `WORKSPACE_ROOT` |
| `ORCHESTRATOR_SERVICE` | `order-service` | Selects `Skills/{service}/` |
| `ORCHESTRATOR_SERVICE_PROJECT` | `src/OrderService` | Project used to replay requests |
| `LOUIS_AGENT_URL` | `http://127.0.0.1:5081` | The louis-agent.api instance for this service |
| `LOUIS_AGENT_API_KEY` | `AGENT_API_KEY` | Sent as `x-api-key` when louis-agent.api requires a key |
| `ORCHESTRATOR_STATE_DIRECTORY` | `logs/orchestrator/{service}` | Decisions, fixes, escalations, processed ids |
| `ORCHESTRATOR_POLL_SECONDS` | `15` | Poll interval when not run with `--once` |

The model is louis-agent's usual `LLM_*` configuration.

## Adding a service or a method

- **A method:** add `Skills/{service}/{method}.md` with a `## Method: {method}` runbook. The file name is the method
  name the error events carry; `ReplayRequest` only accepts methods that have a runbook.
- **A service:** add `Skills/{service}/service.md` plus its method runbooks, run one orchestrator per service with
  `ORCHESTRATOR_SERVICE={service}`, and point it at that service's own louis-agent.api instance.

## Differences from the design in TODO.md

- The orchestrator is built on `louis-agent.core` (`AgentEngine` with a custom toolset) rather than Anthropic's Tool
  Runner, so it reuses louis-agent's providers, skills loader and tool loop.
- The error feed is a JSON-lines file instead of the Exceptionless API, and a fix ends as a commit on a fix branch
  (the demo repository has no remote) instead of a pull request.
