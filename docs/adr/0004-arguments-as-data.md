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

## Amended 2026-10-19 (FX19)

A list stops a value being split; it doesn't stop a value being read as an option. `git show --output=<file> --stat`
takes `--output` as an option however it arrived, and `git config core.fsmonitor <command>` makes the next `git status`
run the command. So for git, and for any program that reads options from its arguments, the rule has two more parts:
our own options come first, then `--end-of-options`, then the model's values, with paths after `--`; and a value that
names something (a ref, branch, remote, path, key) and starts with `-` is refused before the program runs, so a command
that takes neither marker is covered too. Free text that is an option's own argument (a `-m` message) travels whole.
Where a program's own settings can run commands, the keys it may set are an allow-list (`SetConfig`: `user.name`,
`user.email`), since a deny-list ages. `ArchitectureTests` fails any core file that builds a process's command line as
one string. Found by [FX19](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX19-git-arguments-as-data.md).

## Links

[Architecture: Security](../architecture/08-crosscutting-concepts.md#security) · [Add a skill](../guides/add-a-skill.md)
