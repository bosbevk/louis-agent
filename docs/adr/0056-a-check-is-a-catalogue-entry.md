# 0056. A check is a catalogue entry: the contract in its own project, found by type, built from a typed record

**Status:** Accepted · **Date:** 2026-10-30 (Louis's picks on the F49.1 plan's cards) · **Recorded:** 2026-10-30

## Context

Today a check is a C# class (behind `IGrader`) built by hand in an eval's `BuildGraders()`, and its whole contract
(`IGrader`, `Grade`, `Trial` and the turn types) lives inside the harness, `evals/louis-agent.evals`. Two things
follow. A pack can't ship a check, because a pack may not reference the harness: the harness runs evals and names
Puls's ([0027](0027-engine-identities-and-packs.md)). And no file can name a check, because every check is a
constructor call. F49 wants an eval to be a JSON file that names its checks, and a pack to ship the checks beside its
tools.

Weighed for where the contract lives: its own small project (taken), core (core would grow an evals concern it never
runs), and the protocol project (that is the wire, and run-time objects there would make every protocol change a
check change). Weighed for how a check is found by name: a registry each owner fills (taken), an attribute scanned
by reflection (magic a reader can't follow, and composition no longer the one place that knows them all), and an
enum with a switch in the harness (the harness would have to reference every pack).

## Decision

- **The contract lives in `evals/louis-agent.evals.checks`**, which references only what a pack already references:
  core (`MessageText`), the session stream (a turn's events) and the eval protocol (the kinds a grade carries). Never
  the harness, a pack, an identity or a package; `ArchitectureTests` holds it. Pattern: *Separated Interface*
  (Fowler, PoEAA). The moved types keep their namespaces (`LouisAgent.Evals…`).
- **A check is a catalogue entry**, found by its type in kebab case (`within-time`), with a typed settings record
  (`WithinTimeCheck.Settings(int Seconds, bool Required = true)`) read strictly through the record
  ([0041](0041-json-through-records-by-hand-only-for-bytes.md)), never a dictionary: an unknown, missing or null
  field is refused, naming the type and the field. The entry builds the check from the record (*Factory Method*,
  `CheckType<TParameters>`), and it is nested in its check (`WithinTimeCheck.Entry`), so a check and the way a file
  names it sit in one place.
- **A registry each owner fills**: the harness lists the general checks (`GeneralChecks.All`), a pack lists its own
  (F49.3), and the composition gathers them into one catalogue, as it gathers `PACKS` and `IDENTITIES`. Pattern:
  *Registry* with *Plugin* entries (Fowler, PoEAA). A type is one an MCP tool name can carry, listed once.
- **The words:** `ICheck` is what grades a trial (today's `IGrader`, renamed in C# only; the wire keeps its keys,
  with no alias, [0049](0049-nothing-is-compatible-before-a-sale.md)), and `ICheckType` is the catalogue entry that builds one.
- **Data stays data, code stays code** ([0055](0055-keep-it-simple.md)): an eval is a JSON file and a check is C#;
  nothing of our own (no script engine, no expression language) sits between them.

## Consequences

- A pack can ship a check without reaching the harness, and an eval file can name a check by its type.
- The contract project drags the engine with it: it is a "references what a pack references" project, not a
  "references nothing" one. A pack's check costs no new reference.
- Two projects share the namespace root `LouisAgent.Evals`, so a `using` doesn't say which project a type is in.
- A check forgotten from its owner's list is unreachable; a test lists the catalogue, as `PackConvertibilityTests`
  lists the packs.
- A new check is a reviewed C# class and its entry, never a script in a file.
- A check whose setting is code can't be an entry yet: `routed-to`'s expected role is a function, so it waits for
  F49.2's eval file. F49.1's entries are `no-error`, `within-time`, `cost-under`, `contains` and `tool-used`; a
  pattern check was dropped ([0057](0057-regex-is-the-last-option.md)).

## Links

[Write-up 20: Extensibility](../learning/20-extensibility.md) (F49.1, with the plan's findings 2, 3, 6 to 9) ·
[F49 Evals as content](../plans/F49-evals-as-content.md) · [0027](0027-engine-identities-and-packs.md) ·
[0041](0041-json-through-records-by-hand-only-for-bytes.md) · [0043](0043-repeated-names-are-enums.md) ·
[0055](0055-keep-it-simple.md) · [louis-agent.evals.checks](../projects/louis-agent.evals.checks.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
