# 0004. Arguments travel as data, never through a shell

**Status:** Accepted · **Date:** 2026-10-06 · **Recorded:** 2026-10-14

## Context

The model writes the arguments for skills, agent-built tools, git and `dotnet`. Model text can be wrong, and it can
carry instructions injected from a web page or a file it read. Splicing it into a command line would let that text
become a command.

## Decision

Skill and agent-built tool arguments reach their scripts as `SKILL_ARG_*` environment variables, and processes start
from an argument list (`ProcessRunner`), never from a shell string built from model text.

## Consequences

- A quote or `;` in an argument stays a character, not a new command.
- Scripts must read their arguments from the environment and quote them; *Adding a skill* says so.
- The bash, PowerShell and Python toolsets still run scripts the model writes, by design: what those may do is bounded
  by the workspace boundary and the container, not by this rule.

## Links

[Architecture: Security](../ARCHITECTURE.md#security) · [Adding a skill](../CLAUDE.md#adding-a-skill)
