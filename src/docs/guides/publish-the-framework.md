# Publish the framework

> **How-to.** How a new version of one of the framework's packages reaches GitHub Packages, how several are released
> together, what stops a release, and how to check a release from outside the repository. **Elsewhere:** which
> projects are packages and their shared settings in `Directory.Build.props` and `Directory.Build.targets`, held by
> `PackagesTests`; why one name for a project, its DLL and its package in
> [ADR 0129](../adr/0129-a-project-is-named-for-its-namespace.md); the licence the packages carry in
> [ADR 0127](../adr/0127-licence-polyform-noncommercial.md).

![Publish the framework: tag a release, push more than three tags, the guard, and checking a release from outside](diagrams/publish-the-framework.svg)

The framework ships as 31 NuGet packages on GitHub Packages, at `https://nuget.pkg.github.com/bosbevk/index.json`.
They are the projects a product built on the framework needs: the engine, the evals, the API and accounts, the web,
the diagrams, the general packs, and the two test helpers. Each package has a version of its own, so releasing
`LouisAgent.Core` doesn't move `LouisAgent.Web`. A product such as Puls takes them like any other NuGet package.

A release is a git tag. The tag is the package's name, a dash and the version, `LouisAgent.Core-0.2.0`, and it sits on
a commit of `main`. Pushing it starts the *Publish packages* workflow, which packs that project, checks the packages
it depends on, and pushes it to the feed with the run's own token. No secret is stored anywhere. MinVer reads the
version from the tag, so nothing in a project file names a version. This guide walks through one release, then
several at once, then what can stop one, and how to check a release the way a product would use it.

## Release one package

1. Make sure the change is merged to `main` and CI passed on it. A tag runs the workflow its commit holds, so a tag on
   an unmerged branch would publish code nobody reviewed.
2. Tag `main` and push the tag:

   ```bash
   git fetch origin
   git tag LouisAgent.Core-0.2.0 origin/main
   git push origin LouisAgent.Core-0.2.0
   ```

3. Watch *Publish packages* in the repository's Actions tab. Its three steps pack the package, check what it depends
   on, and push it; each pushed package logs "Your package was pushed."

The push skips a version the feed already holds (`--skip-duplicate`) and never replaces it. So a broken release is
followed by the next version, `0.2.1`, never by `0.2.0` again.

## Release several together

When a change reaches across packages, say a new contract in `LouisAgent.Composition` that `LouisAgent.Web` uses, both
need a new version, and the one that depends on the other must find it published. Tag both on the same commit and push
the tags together. GitHub starts a run per tag, in no set order, so every run publishes **all** the packages tagged on
its commit as one batch, and the runs after the first find nothing left to push.

**GitHub makes no event at all when more than three tags are pushed at once** (its docs: "Events will not be created
for tags when more than three tags are pushed at once"). The tags arrive and no workflow runs. Because one run
publishes the whole commit's batch, the cure is to make one event: delete one of the tags on GitHub and push it again.

```bash
git push origin LouisAgent.Core-0.2.0 LouisAgent.Web-0.2.0 LouisAgent.Diagrams-0.2.0 LouisAgent.Api-0.2.0
git push origin :refs/tags/LouisAgent.Core-0.2.0 && git push origin LouisAgent.Core-0.2.0
```

The first release of all 31 at `0.1.0` went out exactly this way.

## What stops a release

**A tag that names no package.** The name before the first dash must be one of the 31 packable projects. A tag such
as `LouisAgent.Site-1.0.0` names a project that isn't a package, and the run stops, naming it.

**A dependency that isn't published.** A project reference becomes a package dependency on "this version or higher"
of the project it references, at the version that project has on the tagged commit. Before anything is pushed, the
guard reads each packed package's framework dependencies and looks for each one in the run's own batch, then on the
feed. One that is in neither stops the run with every missing one named, and nothing is pushed. Tag the missing ones
too, on the same commit, and push again.

**Why an untagged neighbour is fine.** MinVer runs with `MinVerIgnoreHeight`, so a package with no tag on the release
commit keeps the version of its last tag. Releasing `LouisAgent.Web-0.3.0` on a later commit makes it depend on
`LouisAgent.Composition 0.2.0`, which is published, and not on a pre-release nobody can restore.

## Check a release from outside

*Consume packages* is the check a product would make. It writes a project in a folder outside any checkout that knows
only GitHub Packages and nuget.org, restores `LouisAgent.Core` and `LouisAgent.Testing` at the version given, with
nothing but the run's own token, and runs an agent on the fake chat client. The run passes when the agent's answer
comes back. Run it from the Actions tab with the version just released. It also runs on any pull request that changes
it.

GitHub Packages asks for a token even for a public package. In a workflow, the run's `GITHUB_TOKEN` with
`packages: read` is enough. On a PC, a personal access token with `read:packages` goes in the project's `nuget.config`.

The repository's own tests make the same check without the feed: `LouisAgent.Packages.IntegrationTests` packs the 31
into a folder at a version of its own and runs an agent from them, on every pull request.

## Later: nuget.org at 1.0

The packages stay on GitHub Packages while the framework is at `0.x`, where a version can be withdrawn. At the first
stable `1.0` they move to nuget.org, where nothing is ever deleted. nuget.org accepts only licences approved by the
Open Source Initiative or the Free Software Foundation as an SPDX expression, so the PolyForm licence will travel as a
licence file in the package there.

---
[Docs index](../README.md) · Related: [ADR 0129](../adr/0129-a-project-is-named-for-its-namespace.md) · [ADR 0127](../adr/0127-licence-polyform-noncommercial.md) · [Write and run tests](write-tests.md)
