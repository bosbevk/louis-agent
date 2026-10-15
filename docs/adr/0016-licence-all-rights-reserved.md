# 0016. Licence: all rights reserved

**Status:** Accepted · **Date:** 2026-10-15 · **Recorded:** 2026-10-15 · **Supersedes:** [0015](0015-licence-agpl.md)

## Context

[ADR 0015](0015-licence-agpl.md) moved the repo from MIT to the AGPL-3.0 the same day, so the code stayed open source
while a hosted copy of Puls would have to publish its changes. Louis then asked for a strict licence. The options
weighed, from loosest to strictest: Business Source 1.1 (everything except a competing service, open source after a
set date), PolyForm Noncommercial (non-commercial use, changes and sharing), PolyForm Strict (non-commercial use, no
changes or sharing), and no licence at all.

## Decision

All rights reserved: `LICENSE` grants nothing. The repo stays public so it can be read, but nobody may use, copy,
change, share or host it without written permission. It is no longer open source, so every "oopbron" claim goes.

## Consequences

- `LICENSE` is the notice; the README's licence section, the landing page (the hero's line, Oor ons, the FAQ, the
  footer) and the CHANGELOG say so.
- **Not retroactive:** a version released under MIT or the AGPL-3.0 stays under that licence for whoever has it. The
  notice applies from 2026-10-15 onward.
- GitHub's Terms of Service still let anyone view the public repo and fork it on GitHub; that gives no right to use the
  code.
- Every dependency is MIT, Apache or BSD (the reuse rule), and they allow use in closed code; their own notices still
  apply to them.
- An outside contribution would need a written agreement first, since no one can build on the code otherwise.
