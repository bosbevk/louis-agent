# 0015. Licence: AGPL-3.0, so improvements to a hosted Puls stay open

**Status:** Accepted · **Date:** 2026-10-15 · **Recorded:** 2026-10-15

## Context

The repo was MIT from the start: anyone could use, change, sell and close the code. Puls is becoming a paid service
(F23 Credits), built on this code and sold by its author. Under MIT a competitor could take the code, host it as their
own service and keep every improvement to themselves. Louis wanted the code to stay free (open to use, study, change
and share) without that outcome. Options weighed: keep MIT; AGPL-3.0; a source-available licence (Business Source,
Functional Source, Elastic, PolyForm Noncommercial), which is free to use but not open source by the OSI definition, so
the landing page's "oopbron" would have to go.

## Decision

GNU AGPL-3.0 for the whole repo. It is open source, so "oopbron" stays true; its one extra condition over the GPL is
the one that matters here: whoever changes the code and runs it as a service for others must publish their changes
under the same licence. The author keeps the copyright and may offer a commercial licence to anyone who wants out of
that condition. Every dependency is MIT, Apache or BSD (the reuse rule), all compatible with the AGPL. No outside
contributor has code in the repo, so no agreement was needed to relicense.

## Consequences

- `LICENSE` is the AGPL-3.0 text; the README's licence section and the landing page (Oor ons, the FAQ, the footer)
  say so. Earlier releases stay MIT: a licence change is not retroactive.
- Some companies refuse AGPL code; for them a commercial licence is possible later, which the AGPL does not prevent.
- A fork that is only used privately or inside one organisation owes nothing; the condition bites on offering it to
  others over a network.
