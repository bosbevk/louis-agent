# 0136. An app is built from a description or from code, and only a description is checked

**Status:** Accepted · **Date:** 2026-11-27 · **Recorded:** 2026-11-27

![Two ways to build an app over MCP: a description the framework checks and builds from tested parts, or code on the packages that a person reviews; a person confirms both before they go live](diagrams/0136-an-app-is-built-from-a-description-or-code.svg)

## Context

The framework's end goal is to become a tool over MCP, so that someone can ask an AI "build me a system" and the
framework builds it. Louis put it this way on 2026-11-27: "a safe way for AI to generate APPS with my framework". MCP
is the open protocol an AI client such as Claude uses to call tools on a server, and the framework already has an MCP
host (`LouisAgent.Mcp`) that hands a client the packs' tools.

There are two ways an AI could build an app. It could write the app's code, the way the AI app builders do. Or it
could write a description of the app, in a shape the framework knows, and the framework could build the app from
parts that already exist. The first gives the AI every freedom. It also means every app carries code nobody wrote by
hand. Veracode tested more than 100 models on 80 coding tasks and found security flaws in the code in 45% of cases,
with bigger models "not significantly better"
([Veracode, July 2025](https://www.veracode.com/press-release/ai-generated-code-poses-major-security-risks-in-nearly-half-of-all-development-tasks-veracode-research-reveals/)).
OWASP's top ten for model applications lists improper output handling, and its advice is to treat the model like any
other user: check what it sends before anything acts on it
([OWASP LLM05:2025](https://genai.owasp.org/llmrisk/llm052025-improper-output-handling/)).

The framework already works the second way for pictures. A diagram is a JSON file our renderer draws
([ADR 0118](0118-a-diagram-is-data-our-renderer-draws.md)), and the diagrams tool refuses a file that names a box it
doesn't have, saying which one. An AI can write that JSON safely, because the worst it can do is be refused.

## Decision

We offer two ways to build an app on the framework, both over MCP, and say plainly which one is checked.

**A description** is data in a shape the framework defines, never code. The framework checks it before anything is
built. A wrong description is refused with the reason, so the AI can fix it and try again. What passes is built only
from parts a person wrote and tested by hand. When a description needs a part that doesn't exist yet, a person builds
that part by hand, with its tests and its review, and from then on every description can use it. This is the way we
call safe.

**Code** is written on the framework's packages, by a developer or by their AI, for what no description can say yet.
The framework can't check code the way it checks a description, so we never call it safe: a person reviews it like any
code, and its tests run before it ships.

Either way, a person confirms anything that goes live, or that changes what is live.

This binds the framework: every tool that builds a site, an agent or an app for an AI, over MCP or any other way. For
the description, it widens ADR 0118's rule from pictures to apps.

## Consequences

- In the description way, the worst a model can do is write a description that is refused. It can't put new code into
  an app, so the app is as safe as its parts, and those parts are tested once for every app.
- The description way can build only what the parts allow. The code way is there for the rest, at the price of a
  person's review each time.
- An MCP tool says which way it builds. A description tool returns what was built or the reasons it was refused. A
  code tool says that what it writes is not checked by the framework.
- Each kind of description needs its own shape, its own checks and refusals that say what is wrong. The shape is a
  contract, so it is versioned and tested like one.
- "Safer" is a claim we have to measure before we sell it, for example by holding the descriptions' checks against
  the kinds of flaw Veracode counted.
- It fits [ADR 0028](0028-packs-stay-convertible-to-mcp.md): the client's AI is the model, and the framework only gives
  it tools.

## Links

- [ADR 0118](0118-a-diagram-is-data-our-renderer-draws.md), a diagram is data our renderer draws
- [ADR 0028](0028-packs-stay-convertible-to-mcp.md), packs stay convertible to MCP
- [The roadmap's end goal](../../../ROADMAP.md#where-it-ends)
- [Veracode, AI-generated code poses major security risks](https://www.veracode.com/press-release/ai-generated-code-poses-major-security-risks-in-nearly-half-of-all-development-tasks-veracode-research-reveals/)
- [OWASP, LLM05:2025 Improper Output Handling](https://genai.owasp.org/llmrisk/llm052025-improper-output-handling/)

---
[Docs index](../README.md) · [ADRs](README.md) · Related: [0118](0118-a-diagram-is-data-our-renderer-draws.md)
