# 0045. Typed answers and a shared contract: an endpoint declares what it sends, and both sides read the same records

**Status:** Accepted · **Date:** 2026-10-27 (Louis, reviewing the admin endpoint sweep: "why not have a contract and
parse it? is this the standard?") · **Recorded:** 2026-10-27

## Context

After the endpoint sweep's first file, every handler returned `Task<IResult>` through `Results.Json`, and the replies
were records inside `louis-agent.api`. The return type said only "some HTTP answer", and the Puls admin pages read the
same replies as raw `JsonElement`, digging fields out by name in seven files: rename a field on the server and the page
breaks in the browser, not in the build. Errors go out as `{"type":"error","error":{"type","message"}}` from an
anonymous object in `ApiErrors`, a shape the web apps and the orchestrator read.

Microsoft's minimal API guidance: "Returning `TypedResults` is preferred to returning `Results`"; a handler with more
than one answer declares them as `Results<T1, T2>`, which keeps the OpenAPI metadata and gives "compile-time checking
that a route handler actually only returns the results that it declares"
([Responses](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses)). Blazor apps keep the
records both sides use in a shared project (Fowler's *Data Transfer Object*, PoEAA).

Considered: `TypedResults` with the records kept inside the API (rejected: the web keeps guessing field names); keeping
`IResult` (rejected: the type says nothing); Problem Details (RFC 9457) for errors (not now: it changes every error's
JSON that today's clients read; a later version of the API may); C# interfaces as the contract (rejected: the
serializer can't read JSON into an interface, so a client needs the concrete record anyway).

## Decision

- **An endpoint returns `TypedResults`**, and one that can answer in more than one way declares them:
  `Task<Results<JsonHttpResult<TrendReply>, JsonHttpResult<ApiError>>>`, named once with a C# type alias
  (`using TrendAnswer = …;`) so the signature stays readable.
- **Errors keep their shape as a record**, `ApiError` (`type` and `error { type, message }`), built by `ApiErrors`;
  no anonymous object is left there.
- **The records are the contract and live in `louis-agent.api.contracts`**: replies, request bodies, `ApiError` and
  each wire's options object (`AdminJson.SnakeCase`). It references nothing, like `louis-agent.choices`, so the API and
  any client (Puls, the code-review web app, the orchestrator, a customer's app) can reference it.
- **A client reads the record**, `GetFromJsonAsync<TrendReply>(url, AdminJson.SnakeCase)`, never a `JsonElement`
  picked apart by name ([0041](0041-json-through-records-by-hand-only-for-bytes.md) already keeps `JsonElement` for
  input we don't own; this is input we do).

## Consequences

- A renamed or dropped field fails the build on both sides; a test calls a handler and checks the answer's type with no
  HTTP; OpenAPI can describe every endpoint when we want it.
- One more project, and longer signatures (the alias keeps them short).
- The API side moves with the endpoint sweep, file by file; the Puls admin pages switch to the records in F18 step 7b
  (ii), which rebuilds them anyway; other clients when a change touches them.

## Links

[0039](0039-api-routes-under-v1.md) · [0040](0040-records-for-values-classes-for-things.md) ·
[0041](0041-json-through-records-by-hand-only-for-bytes.md) · `docs/CLAUDE.md`, *An endpoint is a named method* ·
[F18 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F18-evals.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
