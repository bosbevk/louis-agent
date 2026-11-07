# Draft: Phase 9: F43, F8, F15, F44, F51

> **Plan draft, not yet approved.** Moved whole out of the old platform plan on 2026-11-07 (CL56), lines 3401 to 3528 of `platform.md` at the tag `pre-roadmap`; the body below is as it was. When the work starts, this is rewritten into the plan Louis approves (the development guide, rule 3) and its TODO row names it. The sub-feature split of 2026-10-28 is the TODO's rows.

### Phase 9 · What a sale needs

Planned to the feature here: four plans, each written and approved by Louis before its first step, with its four
diagrams, namespace map and folder trees: `docs/plans/F43-accounts-and-sessions.md`, `docs/plans/F08-reliability.md`,
`docs/plans/F15-model-limits.md` and `docs/plans/F44-engine-as-a-product.md`, each on its own `feature/` branch. F8 and
F15 already have feature docs (`docs/features/F08-reliability.md`, `docs/features/F15-model-limits.md`); F43 and F44 get
theirs in their first step. GOAL skills: 1, 9, 14, 15, 17, 18. Paid runs: none (F15's capability simulation runs free on
local models). No pilot or sale starts before this phase: until it, a configured agent is only shown (the playground's
simulations, Phase 4) and priced in the ten conversations (Phase 0). It removes known limitations 2, 3 and 8
(`TODO.md:384,386,394-399`), and it is done only when Louis has written its learning write-ups himself, without the code
open.

- **F43 Accounts, tenants and durable sessions** (skills 9 and 15). Sign-in; a tenant on every session, ledger and audit
  row; a sandbox per tenant for the tools that run code. **Durable sessions:** the session store is built with its
  MongoDB store (the API already runs one, `src/louis-agent.api/Services/Benchmarks/MongoBenchmarkStore.cs`, with
  `MongoDB.Driver` 3.12.0): a core `SessionRecord` holds the identity, the toolsets, the role, the frozen prompt and
  tools (FX4, Phase 7) and the transcript with its thinking blocks, and `ISessionStore` and a lifecycle enum live in
  core. A restart ends nothing (removes known limitation 2; today sessions live in a dictionary and are dropped after 4
  hours idle, `src/louis-agent.api/AgentSessions.cs:136,224`); the CLI and Rider resume a session after a restart
  (`TODO.md:252`), and a user's chats follow them across devices instead of living only in the browser's `localStorage`
  (`TODO.md:244`). Phase 6 B4's spike looks at Microsoft Agent Framework's checkpoints first. **POPIA:** a lawful ground
  for each purpose (s11, consent one of six), purpose (s13), retention (s14), security safeguards (s19), our duties as
  operator when we host a business's agent (ss20-21), breach notification (s22), correction and deletion on request
  (s24), and transfers outside South Africa, such as to the model provider (s72). With the accounts come a retention
  setting and a delete path for the logs (Phase 3, step 4f, already takes the place and the user's text out of them).
  Consent to send a chat abroad is Puls's notice, the host's job, not an engine switch. (The Act's sections are as the
  standards check read them at popia.co.za; not re-read here.)
- **F8 Reliability** (skill 14; and skill 1's fallback model). Each provider's own retry and timeout settings, as a
  provider strategy (ADR 0003): for example Anthropic's `ClientOptions.MaxRetries` and `Timeout`, which
  `louis-agent.core/Llm/LlmClientFactory.cs:67-72` sets to neither today, so the SDK retries twice by default (the
  package's docs, as the standards check read them). A fallback model per provider and the stop at the first
  account-limit refusal as `IChatClient` middleware on Microsoft.Extensions.Resilience (Polly v8); because the SDK
  retries a 429 by default, that stop needs its retry setting changed. Microsoft.Extensions.Http.Resilience only on our
  own `HttpClient`s (peers, web tools), never stacked on an SDK that retries, with its 10 s and 30 s timeouts raised for
  streams (Ollama's client already waits 10 minutes, `LlmClientFactory.cs:120-129`). Timeouts and resumable runs (skill
  14's *Missing* items, `docs/GOAL.md:35`) rest on F43's store. With it, the exception strategy (`TODO.md:306-309`):
  four kinds of failure, each with its rule, one base exception. F8's doc already has a design and its steps
  (`docs/features/F08-reliability.md:43-73`); its plan keeps the account-limit classifier and the fallback client and
  rewrites the retry step this way. Its story F8-S4 (`:34`; implementation step 6, the `ReportFixResult` and
  `RecordDecision` tools, `:70`) goes to F39 in Phase 6. F8's retries work at the provider level, one model request at a
  time, and are separate from the workflow's recovery contract (Phase 6, B5a), which decides what a workflow does with
  bad data; the idempotency keys F8 adds for long tasks (*proposed*; F8's doc names none today) serve that contract's
  "failure after a side effect", so a retried action is recognised and never done twice.
- **F15 Model limits and local models** (skill 1). Each model's window, output and cache minimum beside its prices,
  in a table dated by `as_of` (ADR 0007), which also gives skill 13 its last *Planned* item, the cache minimum and the
  price step. `num_ctx` and `think: false` sent to Ollama, so a local model gets its whole prompt (removes known
  limitation 8). Each model's capabilities (real tool calls, thinking, whether forcing one tool works, Phase 6 A6)
  measured by a simulation, run free on the local models, and kept as a capability record per model in place of
  name-prefix lists (`ThinkingBudgetModelPrefixes`, `LlmClientFactory.cs:82,88-89`; `KnownNoToolsPrefixes`,
  `louis-agent.core/Config/LlmOptions.cs:16`), which removes known limitation 3. Takes in F13. **Changed on 2026-10-20
  (Louis):** core knows no facts ("the core knows nothing, just to do calcs and when"). The limits and capabilities are
  a model table beside the price table, both loaded only from the file or URL the host names (`PricesUrl`/`PricesFile`,
  `ModelsUrl`/`ModelsFile`), in a JSON layout we define (`prices.schema.json`, `models.schema.json`, each with a
  `schema_version`, a `source` field per row instead of `//` comments), validated at load; the search for
  `config/prices.json` goes, so no source means costs unknown; the default model (`LlmOptions.cs:21`) and
  `MaxOutputTokens` (`LlmClientFactory.cs:21`) leave core's code too, and `ArchitectureTests` fails on a model id in
  core. **OpenAI as a full provider** comes with F15: `openai` gets its own usage mapping, caching reader, reasoning
  effort, prices and limits (each checked against OpenAI's docs when the plan is written), and the generic one is
  renamed `chat-completions` (`openai-compatible` kept as an alias through `SettingChoice`, ADR 0024; Louis: "we do not
  call anthropic-compatible"); Puls's simulations then run on a GPT model against Haiku 5.5, a paid run on Louis's go.
- **F44 The engine as a product** (skills 18 and 17). Its first step is F12-S1: `ttft_ms` and tokens per second on every
  ledger record (`docs/features/F12-response-speed.md:15-18`), so speed sits with the cost views that read the same
  records (ADR 0007); the ledger has `DurationMs` today but no time to the first token
  (`louis-agent.core/Usage/UsageRecord.cs:40`). Then: traces leave the process, through an OTLP exporter and latency and
  error dashboards (Phase 3 put Microsoft.Extensions.AI's `UseOpenTelemetry()` in the pipeline, sensitive data off),
  with W3C `traceparent` passed through `HttpPeerTransport` and MCP's `_meta`; this takes in F12 and the TODO's
  observability line (`TODO.md:373`). Beside the traces come OpenTelemetry metrics, as Existo's OpenTelemetry package
  has them (ADR 0030; from its NuGet page): token usage and cost read from the ledger's records (ADR 0007, so the
  metrics and the cost views agree), tool call counts, turn duration histograms and error counts, exported through the
  same OTLP exporter. Then the two contracts and the simulation harness are versioned: semantic
  versions, with PublicApiAnalyzers to declare the public surface, plus the SDK's package validation against the last
  released version (`EnablePackageValidation`, `PackageValidationBaselineVersion`), whose CP0005 and CP0006 stop a
  member added to a contract interface outside a major version, because buyers implement `IAgentIdentity` and
  `IAgentPack`. The engine ships as packages and a container. The packages are split as Existo splits its own (ADR
  0030): a core package plus one package per optional part (each provider strategy's extras, the MongoDB stores, MCP,
  OpenTelemetry, and each pack), so a buyer takes only what they use, and each package gets the package validation
  above. A .NET team plugs the engine into its own host the usual way, through one DI entry point,
  `services.AddLouisAgent(...)`, which returns a builder for identities, packs and settings, beside
  `Plugins.FromSettings` for our own hosts (Existo's is `services.AddExisto(...)`, returning an `ExistoBuilder`).
  *Building an identity or a pack* (written in Phase 2, step 5) is published for buyers, with the demo as its sample,
  together with licence terms for customers. It ships notices for the MIT, BSD and Apache-2.0 assemblies we ship
  (Apache's NOTICE files included; Apache-2.0 §4): Serilog's packages, PdfPig, MongoDB.Driver and the MCP SDK are
  Apache-2.0, and the repo has no `THIRD-PARTY-NOTICES` or NOTICE file (Part 3, row 29, checked). The container:
  every runtime image today is `mcr.microsoft.com/dotnet/sdk:10.0` with git installed by apt
  (`docker/Dockerfile.{acp-server,agent,api,mcp-server,orchestrator,puls}`), and git is GPL-2.0, so a container handed
  to a self-hosting buyer needs those licences and their source made available, or a slimmer runtime image. A deploy
  pipeline: CI gates the harness, the graders on recorded transcripts and the golden files; an agent's
  regression is caught only by a run, so a release also waits for the simulations (Phase 7's injection suite among
  them), paid on Louis's go or free on a local model once F15 is in.
  **Changed on 2026-10-20 (Louis):** F44 keeps its observability, versioned contracts, notices, container and
  pipeline, but the packaging (the split packages, the publish, `AddLouisAgent`) moves to *After the engine is
  finished*, after *Later*'s engine items, and then the engine moves to its own repo.
- **Hosting for public traffic** (with F44; its own item, so `TODO.md:151` has one home): where Puls and the configured
  agents run, uptime and alerting on the OpenTelemetry traces, and the running cost per month recorded beside the
  ledger. **Added on 2026-10-24 (Louis, *One agent API*, `plans/one-agent-api.md`):** one agent API image for every
  deployment, configured by `IDENTITIES` and `PACKS`, never an API project per identity. Puls gets a front door,
  `src/louis-agent.puls.gateway` (its web app, F43's sign-in, the consent notice, rate limits, a tenant on every call),
  the only container with a public port; it copies the agent API's SSE byte-equal to CL18's golden, and applies
  hand-offs (`HandOffs`, `ChatMap`, the allow-list checked again, a cap per chat) by pointing the browser's chat at a
  session on the target. The agent API leaves the public port, and `ApiNamesNoProductTests` loses its web-app exception.

**Their plans start from these parts** (the research's steps for row 25 for F43; the rest from the features' own docs
and this plan's decisions):

- *F43:* a `SessionRecord` in `Sessions/` that round-trips through JSON with a thinking signature byte-equal;
  `ISessionStore` and the lifecycle enum, with an in-memory store for tests (the allowed transitions; an unknown id is a
  clean miss); the MongoDB store, with the contract test run against both stores, as F31 does; resume (a chat resumed
  after a restart sends the request it would have sent without the restart); the 4-hour idle drop becomes a lifecycle
  state; then sign-in, tenants, the sandbox, retention and the delete path.
- *F8:* the provider strategy for retries and timeouts (test first: a stub returning 429 then 200 is retried by the
  provider's own setting, once, not by two layers); the account-limit classifier and the stop; the fallback middleware;
  the exception strategy's base exception and kinds.
- *F15:* the limits table beside the prices; `num_ctx` and `think: false` per request; the capability simulation and
  the record that replaces the prefix lists.
- *F44:* F12-S1; the exporter and `traceparent`; the OpenTelemetry metrics; PublicApiAnalyzers and package validation;
  the notices and the container's licences; the split packages and `AddLouisAgent`; the container; the pipeline;
  hosting.
- **The learning write-ups**, written by Louis himself without the code open: new
  `docs/learning/01-models-and-providers.md` (F8's fallback, F15's limits and capabilities), `docs/learning/09-sessions-and-state.md` (opening with FX4's lesson,
  then durable sessions and resume), `docs/learning/14-reliability.md` and `docs/learning/17-observability.md`;
  `docs/learning/18-testing-and-shipping.md` (Phase 2) gains versioned contracts and the pipeline, and
  `docs/learning/15-safety-and-security.md` gains tenants, the sandbox and POPIA. Each feature closed as *Finishing a
  feature* says; limitations 2, 3 and 8 deleted from the TODO.

**Verification.** Every part is free: fakes, an in-memory and a MongoDB store under the same contract test, recorded
transcripts and the golden files; F15's capability simulation runs on local models. A release's own simulation run is
paid only on Louis's go.

