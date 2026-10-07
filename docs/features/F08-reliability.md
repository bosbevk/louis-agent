# F8 · Reliability: retries, account limits, fallback, structured results

> **Status:** planned · **Milestone:** M3 · **Depends on:** F1
>
> **Spec:** [Optimisation §F](../specs/RESPONSE_OPTIMISATION.md) · [Usage §6.2](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F8 Reliability** under *Now* ([TODO](../../TODO.md)) · **Replaces:** "Retry and back off when the model API refuses", "Add model fallback", "Wire up structured outputs"

Failures cost money twice: the work done before the failure is lost and has to be redone. The demo showed both kinds —
the account's credit and then its usage limit ran out mid-run, and every remaining error failed one by one. F8 makes
transient errors retry, account limits stop a run at once, a second model take over when the first is down, and the
agents' machine-read results arrive as structured data instead of a text line that can be malformed.

## User stories

**F8-S1 — Ride out transient errors.** As an *operator*, I want rate-limit and overload errors retried with backoff, so
that a busy moment doesn't fail a fix.
- *Given* a 429, 529 or 5xx from the provider *then* the request is retried up to `LLM_MAX_RETRIES` (default 3), waiting
  as `retry-after` says or with exponential backoff; each retry is logged; the ledger records only the successful call.

**F8-S2 — Stop at an account limit.** As an *operator*, I want the orchestrator to stop at the first credit or usage-limit
refusal, so that a run doesn't fail every remaining error and I know exactly what to do.
- *Given* the provider refuses with "credit balance is too low" or "reached your specified API usage limits" *then* the
  request fails with a typed `AccountLimitException` (kind: credit / usage limit, and the reset date when given);
  the orchestrator stops the run, leaves the remaining errors unprocessed and prints "Account limit reached (…).
  Raise it, then run with -Resume."; the API sends an `error` event of type `account_limit_error`.

**F8-S3 — Fall back to a second model.** As an *operator*, I want a configured fallback model used when the main one keeps
failing, so that work continues during an outage.
- *Given* `LLM_FALLBACK_PROVIDER`/`LLM_FALLBACK_MODEL` and the main model's retries are exhausted (or it's overloaded)
  *then* the request goes to the fallback; the ledger records the fallback model; the log says so once per session.
- *And* account limits (S2) don't fall back to the same account; a different provider may.

**F8-S4 — Structured results between agents.** As the *orchestrator*, I want louis-agent's result and my own decision as
structured data, so that a malformed text line never costs a "continue" round-trip or a wrong decision.
- *Given* a fix session *then* louis-agent reports its result by calling a `ReportFixResult` tool (status, commit,
  branch, tests, summary, reason); the orchestrator reads it from the `tool_use` block in the stream.
- *Given* a triage *then* the orchestrator records its decision by calling `RecordDecision` (outcome, reason).
- *And* the `FIX-RESULT` / `DECISION` text lines are still accepted as a fallback, so older runs and providers work.

## Design

- **Retries:** use the Anthropic SDK's built-in retry setting (`LLM_MAX_RETRIES` passed to the client options in
  `LlmClientFactory`); for the other providers, a small `RetryingChatClient` with the same policy.
- **Account limits:** an `AccountLimitClassifier` in `LlmClientFactory`'s Anthropic path maps the provider's 400 error
  messages to `AccountLimitException`. Provider-specific text stays out of the core; the core only knows the exception.
  Hosts map it (API error type; orchestrator stops the run).
- **Fallback:** `FallbackChatClient(main, fallback)` built by `LlmClientFactory` when a fallback is configured. Caches
  are per model, so the fallback starts cold; thinking blocks from another model are dropped by the provider.
- **Structured results — tools, not output formats:** a tool call is validated against its schema on every provider
  that supports tools, unlike provider-specific structured-output options.
  - `ReportFixResult` lives in a small `TaskResultTools` class registered with the `skills` toolset (F5) — harmless in
    chats, used when the fix prompt asks for it. The fix prompt (`ServiceTools.FixPrompt`) says "call ReportFixResult"
    instead of "end with a FIX-RESULT line".
  - `RecordDecision` is added to the orchestrator's `ServiceTools`; `Program` reads it instead of parsing `DECISION:`.
  - `LouisAgentClient.ParseFixResult` checks the tool call first, then the text line.

## Implementation steps

1. **Retry setting + `RetryingChatClient` for non-Anthropic providers.** *Test:* stub handler returning 429 then 200 →
   one success, one logged retry; `retry-after` honoured.
2. **`AccountLimitException` + classifier.** *Test:* both known messages map to the right kind; other 400s don't.
3. **Orchestrator stops on account limit.** *Test:* the second triage throws it → the run stops, errors 2..n stay
   unprocessed, run summary says why.
4. **API mapping** to `account_limit_error`. *Test:* stream test / manual.
5. **`FallbackChatClient` + settings.** *Test:* main fails after retries → fallback answers; account-limit doesn't fall
   back to the same provider.
6. **`ReportFixResult` and `RecordDecision` tools; prompts updated; parsers prefer tools.** *Test:* `LouisAgentClientTests`
   with a `tool_use` block; text-line fallback still parsed; `DecisionTests` likewise.
7. **Benchmark run**: fewer "continue" messages caused by missing results; no change in fix rate.
8. **Docs:** AGENT_COMMUNICATION (message formats now tools), MODELS (fallback), SETUP, spec status.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Acceptance: with the account's limit artificially low (a test key or stub), the run stops at the first refusal with
  the message, and `-Resume` completes it after.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F8** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F7 Budgets](F07-budgets.md) · Next: [F9 Bounded history](F09-context-management.md)
