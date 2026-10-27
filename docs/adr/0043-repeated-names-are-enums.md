# 0043. Repeated names are enums, one per pluggable project

**Status:** Accepted · **Date:** 2026-10-27 (decided in F18 step 7.5, Louis's review) · **Recorded:** 2026-10-27

## Context

ADR 0024 made a value that names one of a *fixed* set an enum. A name that belongs to an *open* set (a toolset, a role, a mode,
an eval, a grader, a pack, an identity) stayed text, on the reasoning that a pack or an identity can add members, so no closed
list exists. The cost showed in the tests of F18: `"puls-cook"` is typed on 541 lines of C# in the tests, plus 5 in the evals and 1
in a host, and the same for `"puls-only"`, `"web"`, `"specialists"`. A typo compiles and loads nothing: a mode that no run has, a
toolset no engine offers, a grader no score reads, found only when a run silently does less than it should. Nothing makes a
`switch` over them complete, and a rename is a text search.

Louis, 2026-10-27: "No we do it now, I want an enum and make sure this is recorded. Pluggable is the goal but strings that
repeat must be made enum even if we duplicate the enum." And: "same goes for each project that is designed to be pluggable, this
is important, otherwise it is shared."

## Decision

**A name that repeats is an enum, even in an open set.** What counts: the name of a member of a set (a toolset, a role, a mode,
an eval, a grader, a pack, an identity) written as a literal in more than one place, tests included. An id or a sample value
(`sub-1`, a run id, a session id) is data, not a member of a set: where it repeats in tests it is one test constant.

**Where each enum lives follows what plugs in.**

- A project built to plug in (a pack, an identity, an eval set) keeps **its own** enum of the names it uses, and never references
  another pluggable project's enum, even if two enums then list `shops`. ADRs 0027 and 0028 hold: a pack never references another
  pack or an identity, and each can be taken out of the repo and served as an MCP server.
- Projects that are not built to plug in (core's own code, the hosts: the API, Puls's web app, the CLI, the ACP and MCP servers,
  the orchestrator; composition; a project and its own tests) **share one**: core's enum for core's own toolsets, which a host uses.

**Text stays text where it travels.** Environment variables, role files (`toolsets: shops?`), request bodies, the protocol and
stored runs keep their exact wire names (`puls-only`, `puls/voice-af`). A name is parsed once where it enters a project and
written back by the same table. `Config/SettingChoice` does it: a member carries its wire name as `[WireName("puls-only")]`
(`WireNameAttribute`, in core), `TryParse` matches the wire name or the member's name with the case ignored, `Wire(member)` writes
it back (the member's name in lower case when it has none), and `Names<T>()` lists wire names. The golden files and the stored runs
do not change: they are what proves the wire is untouched.

## Considered and rejected

- **One enum shared across the packs and the identity.** It would make every pack reference a project that names them all, which
  breaks ADRs 0027 and 0028 and the point of a pack.
- **Named constants for the strings.** One place per name, but still text everywhere it travels, and nothing makes a `switch`
  complete: the compiler cannot point at a case a new name needs.
- **A smart-enum class per set.** More code than the sets need (ADR 0024 rejected it for the same reason).

## Consequences

- **Duplicate enums between pluggable projects, on purpose.** Two projects may list `shops`; each owns its copy.
- **The cross-boundary names are checked by a test.** Where an identity names a pack, or a role lists a toolset, the name crosses
  a boundary as text. `composition.tests` checks that each such name is one the catalogue knows, so a typo there fails a test too.
- **A ratchet per set** (`RepeatedNameTests`) scans a project's sources and tests for each wire name as a string literal and fails
  on one outside the enum's own file, except test data the step listed as wire data (an expected JSON, a role file's header),
  which stays text.
- **Wire names have one source.** Adding a member is a member and its `[WireName]` in one place; the compiler points at each
  `switch` it must handle.
- **Done one set at a time**, toolsets first (they cross the most projects), each in its own commit.

## Links

Extends [ADR 0024](0024-choices-are-enums.md) · [ADR 0027](0027-engine-identities-and-packs.md) (engine, identities and packs) ·
[ADR 0028](0028-packs-stay-convertible-to-mcp.md) (packs stay convertible to MCP) · the F18 plan, *Approved during the build*
(step 7.5)
