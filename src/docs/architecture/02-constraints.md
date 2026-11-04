# 2. Constraints

> **Explanation.** What was fixed before any design choice, and limits every part. **Elsewhere:** the choices made
> within them in [solution strategy](04-solution-strategy.md) and the [ADRs](../adr/README.md).

| Constraint | Why | Consequence |
|---|---|---|
| **.NET 10 and C#** | The developer's stack | One solution; `dotnet` build and test everywhere |
| **More than one model provider** | Claude by default, but local models (Ollama) and OpenAI-compatible servers must work | The Messages API tier with our own loop ([0002](../adr/0002-messages-api-tier-with-our-own-loop.md)) |
| **Tools run in our own containers** | Repositories are mounted, credentials are scoped, secrets must not leave | Docker for every host; no hosted sandbox |
| **Developed on Windows, run on Linux** | The developer's PC; the containers | Tests platform-neutral; bash is Git Bash on Windows, never WSL |
| **Local only** | Nothing is hardened for the internet yet | Every port bound to `127.0.0.1`; an API key when set |
| **Maintained, permissive packages** | Reuse before building | MIT, Apache or BSD packages; a new one is approved first |
| **A learning project** | The goal is to learn each AI-engineering skill hands-on | Small, reviewed steps; measure before and after; paid runs only when approved |
