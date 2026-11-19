# 2. Constraints

> **Explanation.** What was fixed before any design choice, and limits every part. **Elsewhere:** the choices made
> within them in [solution strategy](04-solution-strategy.md) and the [ADRs](../adr/README.md).

![Constraints: the stack, more than one provider, our own containers, Windows and Linux, permissive packages and a learning project](diagrams/02-constraints.svg)

A constraint is something no design choice gets to change. These were settled before anything was built, and every
part has to live within them. The table says why each one holds and what it costs the design.

| Constraint | Why | Consequence |
|---|---|---|
| **.NET 10 and C#** | The developer's stack | One solution; `dotnet` build and test everywhere |
| **More than one model provider** | Claude by default, but local models (Ollama) and OpenAI-compatible servers must work | The Messages API tier with our own loop ([0002](../adr/0002-messages-api-tier-with-our-own-loop.md)) |
| **Tools run in our own containers** | Repositories are mounted, credentials are scoped, secrets must not leave | Docker for every host on a PC, Container Apps in Azure; no hosted sandbox |
| **Developed on Windows, run on Linux** | The developer's PC; the containers | Tests platform-neutral; bash is Git Bash on Windows, never WSL |
| **Local unless deployed on purpose** | Only what is meant for the public is hardened for it | On a PC every port is bound to `127.0.0.1`; Puls and the two sites are deployed to Azure Container Apps on purpose ([0095](../adr/0095-puls-runs-from-azure-and-nothing-points-at-the-pc.md)) |
| **Maintained, permissive packages** | Reuse before building | MIT, Apache or BSD packages; a new one is approved first |
| **A learning project** | The goal is to learn each AI-engineering skill hands-on | Small, reviewed steps; measure before and after; paid runs only when approved |

---
[Architecture](README.md) · Previous: [Introduction and goals](01-introduction-and-goals.md) · Next: [Context and scope](03-context-and-scope.md)
