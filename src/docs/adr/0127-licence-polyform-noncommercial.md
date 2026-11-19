# 0127. Licence: free for noncommercial use, PolyForm Noncommercial 1.0.0

**Status:** Accepted · **Date:** 2026-11-19 · **Recorded:** 2026-11-19 · **Supersedes:** [0016](0016-licence-all-rights-reserved.md)

![The framework's licence: free for noncommercial use, commercial use by the copyright holder's licence, earlier versions under theirs](diagrams/0127-licence-polyform-noncommercial.svg)

## Context

Since [ADR 0016](0016-licence-all-rights-reserved.md) the framework has been all rights reserved: public to read, but
nobody may use it without written permission. The framework is about to be published as packages, so that Puls, and
anyone else, can build on it from its own repository. Louis wants it free for everyone, but not for commercial use,
which must ask his permission first.

Three licences were weighed. The PolyForm Noncommercial License 1.0.0 is written for software and allows any
noncommercial use, change and sharing, with a standard SPDX name. The Business Source License 1.1 restricts
production use rather than commercial use, and turns each version into open source after a set date. Creative
Commons BY-NC 4.0 is noncommercial, but Creative Commons advises against its licences for software.

## Decision

The framework is licensed under the PolyForm Noncommercial License 1.0.0 (SPDX `PolyForm-Noncommercial-1.0.0`), from
2026-11-19 onward. `LICENSE` holds its text exactly as its publisher wrote it, under a Required Notice and a short
header that says commercial use needs a licence from the copyright holder, which he grants case by case. The framework
is source-available, not open source, and is never called open source.

This binds the framework, this repository. A product built on it, such as Puls in its own repository, chooses its own
licence.

## Consequences

- Anyone may use, change and share the framework for a noncommercial purpose: personal use, study, research, and
  charities, schools and public bodies, as the licence lists them.
- A business that uses it commercially needs Louis's licence first. That licence is how permission is given, and how
  commercial use is charged for.
- **Not retroactive:** a version released under MIT, the AGPL-3.0 or all rights reserved stays under that licence for
  whoever has it.
- The packages the framework is published as carry the licence by its SPDX name, so NuGet and GitHub show it.
- Every dependency is MIT, Apache, BSD or, for the fonts, SIL OFL, and they allow this. Their own notices still travel
  with what we hand out, listed in [THIRD-PARTY-NOTICES.md](../../../THIRD-PARTY-NOTICES.md).
- An outside contribution still needs a written agreement first, so that the copyright holder can licence the whole
  framework commercially.

## Links

- The licence: <https://polyformproject.org/licenses/noncommercial/1.0.0>
- [LICENSE](../../../LICENSE) · [README, Licence](../../../README.md#licence)

---
[Docs index](../README.md) · [ADRs](README.md) · Supersedes: [0016](0016-licence-all-rights-reserved.md)
