# Run an eval

> **How-to.** Start a run of a site's eval from its admin or through its API, follow it, stop it, read its file,
> replay it for free, correct the judge on a mark, re-grade a run, copy a conversation into the eval file, and what to
> do when a replay says its recording is out of date. **Elsewhere:** how a run works in
> [LouisAgent.Evals](../projects/LouisAgent.Evals.md), the routes and their settings in
> [LouisAgent.Api.Evals](../projects/LouisAgent.Api.Evals.md), what an eval is in
> [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

![Run an eval: start it, follow it at its address, read its file, and replay it for free](diagrams/run-an-eval.svg)

A run plays every conversation of an eval file against the site's agent, the way a visitor would ask it, and grades
each turn. A live run calls the agent's model and the judge's, so it spends money: on louis-agent.dev it waits for
Louis's go. A replay plays the last live run back from its recording, calls no model and costs nothing.

These steps use louis-agent.dev's review copy on this PC, on `http://127.0.0.1:5080`. Another site's API works the
same way at its own address.

## Before you start

The site's API must keep runs and recordings. Its compose file sets both: `SITE_DATA_DIRECTORY` to `/data`, which is
`src/site/data/` on this PC, and `REPLAY_DIRECTORY` to `/data/recordings`. Without a data folder a start is refused,
and without recordings a live start is too. Start the container as
[Change the site](change-the-site.md) says, with the model's key in `src/site/config/.env.secrets`.

A conversation may name a page, as the guide's does with `"page": "/"`. Its first ask then carries that page as a
visitor's message would, read from the site's own `GET /v1/site/page?address=`, so the host must serve the site.
A page the site doesn't have stops the run as could not finish, with its address.

Every eval route asks for the admin key, `ADMIN_API_KEY` in `.env.secrets` or `AGENT_API_KEY` when there is none.
Read it into your shell without printing it:

```bash
KEY=$(grep -E '^ADMIN_API_KEY=' src/site/config/.env.secrets | cut -d= -f2-)
```

## In the admin

On a site that names `.Evals()` among its areas, as louis-agent.dev does, the admin does all of it with the key you
already gave it.

![In the admin: a replay starts from the card at once, a live run after its ask, and the run's page follows it until it ends or is stopped](diagrams/run-an-eval-admin.svg)

1. Open **Evals** in the admin's sidebar, at `/admin/evals`. Each eval is a card with its last run's number first.
2. To replay for free, press **Replay free** on the eval's card. It is there once the eval has a live run that
   finished.
3. To run live, press **Run live…**, pick one repeat or three, and read the estimate, which comes from what the last
   live run cost. Press **Run live, about $…** to start it, or **Don't run**. On louis-agent.dev that waits for
   Louis's go.
4. Either way the browser opens the run's page. It reads the run again every few seconds while it goes, and its
   figures say how long it has gone and what the agent and the judge have cost so far.
5. To stop it, press **Stop the run**. The run ends as stopped and keeps what it graded before the stop.
6. Read it. A run's page lists its conversations failed first, with the first miss of each, and the eval's other
   runs at its foot. A conversation opens at its first failed repeat, turn by turn, with every tool call, answer,
   mark and cost, and walks to the next failed one or to the same conversation in the run before. The run's head
   says how it went against the run before, and **The last 8 runs** shows the trend and the criteria missed most.
7. To correct the judge, press **Flip** on a criterion's mark on a conversation's page, write why in one line if you
   want, and press **Flip to met** (or **Flip to not met**). Your answer counts in every number from then on, and
   **Undo the flip** puts the judge's answer back.
8. To grade a run again after you changed the eval file, press **Re-grade** on its page. No agent is asked: facts
   are checked again, unchanged marks are kept with your flips, and the judge is asked only for a criterion that is
   new or reworded. The new run's page says what changed and what it cost.
9. To turn a conversation into a test case, press **Copy as conversation** on its page, paste the block into the
   eval file's `conversations`, and change it.

## Through the API

The same runs, with `curl`, from a terminal or a script.

![The steps: a live run, read until it ends, its file, then a replay, and a live run again when the replay is out of date](diagrams/run-an-eval-flow.svg)

1. See the site's evals, each with its agent and its last run:

   ```bash
   curl -s http://127.0.0.1:5080/v1/admin/evals -H "x-api-key: $KEY"
   ```

2. Start a live run of one, with one repeat or three. The answer is `202` with the run's address in `Location`:

   ```bash
   curl -s -i -X POST http://127.0.0.1:5080/v1/admin/evals/guide/runs -H "x-api-key: $KEY" \
     -H "content-type: application/json" -d '{"kind":"live","repeats":3}'
   ```

3. Read that address until the run's `status` is no longer `running`. It ends as `finished`, as `stopped` when a
   person stopped it, or as `could-not-finish`, each of the last two with its reason in `stoppedBecause`:

   ```bash
   curl -s http://127.0.0.1:5080/v1/admin/evals/runs/20261123-063633-guide -H "x-api-key: $KEY"
   ```

4. Read the run's file, the same record: `src/site/data/evals/runs/{id}.json`. Each conversation has its repeats,
   each repeat its turns, and each turn what was asked, the answer, every tool call, what it cost the agent and the
   judge, and a mark with its reason for each fact and each criterion. `passed` on a conversation is true only when every repeat passed.

5. To stop a run while it goes, delete its address. The answer is `204`, and the run then keeps itself as
   `stopped`. A run that has ended answers `409`, and one the host doesn't have `404`:

   ```bash
   curl -s -i -X DELETE http://127.0.0.1:5080/v1/admin/evals/runs/20261123-063633-guide -H "x-api-key: $KEY"
   ```

6. Flip a criterion's mark, with a one-line note or none, at the mark's own address; the turn and the criterion
   count from 1. The answer is `204`. Delete the same address to put the judge's answer back:

   ```bash
   curl -s -i -X PUT http://127.0.0.1:5080/v1/admin/evals/runs/20261123-063633-guide/guide-what-is/1/1/criteria/2/flip \
     -H "x-api-key: $KEY" -H "content-type: application/json" -d '{"note":"It does say so."}'
   ```

7. Re-grade a kept run, or read one of its conversations as a block for the eval file:

   ```bash
   curl -s -i -X POST http://127.0.0.1:5080/v1/admin/evals/runs/20261123-063633-guide/regrade -H "x-api-key: $KEY"
   curl -s http://127.0.0.1:5080/v1/admin/evals/runs/20261123-063633-guide/guide-what-is/as-file -H "x-api-key: $KEY"
   ```

8. Replay it. The replay plays the eval's last live run that finished, as many repeats as it recorded, and ends as
   a run of its own with `replayOf` naming the live one. A page a conversation names is sent as the live run kept
   it, so the page route isn't asked:

   ```bash
   curl -s -i -X POST http://127.0.0.1:5080/v1/admin/evals/guide/runs -H "x-api-key: $KEY" \
     -H "content-type: application/json" -d '{"kind":"replay","repeats":1}'
   ```

## Give a conversation a page that isn't on the site

To hand the agent a page of your own, such as one with instructions hidden in it, give the page in the eval file
instead of naming its address. It is sent with the first ask as a visitor's page would be:

```json
{ "id": "guide-injection",
  "givenPage": { "url": "/news/a-customer-story", "title": "A customer story", "text": "The page's words." },
  "turns": [ { "ask": "What does this page say?", "facts": ["not-used SendLead"] } ] }
```

A conversation names its page or gives it, never both.

## When a replay says its recording is out of date

A recording answers only the requests it was made from. When the agent's prompt, a tool, a doc it read or a
criterion's words change, the replay ends as `could-not-finish`, saying the recording no longer answers, so it is out
of date and a live run is needed, and what changed first. Nothing is wrong with the run: start a live run again, on
Louis's go, and replay that one. When only a criterion's words changed, re-grade the live run instead: it asks
the judge for the reworded criterion alone and adds its answer to the live run's recording, so Replay free plays
again.

The guide's eval is also one test, `GuideEvalReplayTests`, which replays every conversation's first repeat of a
live run from `src/site/recordings/evals/` with no key, over an empty docs folder, since what the tools answered
is played back too. When it turns red after a change to the guide, copy a new live run's first repeats there: each trial's
file from `src/site/data/recordings/trials/` whose name starts with the run's id and ends in `_1-…`, and each file
in `src/site/data/recordings/answers/` that their lines name by their `hash`. Delete the
old run's files, read every new file before you stage it, since the repository is public, and run the test again.

## Check

- The run's file is in `src/site/data/evals/runs/`, and its address answers the same run.
- A replay of a finished live run gives the same marks, and the usage ledger shows its calls at 0.
- `dotnet test src/tests/site/LouisAgent.Site.Tests --filter "FullyQualifiedName~GuideEvalReplayTests"` passes with
  no key set.

---
[Guides](README.md) · [Docs index](../README.md) · Related: [LouisAgent.Api.Evals](../projects/LouisAgent.Api.Evals.md) ·
[LouisAgent.Evals](../projects/LouisAgent.Evals.md) · [LouisAgent.Web.Evals](../projects/LouisAgent.Web.Evals.md)
