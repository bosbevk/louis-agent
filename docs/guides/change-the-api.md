# Change the API

> **How-to.** Change the HTTP host, `src/louis-agent.api`: add an endpoint, add a session option, change the event
> stream, serve the other web app, and test it. **Elsewhere:** how it works in
> [louis-agent.api](../projects/louis-agent.api.md); every endpoint and event in
> [How the agents and endpoints talk](../AGENT_COMMUNICATION.md).

The API serves sessions over HTTP with their answers streamed as Server-Sent Events, the workspace and git endpoints,
the admin portal, and a web app as static files. `Program.cs` is start-up only; the routes are in `Endpoints/`, a turn's
stream in `Sessions/`. Behaviour that every host needs belongs in core.

## Run it

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api   # http://127.0.0.1:5080
dotnet run --project src/louis-agent.api --launch-profile puls --property:WebApp=puls  # Puls's settings and web app
```

Every route is under `/v1` but `/health` ([ADR 0039](../adr/0039-api-routes-under-v1.md)), and every `/v1` call needs
`AGENT_API_KEY` as `x-api-key` (or `Authorization: Bearer`) unless the route is marked open; without a key the API
refuses to start unless `AGENT_AUTH=off` ([FX18](../fixes/FX18-auth-fails-closed.md)).

## Add an endpoint

1. **Test first.** A rule the endpoint keeps (a limit, a parse, a refusal) goes in a plain class with NUnit tests in
   `tests/louis-agent.api.tests`, mirroring the folder; what the route answers (its status, its `Location`, its `401`)
   is tested in memory with `ApiTestHost`, as `SessionCreatedTests` does.
2. **Name it by the rule** ([ADR 0039](../adr/0039-api-routes-under-v1.md)): a noun, one plural name for a collection
   and its items (`/v1/git/branches`, `/v1/git/branches/{name}`); a create is a `POST` to the collection that answers
   `201` with `Location` (`Results.Created`, the path built from `ApiMap.Version`); a delete is `DELETE`; an action no
   method names is `POST /v1/{resource}/{verb}`, the verb last.
3. **Put it with its concern.** A new area gets its own `Endpoints/{Area}Endpoints.cs` (or a folder, as `Endpoints/Git/`
   is) with a `Map{Area}` extension on `IEndpointRouteBuilder` (`GitBranchesEndpoints` is the model), called from
   `ApiMap.MapApi` on the `v1` group beside the others, never from `Program.cs`. Its paths leave the version out
   (`"/git/branches"`): the group adds it. Take what it needs from the container as parameters (`AgentEngine`,
   `AgentEngineFactory`, `AgentSessions`).
4. **Protect it.** A route under `/v1` needs the key unless the code marks it otherwise, so a new one is closed until
   you open it. Mark it `.WithAccess(RouteAccess.Open)` only when it holds nothing private, and say why in a comment,
   as the prices and the rates do. The admin endpoints have their own key (`AdminGate`, `RouteAccess.AdminKey`).
5. **Regenerate the route golden** (`LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.api.tests --filter
   ApiRoutesSnapshotTests`) and review its diff: one line per route, with its method and who may call it.
   `ApiRouteRuleTests` checks the rule on the real table.
6. **Keep the guards.** A path from a client goes through `WorkspaceBoundary`; a git call through
   `GitProcess.RunAsync`, with its arguments as a list, never a shell line
   ([ADR 0004](../adr/0004-arguments-as-data.md)).
7. **Errors and texts.** An error is `ApiErrors.Body(type, message)`, the shape Claude's API uses; every text is a
   member of `Messages/ApiUserText` (what a client sees), `ApiLogMessage` (a log line) or `ApiErrorMessage` (an exception's text)
   ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).
8. **Document it** in [How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-http-api-louis-agentapi) and the
   API's design doc. A client names the whole route, version included (`"v1/git/branches"`).

## Add a session option

A session's options are fixed when it opens, as its toolsets are, so its prompt stays cacheable
([ADR 0006](../adr/0006-toolsets-per-session.md)).

1. Add the field to `Sessions/Requests/CreateSessionRequest.cs`, named in JSON as the others are (a two-word name
   takes a `JsonPropertyName`, as `time_zone` does), with a test in `CreateSessionRequestTests` for its parsing and its
   limits.
2. Validate it before any engine is built, and refuse a bad value with a 400 and its reason, as an unknown role is.
3. Pass it through `SessionEndpoints` to `AgentSessions`, which opens the session with it; a test in
   `AgentSessionsTests` with the fake model client.
4. If it is a choice of a fixed set, it is an enum parsed once with `SettingChoice`
   ([ADR 0024](../adr/0024-choices-are-enums.md)). Add the field to `AgentApi.CreateSessionAsync` in
   `src/louis-agent.web.shared` if the web apps send it.

## Change the event stream

`Sessions/TurnStream.cs` runs a turn, and `ITurnStreamFormat` (`Sessions/Streaming/`) writes it. Its one format is
`Sessions/Streaming/Claude/ClaudeStreamFormat.cs`: Claude-shaped events (`message_start`, `content_block_start`, `_delta` and `_stop`
for thinking, text, `tool_use` and `tool_result`, `message_delta` with the turn's usage, `message_stop`). A client picks the format
with `"stream"` on `POST /v1/sessions` (`StreamFormat`; Claude by default), and the session keeps it. Three kinds of client read the
stream: the web apps (through the session-stream reader, `louis-agent.session-stream`), the orchestrator, and the benchmarks.

To add a new event or field to the Claude format, add it without changing an existing one, test it, and regenerate
`tests/louis-agent.api.tests/Sessions/StreamGolden.sse` with `LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.api.tests --filter
StreamGoldenTests`. Read the golden's diff before you commit it. A new format is a new `StreamFormat` member and its
`ITurnStreamFormat` writer, tested beside `StreamGoldenTests`. Then update the stream's section in
[How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-event-stream).

## Serve the other web app

The API serves the code-review app by default and Puls when built with `-p:WebApp=puls` (`Dockerfile.puls` does).
Nothing in the API names the app beyond the csproj's two conditional references, so keep it that way.

## Test it

```bash
dotnet test tests/louis-agent.api.tests
```

The sessions run against a fake model client that is never called; the benchmark store's contract runs against the
in-memory store, and against MongoDB when `BENCHMARKS_MONGO_URL` is set (CI sets it). The routes run in memory
(`ApiTestHost`): the table is pinned by `ApiRoutesSnapshot.json`, the key gate is tried on every route
(`AgentKeyGateTests`), and the git routes run in a temporary repository (`GitResourceTests`). A streamed turn needs
the model, so check one by hand, and check a new route on the running stack with and without the key:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5080/v1/your-endpoint                     # 401
curl -s -H "x-api-key: $AGENT_API_KEY" http://127.0.0.1:5080/v1/your-endpoint                        # its answer
```

## Add an eval to the portal

The portal lists the C# evals the runner lists, so a new eval needs no change in the API or the portal. The runner's
`--list` gives each eval its name, identity, version and `proves` line, and the portal shows the `proves` as the check's
title. The eval's name is its check's id; a Python check with the same id is hidden.

1. Write the eval in its eval set's project (`evals/louis-agent.evals.{identity}/`), with its scenarios, graders and
   reference transcripts; [Add an eval set](add-an-eval-set.md) walks through it.
2. Check the runner lists it: `dotnet evals/louis-agent.evals.runner/bin/Debug/net10.0/louis-agent.evals.runner.dll --list`
   prints one line for it (build the runner first).
3. Rebuild the image that serves the portal. The runner is in the image, so the new eval comes with the rebuild:
   `docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build`.
4. Open the admin portal: the check is listed with its `proves` as its title. Its manual is the file the eval's `Manual`
   names (`evals/{project}/Manuals/{name}.md`, F18 step 7.1), which the runner's listing hands to the API.

A run starts with `POST /v1/admin/benchmarks/{check}/runs`. Its body takes `repeat`, and for an eval `web` (`record` or `off`) or
`replay` (a recorded run's id, with the model's answers: the runner's `--replay`), not both; a Python check answers 400 to either.
`BenchmarkRunOptions` parses it once, `BenchmarkRunner.StartInfoFor` turns it into the runner's arguments, and the run's settings keep
`web` or `replay_of`. A portal run is always the dev scenarios, and a replay is no point of the check's trend. [Run the evals](run-the-evals.md)
says when to record and when to replay.

## Before you ask for a review

- [ ] Every new route is under `/v1`, named by the rule, and needs the key, or is marked open on purpose and says why.
- [ ] The route golden regenerated and its diff reviewed.
- [ ] Paths through `WorkspaceBoundary`, git arguments as a list.
- [ ] Errors in `ApiErrors`' shape; every text a message enum member.
- [ ] New session options validated before an engine is built.
- [ ] The stream only gained events or fields; the docs say what changed.

---
[Guides](README.md) · [Docs index](../README.md)
