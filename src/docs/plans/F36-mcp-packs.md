# Draft: F36 MCP servers as packs (Phase 8)

> **Plan draft, not yet approved.** Moved whole out of the old platform plan on 2026-11-07 (CL56), lines 3215 to 3400 of `platform.md` at the tag `pre-roadmap`; the body below is as it was. When the work starts, this is rewritten into the plan Louis approves (the development guide, rule 3) and its TODO row names it. The sub-feature split of 2026-10-28 is the TODO's rows.

### Phase 8 · F36 Their systems, no code: MCP servers as packs

Planned to the feature here: before its first step, its own plan, `docs/plans/F36-mcp-packs.md` on
`feature/F36-mcp-packs`, is written and approved by Louis, with its four diagrams, namespace map and folder trees; its
first step writes F36's feature doc (none exists yet, checked). GOAL skill 11 (*a full MCP client*, *Missing* today,
`docs/GOAL.md:32`). Paid runs: none. The MCP simulation runs free on a local model (Ollama); one paid run only if the
local model can't drive the tools, on Louis's go. It starts after Phase 7, and needs three things from it: FX4, so a
chat keeps the tool list it opened with; `ToolEffect` and F41's policy, so an orange tool's effect decides whether it
asks; and F42's frame and egress proxy, for an HTTP server's results and traffic. It removes known limitation 1
(`TODO.md:382`), and it is done only when Louis has written its learning write-up himself (part 11).

**Why it waits for Phase 8.** The challenge recommended F36 right after the cut. It waits for Phase 8 because Louis said
MCP only when needed ("if needed, we are [alone] in this"; "we [don't] have to do mcp's now, I just want it to [be]
pluggable into the mcp with almost no [code]"). Until then, Phase 2 keeps every pack convertible to MCP (ADR 0028, the
convertibility test), so this phase plugs servers in without changing a pack.

**What it builds.** An `McpPack` on the official ModelContextProtocol SDK's client (2.2.0, Apache-2.0; the same SDK our
MCP server already uses) turns someone else's MCP server into a one-toolset pack with the same name, so an identity that
names it can't tell the difference (ADR 0028: MCP plugs in at the toolset). It is an orange pack: their code, with their
own security and data layer, in another process. A server is named by an identity or by `MCP_PACKS`
(`MCP_PACKS=pricerules` reads `config/mcp/pricerules.json`: the transport, the command or URL, headers by the names of
the settings that hold them, the tools allowlist, the prefix, whether it is trusted, each tool's effect, and the pin;
and its guide, `config/mcp/pricerules.md`). These names are proposals for F36's plan to settle; Part 3's *Row 23 in
detail* holds the corrections they need. The two additive lines in core that an MCP pack needs are already there,
because Phase 6's A5 brought them forward for its result tools: the engine adds a ready `AITool` as it is (beside
`louis-agent.core/Agent/AgentEngine.cs:146`, where it adds each toolset's public methods today), and
`ToolsetDefinition` has an optional list of a toolset's ready tools, which `AgentCatalog.NamesOf` reads for its tool
names. F36 fills that list with the tools a server listed and changes no core contract. Each tool is wrapped in our
guard, `McpToolGuard` (over Microsoft.Extensions.AI's `DelegatingAIFunction`): results framed as untrusted (Phase 7's
frame) unless the pack is marked trusted, oversized results summarised, failures with their reasons, and a timeout per
call, because the SDK has no timeout per request.

**What the composition and the guide need.** `Plugins.FromSettings` stays synchronous until here (Phase 2: nothing to
await yet). F36 adds a second, async source of packs, `Plugins.LoadAsync(IDENTITIES, PACKS, MCP_PACKS)`, which connects
every named server before the catalogue is built and owns the connections. Each host holds what it returns with
`await using`, so a stdio child ends when the host stops. The CLI, ACP and MCP hosts already await
(`src/louis-agent.cli/Program.cs:9,13`, `src/louis-agent.acp/Program.cs:11`,
`src/louis-agent.mcp/Program.cs:28,32`), and the API calls `app.Run()` (`src/louis-agent.api/Program.cs:221`),
so the API either awaits `app.RunAsync()` or disposes the packs when it stops (*inferred*; the plan picks). A name that
is both a `PackKind` and an `MCP_PACKS` entry stops start-up. The pack's guide is ours (`config/mcp/{name}.md`),
reviewed like any prompt; only a trusted server's own instructions may stand in for it. The guide reaches
`SkillFileSource` through a small in-memory `IFileProvider` (F36's own, or a maintained one if one fits), added before
the first read, because a new pack's prompts after the first read throw `AgentCatalogException` (Phase 2). A server
that can't be reached gets its own *unavailable* message, distinct from `ToolsetSettingsMissing` ("Toolset '{Name}'
needs {Settings} to be set on this instance.", `louis-agent.core/Messages/ErrorMessage.cs:111-112`).

**When its tools are listed.** Once, when the host starts, before the catalogue is built; never when a session opens.
A chat keeps the tool list it opened with (ADR 0006 and 0008; FX4), and engines are built per toolset list and cached
(`louis-agent.core/Hosting/AgentEngineFactory.cs:34`), so a list that could change after start would change open chats.
`tools/list_changed` is ignored until a restart. That costs nothing: under the 2026-07-28 protocol revision it arrives
only after the client opens `subscriptions/listen`, and the 2.2.0 client never does (the MCP check's reading of the
SDK's source). The price is a snapshot: a server that is down when the host starts stays unavailable until a restart
or a refresh. **Refreshed for new chats** (step 9a; Louis, 2026-10-19, question 12: "yes and tool search can stay in later"): an admin action re-lists the servers into a new
catalogue generation that only chats opened after it use, so a business's MCP server is added without a restart and no
open chat changes. A tool list that changes *inside* a chat stays out: tool search (F38) is the way to more tools
mid-chat, and it stays in *Later*.

**What the SDK's source says, and what F36 does about it** (the MCP-packs check read the ModelContextProtocol 2.2.0
source at the commit its package names; nothing was run; the details are Part 3's *Row 23 in detail*):

- **The protocol revision.** 2.2.0 uses the 2026-07-28 revision by default: it probes with `server/discover` and falls
  back to `initialize`, and the probe's own timeout defaults to 5 s (`McpClientOptions.cs:50-74,128-139`). So a server
  that only speaks `initialize` spends up to 5 s of the start-up budget on the probe, and `serverInfo` is optional.
- **One start-up timeout.** A linked `CancelAfter` token covers `CreateAsync`, `ListToolsAsync` and the pin together.
  Not `ConnectionTimeout`, which applies to the SSE transport only (`HttpClientTransportOptions.cs:56-70`), and not
  `InitializationTimeout` alone, which covers only `CreateAsync`, so a server that connects and never answers
  `tools/list` would stall the host.
- **The pin.** By the server's version when it reports one, and always by a hash of the listed tools. `ServerInfo`
  throws when the server reported none (`McpClientImpl.cs:174-175`), so it is read inside a guard.
- **Drift.** `ListToolsAsync` returns `ValueTask<IList<McpClientTool>>` and silently drops a tool whose header
  annotations fail validation (`McpClient.Methods.cs:207-209`), so an allowlisted tool missing from the list counts as
  drift: the pack is unavailable, with its reason. The listing's `ttlMs` and `cacheScope` hints are ignored on purpose,
  as `list_changed` is, and F36's doc says so.
- **Failure at start-up.** The boundary catches every exception that is not the caller's own cancellation: a stdio
  start that fails comes back as a plain `IOException` (`StdioClientTransport.cs:220,229-247`), a bad discovery answer as
  a `JsonException`, a missing `ServerInfo` as an `InvalidOperationException`. The pack is then unavailable, with its
  reason, and the client is disposed when listing or the pin fails, because disposing the client is what ends a stdio
  child's process tree (`McpClientImpl.cs:801-813`); otherwise the child lingers. When several servers load together
  (`Task.WhenAll`), a failure in one still disposes the clients the others opened.
- **Names.** Each tool's name gets the prefix we give the server (never its `serverInfo.name`), then is checked against
  our rule, `^[A-Za-z0-9_-]{1,64}\z` (with `\z`, because in .NET `$` also matches before a final newline; the SDK's own
  check ends in `\z`, `AIFunctionMcpServerTool.cs:429`), or refused. The SDK doesn't check a foreign server's names,
  and MCP allows dots and up to 128 characters. A prefixed name that clashes with a tool already known (core's, a
  compiled pack's, or an engine tool such as `GetToday`), which the composition hands to each server's connection,
  refuses that one tool with a log line before the catalogue is built. So the catalogue's start-up error for a tool
  name in two toolsets (Phase 2 step 1; Part 2, rule 7) still holds for compiled packs, and never fires because of
  someone else's naming.
- **Transports.** A stdio server runs as a child process on the same machine (Axiz's price rules speak stdio only), its
  exact command shown to a person and approved before it first starts, with only its own settings passed
  (`InheritEnvironmentVariables = false`; the SDK's default is `true`). An HTTP server is reached over the network
  through `SsrfGuard` and Phase 7's egress proxy. HTTP's OAuth comes only when a real HTTP server needs it, through the
  SDK's `ClientOAuthProvider`: PKCE, the server's protected-resource metadata (RFC 9728), resource indicators (RFC
  8707), the `iss` check and Client ID Metadata Documents, with dynamic client registration only as the deprecated
  fallback; a server is sent only tokens its own authorisation server issued, never one passed through (the MCP
  authorization specification of 2026-07-28,
  https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization, as the engine research read it; not
  re-read here). Sampling, roots and the server's logging are not used.
- **Trust.** A server's annotations may only tighten Phase 7's approval policy: a tool marked destructive, or with no
  annotations, asks every time; `readOnlyHint` relaxes approval only for a server named as trusted in settings; by
  default every tool of an orange pack counts as an action outside the workspace, so it asks. The MCP specification says
  clients must treat annotations as untrusted unless they come from trusted servers, and the SDK's `ToolAnnotations`
  docs say the same (OWASP LLM06:2025).
- **Named, never discovered.** `RiderMcpClient`'s discovery runs in every host that calls `AgentHost.Build` today, the
  CLI, ACP and the API (`louis-agent.core/Hosting/AgentHost.cs:179-197`, on by default,
  `louis-agent.core/Config/AgentOptions.cs:222`; the MCP server's `BuildToolHost` and the orchestrator's own engine have
  none), and only logs what it finds (`louis-agent.core/Agent/ToolRegistry.cs:71-83`). With F36 a server is named by an
  identity or `MCP_PACKS`, never discovered, and the hand-written 199-line `RiderMcpClient`
  (`louis-agent.core/Protocols/Mcp/RiderMcpClient.cs`) is replaced; a Rider server named in settings keeps today's
  `rider_` names (`louis-agent.core/Hosting/RiderMcpToolDiscovery.cs:48`).

**Our guards on every path, and one pack published alone** (Part 3, row 24). Today the guards run only as the loop's
invoker (`AgentEngine.cs:123`, `ToolCallGuards.cs:38-70`), so a call served over our MCP server skips them, and Phase 2
had each pack frame and bound its own results. Here the guards become a decorator around each `AIFunction` (over
`DelegatingAIFunction`), so the loop, our MCP export and imported MCP tools all get the log line, a timeout and the
oversized-result guard. The cut-off check stays in the loop's invoker, because only it has `FunctionInvocationContext`.
A failure goes out as `ErrorContent`, so MCP marks it `isError`; today an "Error: …" string reaches an MCP client as a
success (`ErrorWithReason`, `ToolMessage.cs:156-157`). Over MCP an `McpException`'s message is kept and an
`McpProtocolException` becomes a protocol error, not an `isError` result (the MCP check's reading of
`McpServerImpl.cs:1699-1729`). `BuildToolHost` takes a toolset list (`MCP_TOOLSETS=shops`), so one pack is published
alone, with its guide as the server's instructions.

**Our own MCP server gains** the same annotations, from each tool's `ToolEffect` (never from `ParallelSafe`), and an
output schema on every tool (skill 4's "tool annotations ... and an output schema on every tool", `docs/GOAL.md:25`).

**Proved by** a simulation against a test MCP server in the repo, run free on a local model (Ollama); one paid run only
if the local model can't drive the tools, on Louis's go. F15, which tells Ollama the window, comes in Phase 9, so until
then a local model gets a truncated prompt on the CPU `ollama` service; the run uses the `ollama-nvidia` or `ollama-amd`
container, whose 32k default covers it (`TODO.md:394-399`), or a session that holds only the pack's toolset
(*inferred*; the plan picks). Phase 7's injection suite gains an injected MCP result. A client's own server (Axiz's
price rules) is plugged in only with their OK. The challenge suggested plugging in one real server early, Google's Maps
Grounding Lite, under its no-cache and attribution rules. It is not planned here: Louis said MCP only when needed ("if
needed, we are [alone] in this"), and its price and terms were not checked (*unverified*). It is a candidate for the
area guide once Phase 4's simulation shows where open data fails.

What happens to one MCP pack when the host starts:

![One MCP pack as the host starts: its settings, the approval, the connection, the tool list and the pin are checked before its tools join the catalogue](diagrams/F36-mcp-packs-phase-8-f36-their-systems-no-code-mcp-servers-as-packs.svg)

**Its plan starts from these parts**, in order (the research's steps for rows 23 and 24, corrected by this plan's
decisions; one commit each, test first, stop after each):

| # | Part | Done when |
|---|---|---|
| 1 | A guarded function over `DelegatingAIFunction`: the log line, a timeout, an oversized result cut (with no model, it cuts instead of summarising) | a fake function that returns 60,000 characters comes back cut and marked; a slow one times out with its reason |
| 2 | The loop wraps each function, and its invoker keeps only the cut-off check | one log line per call, not two; the golden files byte-equal |
| 3 | The MCP export publishes guarded functions, failures as `ErrorContent`, annotations from `ToolEffect` and an output schema per tool | through a test MCP client on in-memory streams (`ModelContextProtocol.Protocol.StreamClientTransport` and `ModelContextProtocol.Server.StreamServerTransport`), an oversized result arrives cut, a failure arrives with `isError` true, and a read-only tool is marked read-only; through the same test client, our server answers `server/discover`, and two `tools/list` calls return the tools in the same order (the 2026-07-28 server duties, *unverified* in Part 3's standards check until this test) |
| 4 | `BuildToolHost(catalog, toolsets)`; the guide as the server's instructions; a convention test that anything taken from the session also has a parameter (the MCP rule, Part 2's rule 3) | `PACKS=shops`, `SHOP_CATALOGUES=checkers` and a test `BROWSER_URL` (as `tests/louis-agent.core.tests/Agent/ModelRequestSnapshotTests.cs:45-46` sets them at `main`; building the tools connects to nothing) with `MCP_TOOLSETS=shops` publish the shop tools and nothing else, and without `BROWSER_URL` the toolset is listed as missing it (`louis-agent.core/Agent/ToolsetCatalog.cs:75-77`) and nothing is published; the instructions equal `shops.md`; the convention test fails on a fake tool that reads the user's place (`UserPlace.Current`, or from Phase 6 `TurnContext`) with no parameter for it, and every tool in core and every pack passes it or is named in the test with its reason (today `GetToday` reads the shared time zone with no parameter, `louis-agent.core/Tools/Clock/ClockTools.cs:24-26`; the plan decides whether it gains one) |
| 5 | `McpPack` over stdio against our own MCP server, listed when the host starts, its tools added through the ready-tool lines Phase 6's A5 brought; `Plugins.LoadAsync`, and `await using` in each host | its tools are listed with our prefix and reach the engine as ready functions, not as methods; a call goes through the guard; the host's exit ends the stdio child |
| 6 | Names: our rule after the prefix, a clash refusing one tool | a test server whose tool, once prefixed, takes a compiled tool's name (the prefix `Find` and a tool `Products` give `FindProducts`) loads all its tools but that one, with a log line, and the catalogue still builds; a name with a dot is refused |
| 7 | The start-up boundary: one timeout, every failure caught, the client disposed, drift and the pin | a server that never answers, one whose listing fails and one whose pin differs each leave the pack unavailable with its reason, and no child process behind |
| 8 | Approval and trust: a stdio command approved before it starts; annotations only tighten | an unapproved command never starts; a test server that marks a writing tool read-only still gets an approval prompt; a trusted server's read-only tool doesn't |
| 9 | `RiderMcpClient` replaced; discovery removed from every host | a fake Rider server, named in settings, gives today's `rider_` names; a host with no server named starts no discovery |
| 9a | **New chats pick up new MCP packs without a restart** (question 12): an admin action re-lists the configured servers (the same boundary, pin and drift checks as at start-up) into a new catalogue generation; sessions opened after it get engines from the new generation, keyed by it in `AgentEngineFactory`'s cache; every open chat keeps the engine and tool list it opened with (FX4, Phase 7); `tools/list_changed` stays ignored, so nothing changes by itself | test first: a chat opened before the refresh keeps its request byte-equal (its golden unchanged) and a chat opened after it lists the new server's tool; a refresh whose server fails leaves the current generation in place with the reason; the old generation's engines are dropped when their last chat closes |
| 10 | The simulation against a test server, run free on a local model, and an injected MCP result in Phase 7's suite | the simulation's graders pass on a free local run (or on the one paid run, on Louis's go, if the local model can't drive the tools) |
| 11 | **Docs and the learning write-up for skill 11**, written by Louis himself without the code open: the new `docs/learning/11-protocols.md` (MCP both ways, why the list is a start-up snapshot, why annotations can't be trusted); F36's doc records the SDK facts above; known limitation 1 deleted; F36 closed as *Finishing a feature* says | Louis's write-up written; links checked |

HTTP servers and OAuth come later, as their own steps, when a real HTTP server is needed.

**Risks.** The start-up snapshot: a server that is down at boot stays unavailable until step 9a's refresh or a restart,
which the control room shows with its reason. Step 9a re-lists for new chats only, without changing Phase 2's contract,
since after FX4 each open chat keeps its own list; a refresh on a timer can follow if a manual one ever pinches. Rule creep: every new server tempts a special
case, so each server is settings and a guide, never code. The name rule's 64-character bound is the provider's limit as
the plan states it, not an SDK fact, and was not re-checked (*unverified*). The 2026-07-28 revision is new, so an older
server's behaviour is the first thing the test server checks.

**Verification.** Every part is free: fakes, a test MCP server in the repo, the touched projects' tests and the golden
files. Paid: none, unless the local model can't drive the tools; then one run, on Louis's go.

