# Change the API

> **How-to.** Change the HTTP host, `src/hosts/LouisAgent.Api`: add an endpoint, add a session option, change the event
> stream, serve the other web app, and test it. **Elsewhere:** how it works in
> [LouisAgent.Api](../projects/LouisAgent.Api.md); every endpoint and event in
> [How the agents and endpoints talk](../AGENT_COMMUNICATION.md).

![The API at a glance: LouisAgent.Api serves sessions with their streamed answers, the workspace and git routes, the admin routes and a site's pages, all under /v1 behind the key](diagrams/change-the-api.svg)

The API serves sessions over HTTP with their answers streamed as Server-Sent Events, the workspace and git endpoints,
the admin routes, and a site's pages. A program is start-up only. The routes are in `Endpoints/`, and a turn's stream
in `Sessions/`. Behaviour that every host needs belongs in core.

## Run it

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api   # http://127.0.0.1:5082
```

Every route is under `/v1` except `/health` ([ADR 0039](../adr/0039-api-routes-under-v1.md)). Every `/v1` call needs
`AGENT_API_KEY` as `x-api-key` (or `Authorization: Bearer`) unless the route is marked open. Without a key, the API
refuses to start, unless `AGENT_AUTH=off` says you mean it
([FX18](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX18-auth-fails-closed.md)).

## Add an endpoint

![Adding an endpoint: the test first, named by the rule, mapped with its concern, protected by the key, then the route golden written again and reviewed](diagrams/change-the-api-flow.svg)

1. **Test first.** A rule the endpoint keeps (a limit, a parse, a refusal) goes in a plain class with NUnit tests in
   `src/tests/hosts/LouisAgent.Api.Tests`, mirroring the folder; what the route answers (its status, its `Location`, its `401`)
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
   as the prices and the rates do. The admin endpoints have their own key, on the admin group (`AdminGroup`, `RouteAccess.AdminKey`).
   A host with a route of its own (louis-agent.dev keeps its CV) doesn't add it here: its program calls
   `app.MapLouisAgentHostGroup()` after `UseLouisAgentKey` and maps onto the group it gets, which is the same `/v1`
   with the same key check, so the host's routes are closed until opened, like the framework's own.
5. **Regenerate the route golden** (`LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/hosts/LouisAgent.Api.Tests --filter
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

1. Add the field to `CreateSessionRequest`, the contract the API and its clients share, in
   `src/engine/LouisAgent.SessionStream/Wire/Requests/`. Name it in JSON as the others are (a two-word name takes a
   `JsonPropertyName`, as `time_zone` does). The API reads it in `Sessions/Requests/CreateSessionRequestReading.cs`, with
   a test in `CreateSessionRequestTests` for its parsing and its limits.
2. Validate it before any engine is built, and refuse a bad value with a 400 and its reason, as an unknown role is.
3. Add it to `SessionRequest` in `Sessions/Opening/`, with its default, and set it where `SessionHandlers` builds
   the request. `SessionOpening` reads it in the step it belongs to, or `AgentSession` carries it; a test in
   `AgentSessionsTests` with the fake model client.
4. If it is a choice of a fixed set, it is an enum parsed once with `SettingChoice`
   ([ADR 0024](../adr/0024-choices-are-enums.md)). Add the field to `AgentApi.CreateSessionAsync` in
   `src/web/LouisAgent.Web` if the web apps send it.

## Change the event stream

`Sessions/TurnStream.cs` runs a turn, and `ITurnStreamFormat` (`Sessions/Streaming/`) writes it. Its one format is
`Sessions/Streaming/Claude/ClaudeStreamFormat.cs`, which writes Claude-shaped events: `message_start`;
`content_block_start`, `_delta` and `_stop` for thinking, text, `tool_use` and `tool_result`; `message_delta` with the
turn's usage; and `message_stop`. A client picks the format with `"stream"` on `POST /v1/sessions` (`StreamFormat`,
Claude by default), and the session keeps it. Three kinds of client read the stream: the web (through the
session-stream reader, `LouisAgent.SessionStream`), the orchestrator, and the evals.

To add a new event or field to the Claude format, add it without changing an existing one, test it, and regenerate
`src/tests/hosts/LouisAgent.Api.Tests/Sessions/StreamGolden.sse` with `LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/hosts/LouisAgent.Api.Tests --filter
StreamGoldenTests`. Read the golden's diff before you commit it. A new format is a new `StreamFormat` member and its
`ITurnStreamFormat` writer, tested beside `StreamGoldenTests`. Then update the stream's section in
[How the agents and endpoints talk](../AGENT_COMMUNICATION.md#the-event-stream).

## Serve another web app

The API serves whatever web app the program references, and nothing in the API names one, so keep it that way. A
program calls `AddLouisAgentApi`, `UseLouisAgentKey`, its own `MapStaticAssets` and `MapLouisAgentSite`, then
`MapLouisAgentApi`, which refuses to map without the key check.

`MapLouisAgentSite` takes the web app's assembly and the services its pages inject, and serves each page as HTML
(F121). A page that declares a `PageHead` is rendered once per process and served with its own title, description,
structured data, canonical link and words. Any other page gets the plain `index.html`.

## The whole program

This is the API's `Program.cs`, the one the coding agent's API runs (`demos/louis-agent/hosts/LouisAgent.CodingApi`),
complete, so a developer can paste it and change the catalogue and the web app for their own (FX96). The accounts and
the eval studio are the two optional lines; leave them out and the API runs without them.

```csharp
using LouisAgent.Core.Web;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Telemetry;
using LouisAgent.Accounts;
using LouisAgent.Accounts.Config;
using LouisAgent.Api;
using LouisAgent.Api.Site;
using LouisAgent.Api.Evals;
using LouisAgent.Coding;
using LouisAgent.Core.Hosting;

// The coding agent's API. It loads what this demo's catalogue names, and serves the code-review app, with the
// framework's accounts (F61) when ACCOUNTS_SQL_URL is set.
var host = AgentHost.Build(CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
    Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
var builder = WebApplication.CreateBuilder(args);
builder.AddLouisAgentApi(host);
builder.AddLouisAgentAccounts(AccountsOptions.FromEnvironment());   // optional: sign-in, tenants (F61)
var app = builder.Build();

// The accounts first (the tenant from the host, who the cookie says), then the key check, which takes a signed-in
// person in place of the key. The agent can run shell commands and write files, so the key check comes before any
// route.
app.UseLouisAgentAccounts();                                         // optional, with the line above
app.UseLouisAgentKey();

// The code-review app's files, and any path that isn't an API route loads it, such as /chat/{sessionId}. Its pages are
// the chat and the admin, none meant for search, so each answers the plain shell; the framework's site has its own
// host since F124.
app.MapStaticAssets();
app.MapLouisAgentSite(typeof(LouisAgent.CodeReview.App).Assembly, services =>
{
    services.AddSingleton(new LouisAgent.Web.Text.ChatText());
    services.AddSingleton(new HttpClient());
});

app.MapLouisAgentAccounts();                                         // optional, with the two above
app.MapLouisAgentApi(host);
app.MapLouisAgentEvals(host);                                        // optional: the eval studio
app.Run();
```

It runs with `AGENT_API_KEY` set (or `AGENT_AUTH=off` for a local try), a model's key (`ANTHROPIC_API_KEY`, or Ollama
with none) and the paths in the host's run profile; the rest has defaults.

## Test it

```bash
dotnet test src/tests/hosts/LouisAgent.Api.Tests
```

The sessions run against a fake model client that is never called; the benchmark store's contract runs against the
in-memory store, and against MongoDB when `BENCHMARKS_MONGO_URL` is set (CI sets it). The routes run in memory
(`ApiTestHost`): the table is pinned by `ApiRoutesSnapshot.json`, the key gate is tried on every route
(`AgentKeyGateTests`), and the git routes run in a temporary repository (`GitResourceTests`, in
`LouisAgent.Api.IntegrationTests`). A streamed turn needs
the model, so check one by hand, and check a new route on the running stack with and without the key:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5082/v1/your-endpoint                     # 401
curl -s -H "x-api-key: $AGENT_API_KEY" http://127.0.0.1:5082/v1/your-endpoint                        # its answer
```

## Add an eval to the portal

The admin's eval pages are served by the eval studio's API, `LouisAgent.Api.Evals`, which a program maps with
`MapLouisAgentEvals`. They list the evals the runner lists, so a new eval needs no change in the API or the pages. The runner's
`--list` gives each eval its name, identity, version and `proves` line, and the portal shows the `proves` as the check's
title. The eval's name is its check's id.

1. Write the eval as a folder of files in its demo's evals project (for the triage team,
   `demos/louis-agent/evals/LouisAgent.Identities.Triage.Evals/Evals/`), with its scenarios, checks and reference
   transcripts.
   [Add an eval](add-an-eval.md) walks through it.
2. Check the runner lists it: `dotnet src/evals/LouisAgent.Evals.Runner/bin/Debug/net10.0/LouisAgent.Evals.Runner.dll --list`
   prints one line for it (build the runner first).
3. Rebuild the image that serves the portal. The runner is in the image, so the new eval comes with the rebuild:
   `docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api`.
4. Open the admin's eval pages: the check is listed with its `proves` as its title. Its manual is the markdown file the
   eval's `Manual` names, beside the eval, which the runner's listing hands to the API. A manual outside the eval
   folders is never read.

A run starts with `POST /v1/admin/evals/{check}/runs`. Its body takes `repeat`, and for an eval `web` (`record` or `off`) or
`replay` (a recorded run's id, with the model's answers: the runner's `--replay`), not both.
`EvalRunOptions` parses it once, `EvalProcess.StartInfoFor` turns it into the runner's arguments, and the run's settings keep
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
