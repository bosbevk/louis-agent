# 0025. A role declares its own toolsets; a `?` toolset loads only where an instance has it

**Status:** Accepted · **Date:** 2026-10-17 (decided in F33 step 4) · **Recorded:** 2026-10-17

## Context

[0006](0006-toolsets-per-session.md) has the caller name a session's toolsets when it opens: a parent passes them in
`StartSubSession`, a client in `POST /sessions`. F33 gave the cook a `shops` toolset (`FindProducts`, Checkers'
own catalogue), and the cook needs it every time it cooks. Louis: "we want the cook to have the tool always", without
Puls passing it. Puls's routing would otherwise have to know what each of its specialists needs.

A second problem came with it: `shops` exists only where `SHOP_CATALOGUES` and `BROWSER_URL` are set (development and
benchmarks, [0023](0023-copyleft-service-beside-us.md)). On the sold endpoint, a cook that required `shops` would be
refused, though it still cooks well with `web`.

Considered: the parent passes `web, shops` (every caller must know, and the sold endpoint refuses the cook); a table
in C# from role to toolsets (a prompt's needs kept apart from the prompt, against
[0011](0011-prompts-are-content.md)); the role's file itself (chosen).

## Decision

- **A role's file may declare toolsets** in a header at its top, which the prompt reader strips, so the model never
  sees it:

  ```markdown
  ---
  toolsets: shops?
  ---
  ```

  When a session opens with that role, its declared toolsets are **added to** those the caller passed, then the
  delegation rules apply to the whole list, as to any other. The session keeps them for its lifetime, as
  [0006](0006-toolsets-per-session.md) says: the list is still fixed when the session opens, so its prefix stays
  cacheable.
- **A toolset name ending in `?` is optional:** it loads where this instance has the toolset (its settings are set),
  and is skipped, with a log line, where it doesn't. An unknown name is still refused, `?` or not, so a typo never
  passes silently. Named both ways, the required name wins.
- `?` works wherever a toolset is named (a header, `StartSubSession`, `POST /sessions`); a header uses it when its tool
  is a bonus, not a need.

## Consequences

- A role carries what it needs: the cook gets `shops` from any caller, and Puls's routing passes `web` alone.
- One role file works on every instance: where `shops` is off, the cook runs without it, and the log says why.
- Which tools a session has now depends on the instance as well as the request. The session's record lists the
  toolsets it actually loaded, not the names it was asked for.
- Role files have a small format of their own (`RoleHeader`); only `toolsets:` is read, and any other key in it is
  ignored.

## Links

[F33 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F33-shop-catalogues.md) (*Approved during the build*, step 4) ·
[F33 Shop catalogues](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F33-shop-catalogues.md) · [0006](0006-toolsets-per-session.md) ·
[0017](0017-skills-by-level.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
