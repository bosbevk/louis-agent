# 0129. A project is named for its namespace

**Status:** Accepted · **Date:** 2026-11-21 · **Recorded:** 2026-11-21

![A project's folder, project file, DLL, namespace and package carry one name; a new project starts with it, a shared namespace gets the project's own part, and code that reads names matches it](diagrams/0129-a-project-is-named-for-its-namespace.svg)

## Context

Every project used to have two names. On disk it was `louis-agent.pack.web`, and so was its DLL, while its code said
`LouisAgent.Packs.Web`. That was fine while nothing left the repository. But the framework is about to be published as
packages (F166.2), and a package takes its project's name unless told otherwise, so users would install
`louis-agent.pack.web` and type `using LouisAgent.Packs.Web;`. Microsoft's naming guidance asks for the
`Company.Component` shape in PascalCase for namespaces and assemblies, and says the two need not match but usually
should.

Three ways were weighed: give only the package the namespace's name, rename the assemblies too, or rename the
projects, files and folders as well. Louis picked the last, so a project has one name everywhere. F167 renamed all 155
projects that way, reading each new name from the project's own `RootNamespace`.

## Decision

We name a project for its namespace. Its folder, its project file, its DLL and, when it ships, its package carry the
name its `RootNamespace` says, in PascalCase: `src/engine/LouisAgent.Core/LouisAgent.Core.csproj`. A project's tests
take its name and `.Tests`, or `.IntegrationTests` for the ones that leave their process. Its design doc and pictures
take its name too (`LouisAgent.Core.md`, `LouisAgent.Core-how-it-works.json`).

Where two projects share a namespace, the one that owns it keeps it as its name, and the other adds its own part:
`LouisAgent.Evals.Checks` beside `LouisAgent.Evals`. That project's `RootNamespace` stays as its code needs it.

Code that finds a project by its name matches the PascalCase name, never the old lower-case spelling. Where it needs
the short name an identity or a site goes by (`puls`, `code-review`), it reads it from the folder's name, as the tests'
`ProjectNames.ShortName` does.

## Consequences

- A package, its DLL and the `using` a reader types are the same words, which is what F166.2 publishes.
- A new project is created under its final name: the guides' examples (`LouisAgent.Packs.Units`,
  `LouisAgent.Identities.Acme`) show it, and `SolutionLayoutTests` lists every project by that name.
- The names that are not projects keep their own spelling: the domain `louis-agent.dev`, the repository, the brand,
  and the keys a browser stores (`louis-agent.chats`). Only a project's name follows this rule.
- A host's service name in the logs and traces is read from its assembly and kebab-cased, so it stays `site-api`, and
  dashboards that group by it don't change.
- Earlier docs that tell what was true then, such as write-ups and older ADRs' context, keep the old names.

## Links

- F167, the rename (its commands and reports are kept in `src/tools/rename-projects`)
- [ADR 0126](0126-a-name-says-what-it-is-by-its-kind.md), a name says what it is, by its kind
- [ADR 0010](0010-folders-by-concern.md), namespace = folder path
- Microsoft's [names of namespaces](https://learn.microsoft.com/dotnet/standard/design-guidelines/names-of-namespaces)
  and [names of assemblies and DLLs](https://learn.microsoft.com/dotnet/standard/design-guidelines/names-of-assemblies-and-dlls)

---
[Docs index](../README.md) · [ADRs](README.md) · Related: [0126](0126-a-name-says-what-it-is-by-its-kind.md) · [0010](0010-folders-by-concern.md)
