# F26 · Specialist follow-up: plan

> Approved 2026-10-15 · Feature: [F26 Specialist follow-up](../features/F26-specialist-follow-up.md) · Branch:
> `claude/project-thread-381vmw`, with F25 (Louis: "implement cook fully, run benchmark"; the cook check asks its
> follow-up and the container carries both)

## Context

Louis (2026-10-15): "if I ask recipe questions, the specialist must answer, and if the user gives a follow up Puls must
direct it again to the same cook specialist. Just check if it does not work like that already." It doesn't:
`AgentPeerTools.StartSubSession` deletes the peer's session in its `finally`, and nothing remembers it.

## Design

What changes from the feature doc: nothing; the doc was written with this plan.

- **Reuse before building:** the per-turn context pattern of F22 (`SessionAttachments`, `UserPlace`: an `AsyncLocal`
  the host sets for a turn), `SendAsync` and its roll-up for the continue, `StubHandler` for the tests. Nothing
  third-party does this (Microsoft.Extensions.AI has no sessions between agents). Considered: a `keep` argument on
  `StartSubSession` instead of a registry (the model would decide what to keep; it can't know what the user asks next)
  and one tool `AskSpecialist(role, message)` that reuses an open session for the same role (hides the choice; two cooks
  in one chat would collide). Chosen: the registry keeps every sub-session until the cap, and the model continues by
  handle.
- **`SubSessionRegistry`** (`louis-agent.core/Tools/Agents/`): `Use(registry)` / `Current` like the others; `Add` returns
  the next handle (`sub-N`) and closes the oldest past the cap; `TryGet(handle)`; `CloseAllAsync()` deletes each on its
  peer (the registry carries the peer URI and the API key's `HttpClient` from the tools). The API's `AgentSession` owns
  one; `AgentSessions.Remove` and the idle sweep close them.
- **`StartSubSession`:** when a registry is current, registers the session instead of deleting it, and the footer
  becomes `[Sub-session sub-1 on {Agent} ({Toolsets}, {Role}): {Usage}; continue with ContinueSubSession("sub-1", …)]`.
  Without one, as today. A failed or timed-out start is still deleted.
- **`ContinueSubSession(handle, message)`:** a turn slot (same count as starts), the timeout, `SendAsync`, the FX11
  retry for an empty answer, the same roll-up; unknown handle → `ToolMessage.SubSessionUnknownHandle` listing the open
  ones; a peer that answers 404 (its idle drop) → the handle is removed and the error says to start over.
- **Prompt:** *Following up* in `agents-skills.md`; one line in `default.md`'s delegation rule.

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; TODO line; index rows | — |
| 1 | `SubSessionRegistry`; `StartSubSession` keeps the session when one is current, with the handle in its footer | unit tests (stubbed peer: no DELETE with a registry, DELETE without; the cap closes the oldest); golden files |
| 2 | `ContinueSubSession` | unit tests (right session, roll-up, unknown handle, 404, the per-turn count); golden files |
| 3 | API: `AgentSession` owns a registry, current per turn; delete and idle drop close children; `GET /sessions/{id}` lists them | API tests |
| 4 | Prompt: *Following up*; the router's line; CLAUDE.md's MCP note (an MCP call keeps nothing) | golden files; a chat by hand (free: the handle in the footer) |
| 5 | Measured: the cook check asks a follow-up (F25 step 2) | F25's run |

## Verification

`dotnet test` for core and API per step; the golden diffs reviewed; no model calls until F25's run, which has its own
approval. A chat by hand against the Puls containers (rebuilt: core changed) shows the handle and a continue.

## Concepts

- **Sessions and state between agents** (GOAL #9, #10): a specialist as a stateful collaborator rather than a function
  call; the handoff pattern "handle, then continue" against "repeat the context"; who owns the lifetime.
- **Cost of context** (GOAL #13): a follow-up as cache reads on a kept prefix against a cold start, measured in F25's run.
- **Limits in code, not in the prompt** (GOAL #15): caps and timeouts the model can't talk its way past.

## Approved during the build

*(none yet)*
