# LouisAgent.Api.Evals — design

> **Kind:** library (`net10.0`) · **Path:** `src/hosts/LouisAgent.Api.Evals/` · **References:** LouisAgent.Evals,
> LouisAgent.Api · **Used by:** LouisAgent.SiteApi, LouisAgent.SiteFrameworkApi, the demo's LouisAgent.CodingApi, and their tests
> **Also shows:** `component`

![What a site's API takes for its agents' evals: its routes behind the admin key, a desk that runs one run at a time, the work that plays a run, and its settings](diagrams/LouisAgent.Api.Evals.svg)

## Purpose

This project is how a site runs the evals of its own agents. A site's API program takes it with two lines, and from
then on someone with the admin key can start a run of an eval, follow it while it plays, stop it, read it once it
has ended, correct the judge on a mark, grade it again, and copy a conversation back to its file, all over the site's
own API. The playing and grading are [LouisAgent.Evals](LouisAgent.Evals.md)'s; this
project serves them, one run at a time, in the background of the host.

The doc follows a run from the start: how a site takes the evals, how a start is checked, how the run goes on after
the answer, how a person stops it, how it reaches the agent, what a person does with a run that has ended, and what
its settings are.

## How it works

![The eval library a site's API maps: a run asks the agent over the sessions API like any app, keeps its file in the data folder, and its recordings are the engine's](diagrams/LouisAgent.Api.Evals-component.svg)

### 1. A site takes the evals

A site's program calls `AddLouisAgentEvals` among its services and `MapLouisAgentEvals` on the API's group, beside
its own routes. The first reads the host's eval files from its catalogue there and then, so a wrong one stops the
host at start-up with its reason, and it registers the run desk and the routes' handlers. The second maps the
routes, all behind the admin key:

| Route | What it answers |
|---|---|
| `GET /v1/admin/evals` | Every eval this host has: its name, its agent, how many conversations, its last run in short, the run that one is compared with, and its last live run that finished, whose costs a page estimates the next one from |
| `POST /v1/admin/evals/{name}/runs` | Starts a run, from a body of its kind (`live` or `replay`) and its repeats (1 or 3): `202` with the run's address in `Location` |
| `GET /v1/admin/evals/runs/{id}` | The run as it is kept, running or ended |
| `DELETE /v1/admin/evals/runs/{id}` | Stops the run going: `204`, then the run keeps itself as stopped. A run this host doesn't have is `404`, one that isn't going `409` |
| `POST /v1/admin/evals/runs/{id}/regrade` | Grades a kept run's answers again against the eval file as it is now: `202` with the new run's address |
| `PUT /v1/admin/evals/runs/{id}/{conversation}/{repeat}/{turn}/criteria/{criterion}/flip` | Flips a criterion's mark, with a body of a one-line note or none: `204` |
| `DELETE` at the same address | Puts the judge's answer back: `204` |
| `GET /v1/admin/evals/runs/{id}/{conversation}/as-file` | The conversation as a block for its eval file, with the page the file gives it |
| `GET /v1/admin/evals/{name}/runs` | That eval's runs in short, the newest first, each with its criteria missed or flipped |

Every answer is the run's own JSON, so what the API sends is what the run's file holds. A run in short carries what
it cost the agent and the judge, and their currency, so a list needs no second read to price it.

![Its classes: the startup registers the desk and the handlers, a handler checks a start and hands the desk the work, which plays the run and keeps it](diagrams/LouisAgent.Api.Evals-classes.svg)

### 2. A start is checked first

`EvalHandlers` asks every reason to refuse a start before anything runs, one guard after another, each with its own
answer. An eval this host doesn't have is a `404`. A body that doesn't read, repeats other than 1 or 3, a host with
no data folder, a live run on a host that keeps no recordings, or a replay with no live run of that eval that
finished, is each a `400` that says why. A run already going is a `409`. A replay plays the live run's repeats back,
as many as it recorded, whatever the body asked.

A run's id is its start time to the second and the eval's name. A start in a second another run of that eval started
in takes the next second, so an id names one run.

![A start, step by step: every refusal asked first, then the desk's one slot, then 202 with the run's address](diagrams/LouisAgent.Api.Evals-flow.svg)

### 3. The run goes on after the answer

The start answers `202` at once, and the run plays on in the same process. That is Microsoft's Asynchronous
Request-Reply, with one thing left out: the run's address keeps answering the run once it has ended, since running
and ended are the same record.

`RunDesk` holds the one run that is going. It is a hosted service, Microsoft's `BackgroundService`: a start takes its
one slot under a lock and hands it the run's work, so two starts at the same moment can't both win, and the host
waits for the run when it shuts down. A failure inside the work is logged, and the slot is free again whatever
happened. A run cut off by a shutdown stays running in its file, and the desk reads a stored run that says running
and isn't its own as could not finish, so no clean-up job is needed.

### 4. A person stops a run

A run can be stopped while it plays, and the stop takes effect at once. The `DELETE` route asks the desk to stop the
run by its id. The desk holds a cancellation source for the one run going, and `TryStop` cancels it under the same
lock a start takes, so a stop can't land between a run ending and the next one starting. It answers whether that
run was the one going.

The run's work gets two signals, as `RunSignals`. `Stopped` is a person's stop alone. `Playing` joins it with the
host's shutdown, and that is what the runner plays under. So when the play is cancelled, the work can tell why. On
a person's stop it keeps the run as stopped, with everything graded before the stop and the reason "A person stopped
the run". On a shutdown it leaves the file as it was, running, which the desk reads as could not finish when the
host comes back, as above.

The conversation in play is cut off part way, so its turn costs what it had spent and no more. The session the play
opened is still closed: reading the answer to the session's creation ignores the stop, so a session created at the
moment of the stop is known and deleted, never left open. A stopped run is never the run a replay plays back, nor an
eval's last live run, since both must have finished.

![A run started and followed: the start is answered at once, the run plays on in the background and is kept after each conversation, and its address answers it](diagrams/LouisAgent.Api.Evals-sequence.svg)

### 5. A run reaches the agent as any app does

`RunWork` is one run's work. It saves the run as it starts, then plays it with the evals' runner, keeping it in its
file after every conversation, and keeps it as could not finish with the reason when something fails part way. It
asks the agent over HTTP at the program's own address, the port it listens on at the loopback address, with the
instance's own key, so the engine and the API hold no eval code and the agent that is measured is the one visitors
get. Each ask may take ten minutes before its answer starts.

An agent the open door lets visitors reach, as the site's guide, is reached the same way. The door tells an
operator's run, a request with the instance's key that names a run, from a visitor's: it gets the identity exactly
as a visitor does, its own toolsets and nothing the body asks for, but keeps its run tag and its recording, isn't
counted against the day's cap, and isn't tagged as a visitor's in the ledger
([LouisAgent.Api](LouisAgent.Api.md)).

The judge's client is `EvalsStartup.JudgeClient`: the host's provider on the judge's model, then the judge's replay
when the host keeps recordings, then the host's usage ledger, so a live mark is priced under the grading purpose and
a replayed one is a known 0. Its rows carry the run's id and the conversation's task, as the agent's do, so the ledger
alone can say what judging one run cost.

*Decided in:* F126.3.1, F126.3.4, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

### 6. A run that has ended: flipped, re-graded, copied

A person can correct the judge on a criterion's mark, at the mark's own address: the turn and the criterion count
from 1, as the pages show them. `ConversationHandlers` asks every refusal first: `404` for a run, a conversation, a
repeat, a turn or a criterion the run doesn't have, `409` while the run is the one going, since its file is still
being written, and `400` for a body that doesn't read or a note of two lines or over 200 characters. The store then
changes the run in its file under its lock (`RunStore.Change`), reading and saving as one step, so two flips sent at
once on two marks of the same run both stay.

A kept run can be re-graded. The route makes a run of the kind `re-grade`, its id from the store's `NextId` as a
start's is, naming the run it re-graded and the live run its answers came from: a re-grade of a replay or of a
re-grade names the same live run, so the judge's new answers go into that run's recording. It goes through the desk
like any run, one at a time, and Stop stops it. Its work, `RunWork.ForRegrade`, is kept, stopped and failed by the
same `KeepAsync` as a run's, and asks no agent. It is refused with `400` on a host with no data folder, `404` for a
run the host doesn't have or one whose eval is gone, and `409` while any run goes. It starts with no ask, since it
asks the judge only for what changed and the run says after what it spent.

A run's conversation is answered as a block for its eval file, to copy and change: its asks, facts and criteria from
its first repeat, written by the file's own options, with the page the eval file still gives it, since a run
doesn't keep it.

*Decided in:* F126.4, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

```
src/hosts/LouisAgent.Api.Evals/
├── EvalsStartup.cs     # AddLouisAgentEvals, MapLouisAgentEvals, the judge's client
├── EvalHandlers.cs     # the evals' and their runs' routes, a start's and a re-grade's refusals first
├── RunDesk.cs          # the one run going, as a hosted service, and its stop
├── RunSignals.cs       # a person's stop, and the stop the play runs under
├── RunWork.cs          # one run played over the sessions API, or re-graded, and kept
├── Conversations/      # ConversationHandlers, FlipRequest: a mark's flip, and a conversation as a block
├── EvalsOptions.cs     # the settings, read once
└── Messages/           # EvalsApiMessage and its Holes/
src/tests/hosts/LouisAgent.Api.Evals.Tests/     # the routes, the desk, the judge's replay, a replayed run
src/tests/hosts/LouisAgent.Api.Evals.Testing/   # EvalReplay: an eval replayed from recordings, as one test
```

## Configuration

`EvalsOptions` reads these once, when the host starts. A blank one is none.

| Setting | What it does | Without it |
|---|---|---|
| `SITE_DATA_DIRECTORY` | The site's data folder; runs are kept in `evals/runs/` under it | A start is refused: nowhere to keep a run |
| `EVALS_JUDGE_MODEL` | The judge's model, on the host's provider | The host's own model |
| `EVALS_API_URL` | Where the agent is asked | The program's own address |
| `AGENT_API_KEY` | The instance's key, sent with every ask | No key is sent |

A live run also needs the engine's replay folder, `REPLAY_DIRECTORY`, so every live run can be replayed; the site's
compose file and its Azure app set it to `/data/recordings`. Nothing is recorded unless a session asks to, and a
visitor's never does.

## Tests

`LouisAgent.Api.Evals.Tests` runs the API in memory with the evals mapped, a test identity whose folder holds an
eval, a fake model for the agent and one for the judge. `EvalRoutesTests` holds the routes: the admin key, the
list, a live run started and read until it ends, a second start refused while one runs, each refusal, and a going
run stopped with `204` and kept as stopped, a stop of a run that isn't going refused. `RunDeskTests` holds a cut-off
run read as could not finish, and a stop that cancels the going run's work. `Conversations/FlipRoutesTests` holds a
flip and its undo, every refusal and two flips at once, and `AsFileRouteTests` the block; `Regrades/RegradeRouteTests`
holds a re-grade, its refusals, its stop, and a replay that plays again after a criterion was reworded. `JudgeReplayTests` proves the engine's replay client
serves the judge's typed call, and `ReplayRunTests` holds a live run recording and a replay giving the same marks
with no model called, or stopping when its recording is out of date.

`LouisAgent.Api.Evals.Testing` holds `EvalReplay`, which a site's tests use to replay an eval from recordings kept
in git, on the site's own API in memory, facts only, with no key. It replays each conversation its recordings hold
and answers their ids, so a test says which it expects and an eval that grew past its recordings can't pass by
replaying nothing. The guide's is
`GuideEvalReplayTests` in `LouisAgent.Site.Tests`.

## Limits and plans

One run goes at a time, and nothing starts one on a schedule. The admin's pages start, follow and stop a run
([LouisAgent.Web.Evals](LouisAgent.Web.Evals.md)), and the routes stay open to `curl` with the admin key. The
project is not a package ([ADR 0130](../adr/0130-the-framework-is-published-as-packages-one-tag-each.md)).

## Related docs

[LouisAgent.Evals](LouisAgent.Evals.md) · [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) ·
[LouisAgent.Api](LouisAgent.Api.md) · [Run an eval](../guides/run-an-eval.md) ·
[ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md) · [Test projects](tests.md)

---
[Projects](README.md) · Previous: [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) · Next: [Test projects](tests.md)
