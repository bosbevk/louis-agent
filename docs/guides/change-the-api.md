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

Every API call needs `AGENT_API_KEY` as `x-api-key` (or `Authorization: Bearer`); without a key the API refuses to start
unless `AGENT_AUTH=off` ([FX18](../fixes/FX18-auth-fails-closed.md)).

## Add an endpoint

1. **Test first** where you can: a rule the endpoint keeps (a limit, a parse, a refusal) goes in a plain class with
   NUnit tests in `tests/louis-agent.api.tests`, mirroring the folder.
2. **Put it with its concern.** A new area gets its own `Endpoints/{Area}Endpoints.cs` with a `Map{Area}` extension on
   `WebApplication` (`HistoryEndpoints` is the model), called from `Program.cs` beside the others. Take what it needs
   from the container as parameters (`AgentEngine`, `AgentEngineFactory`, `AgentSessions`).
3. **Protect it.** The key check in `Program.cs` covers only the path prefixes it lists (`/sessions`, `/workspace`,
   `/git`, `/toolsets`, `/roles`, `/model`). **An endpoint under a new prefix is public until you add it there.** The
   admin endpoints have their own key (`AdminGate`).
4. **Keep the guards.** A path from a client goes through `WorkspaceBoundary`; a git call through
   `GitEndpoints.RunAsync`, with its arguments as a list, never a shell line
   ([ADR 0004](../adr/0004-arguments-as-data.md)).
5. **Errors and texts.** An error is `ApiErrors.Body(type, message)`, the shape Claude's API uses; every text is a
   member of `Messages/ApiUserText` (what a client sees) or `ApiLogMessage` (a log line)
   ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).
6. **Document it** in [How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-http-api-louis-agentapi) and the
   API's design doc.

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

`Sessions/TurnStream.cs` runs a turn and `Sessions/ClaudeStyleStream.cs` writes it as Claude-shaped events
(`message_start`, `content_block_start`, `_delta` and `_stop` for thinking, text, `tool_use` and `tool_result`,
`message_stop`). Three kinds of client read it: the web apps (`TurnStream` in web.shared), the orchestrator, and the benchmarks. Add a
new event or field without changing an existing one, test it, and update the stream's section in
[How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-event-stream).

## Serve the other web app

The API serves the code-review app by default and Puls when built with `-p:WebApp=puls` (`Dockerfile.puls` does).
Nothing in the API names the app beyond the csproj's two conditional references, so keep it that way.

## Test it

```bash
dotnet test tests/louis-agent.api.tests
```

The sessions run against a fake model client that is never called; the benchmark store's contract runs against the
in-memory store, and against MongoDB when `BENCHMARKS_MONGO_URL` is set (CI sets it). The endpoints themselves have no
automated tests yet, so check a new one with curl, with and without the key:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5080/your-endpoint                        # 401
curl -s -H "x-api-key: $AGENT_API_KEY" http://127.0.0.1:5080/your-endpoint                           # its answer
```

## Before you ask for a review

- [ ] A new prefix is in the key check, or is public on purpose and says why.
- [ ] Paths through `WorkspaceBoundary`, git arguments as a list.
- [ ] Errors in `ApiErrors`' shape; every text a message enum member.
- [ ] New session options validated before an engine is built.
- [ ] The stream only gained events or fields; the docs say what changed.

---
[Guides](README.md) · [Docs index](../README.md)
