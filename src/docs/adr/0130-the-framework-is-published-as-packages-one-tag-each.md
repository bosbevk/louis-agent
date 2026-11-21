# 0130. The framework is published as packages, one tag each

**Status:** Accepted · **Date:** 2026-11-21 · **Recorded:** 2026-11-21

![The 31 packages, a version from a tag, one commit's tags as one batch, and a guard that refuses what can't be restored](diagrams/0130-the-framework-is-published-as-packages-one-tag-each.svg)

## Context

Puls is leaving this repository for its own (F166), and a product outside the repository can't reach the framework by
a path to its project files. The framework had to become something a product installs. Three choices had to be made:
where the packages live, how each gets its version, and how a release keeps the packages that depend on each other
restorable.

For where, GitHub Packages and nuget.org were weighed. nuget.org needs no token to install, but never deletes a
version and accepts only licences approved by the Open Source Initiative or the Free Software Foundation as an SPDX
expression; GitHub Packages asks for a token even for a public package, but a version can be withdrawn while the
framework settles. For the version, MinVer (from git tags), Nerdbank.GitVersioning (from a file and a count of
commits) and a version written by hand were weighed. For which projects, the 31 a product reaches, all 44 libraries,
and one version for all were weighed against one version each. For a release, one tag per package as OpenTelemetry
.NET contrib releases, one run that bumps whatever changed, and packages referencing each other's released versions
were weighed.

## Decision

We publish the framework as NuGet packages, one tag each.

- **The packages** are the projects a product built on the framework reaches: 31 today. A project is a package only
  when its project file says `IsPackable`; `Directory.Build.props` makes every other project none, and `PackagesTests`
  holds the list. Every package carries the licence by its SPDX name (ADR 0127) and Source Link.
- **They live on GitHub Packages** while the framework is at `0.x`, and move to nuget.org at the first stable `1.0`,
  with the licence as a file in the package there.
- **Each package has its own version**, read by MinVer from a tag of the package's own name: `LouisAgent.Core-0.2.0`.
  `MinVerIgnoreHeight` keeps an untagged package at its last tag's version, so a release of one package depends on
  its neighbours' published versions.
- **A release is a tag on a commit of `main`.** The publish workflow packs every package tagged on that commit as one
  batch, since GitHub starts one run per tag in no set order, and pushes with the run's own token.
- **Nothing is published that can't be restored.** Before the push, a guard finds each package's framework
  dependency in the batch or on the feed at the version it names; one that is neither stops the run.
- **A package carries everything a product needs to use it outside this repository** (added 2026-11-21, F166.3): its
  Sass, the files its code reads at run time, and public access to what a pack calls. Inside this repository every
  path exists, so a gap shows only from outside: `LouisAgent.Packages.IntegrationTests` proves each one from a
  project outside it, and a product never copies a framework file to fill one.

## Consequences

- A product installs the framework like any NuGet package; Puls is the first. The proof is a project outside the
  repository that restores the packages with only a workflow's token and runs an agent (`consume-packages.yml`), and
  the same without the feed on every pull request (`LouisAgent.Packages.IntegrationTests`).
- Packages move at their own pace, so a change across several means tagging each of them, together, on one commit.
- GitHub makes no event when more than three tags are pushed at once; one tag is then deleted and pushed again, and
  its run publishes the whole batch. [Publish the framework](../guides/publish-the-framework.md) says how.
- A version is never published twice: a broken release is followed by the next version.
- A test that packs a project says the version it means, since a project's version now comes from its tags (FX218).
- Puls's first build on its own found four such gaps, each fixed in the framework and held by a test there: the web
  packages' Sass, the test helpers' and the runner's files, and `LeadImage`. How each is packed is in
  [Publish the framework](../guides/publish-the-framework.md#what-a-package-carries-besides-its-code).

## Links

- F166.2, the framework as packages; [Publish the framework](../guides/publish-the-framework.md)
- [ADR 0129](0129-a-project-is-named-for-its-namespace.md), a project is named for its namespace
- [ADR 0127](0127-licence-polyform-noncommercial.md), the licence
- MinVer: <https://github.com/adamralph/minver> · OpenTelemetry .NET contrib's releasing:
  <https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/build/RELEASING.md>

---
[Docs index](../README.md) · [ADRs](README.md) · Related: [0129](0129-a-project-is-named-for-its-namespace.md) · [0127](0127-licence-polyform-noncommercial.md)
