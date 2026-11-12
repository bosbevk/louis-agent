# The general packs

> **Design doc.** The tools any agent may want, one pack each: what each holds and what it gets from the engine.
> **Elsewhere:** why they aren't in core in [ADR 0076](../../../src/docs/adr/0076-core-ships-no-agent.md), how to write one in
> [Build a pack](../../../src/docs/guides/build-a-pack.md).

![The general packs: files, git, .NET, shell, web, Paymo and Azure DevOps](diagrams/general-packs.svg)

| Pack | Toolsets | Holds | Needs from the engine |
|---|---|---|---|
| `louis-agent.pack.files` | `files` | Read, write, search, copy, rename and delete in the workspace; PDF, Word and Excel as text | The workspace root |
| `louis-agent.pack.git` | `git` | Status, diffs, branches, commits, merges, history; one runner shared by five classes | The workspace root |
| `louis-agent.pack.dotnet` | `dotnet` | Build, test, run, restore, list packages | The workspace root |
| `louis-agent.pack.shell` | `python`, `powershell`, `bash` | The tools the model calls, on the composition's script runners | The workspace root |
| `louis-agent.pack.web` | `web` | Search, fetch, read a drawn page, check a quote, convert money | Search, a guarded fetcher, the renderer, the rates |
| `louis-agent.pack.paymo` | `paymo` | Time entries, tasks, projects and summaries | Nothing: its own `PAYMO_API_KEY` |
| `louis-agent.pack.devops` | `devops` | Work items, sprints and team queries | Nothing: its own `DEVOPS_*` settings |

Each says `ComesBeforeTheEngine`, so a session's tools keep their order: these, then the engine's, then a product's.

**The web pack holds only tools.** The browser, the search providers and the guards around every fetch stay in the engine,
so a pack can't reach an internal address or skip a site's `robots.txt`. It keeps its own small PDF reader, because a pack
references no other pack.

## Diagrams

![The general packs: from a catalogue to their tools and what they reach](diagrams/general-packs-diagrams.svg)

![A session and a tool call: the factory finds each toolset and builds its tools, and the guards run a call the model makes](diagrams/general-packs-diagrams-2.svg)

![The types: every pack the same shape, its toolsets building their tool classes from what the engine hands them](diagrams/general-packs-interface.svg)

**Tests** are in `packs/tests/`, one project per pack, and a `.integration-tests` project where a tool starts a process.
`APackInstallsAsAPackageTests` proves a pack runs from a NuGet package with no engine in its dependencies.

---
[Packs' docs](../README.md) · Related: [Build a pack](../../../src/docs/guides/build-a-pack.md)
