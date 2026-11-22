# 0131. The sites' Azure is one Bicep file a person deploys

**Status:** Accepted · **Date:** 2026-11-22 · **Recorded:** 2026-11-22

![One Bicep file on Microsoft's verified modules, our names in a parameters file, proved by what-if, deployed by a person, read by every release](diagrams/0131-the-sites-azure-is-one-bicep-file-a-person-deploys.svg)

## Context

The framework's two sites ran in an environment built once from Puls's Bicep file, and that file left with Puls
(F166). Every merge still released the apps, but nothing in this repository could make what they run on again. A new
file had to describe infrastructure that was already running the live sites, so a deploy of it would change whatever
it got wrong.

Four choices had to be made (F170). How the file is written: Microsoft's Azure Verified Modules, plain Bicep of our
own, or Azure.Provisioning in C#. Its names: parameters with today's names in one file, today's names written into
the Bicep, or new names with everything rebuilt beside the old. What it covers: the infrastructure only, the apps
too, or the domains and certificates as well. How it is proved and run: built in CI and deployed by a person, a
`what-if` on every merge too, or a deploy on every merge.

## Decision

What the framework's sites stand on in Azure is one Bicep file, `src/site/deploy/azure/main.bicep`, deployed by a
person.

- **Microsoft's Azure Verified Modules, each pinned to a version**, are the parts. A part whose module can't say what
  stands is plain Bicep at a stable API version, with the reason beside it: today the environment, whose module has no
  setting for its telemetry agent, and the network, whose module would rename a subnet's delegation.
- **The file names nothing itself.** Every name is a parameter, and our own instance's values are in
  `main.bicepparam` beside it, so the same file builds another business's sites and names no product (ADR 0076).
- **It covers the infrastructure and the rights between its parts**, never the apps or their domains. The apps are
  released on every merge by `deploy.ps1`, which reads the environment, the identity and the vault from this
  deployment's outputs, so a release and the file can't disagree about where an app runs.
- **A deploy is proved before it runs.** Azure's `what-if` against what stands shows nothing to create and nothing
  to delete, and any change is explained; a module's default is read in its own code and set to what stands wherever
  it differs.
- **A person deploys it**, through `deploy.ps1 infra`, which shows the `what-if` and deploys on a yes, as
  `louis-agent-infra`. The file gives rights, and CI's identity never gets the right to grant them; CI builds and
  lints the file on every pull request, offline, and deploys nothing.

## Consequences

- The sites can be made again from code: the file, then each app's secrets, release and domain, as
  [Deploy the sites](../guides/deploy-the-sites.md#rebuilding-from-nothing) says. A rebuild beside the old group
  needs new names, since the registry's, the vault's and the storage's are global and a deleted vault keeps its name
  for 90 days.
- A module's version moves only by a change to the file, proved by `what-if` again; the linter's nag about newer
  versions is off, since pinning is the point.
- A part made by hand in Azure is a difference the next `what-if` shows, so something that leaves takes its rights
  and its network with it, or the file can't describe what stands (FX221, FX223).
- The safest defaults are not the safest change to something running: purge protection, a firewall that denies every
  address and an environment closed to the internet are each right for a new resource and harmful here, so the ones
  that would hurt most are held by `BicepFileTests`.
- The infrastructure changes only when a person runs the deploy, so a drift between the file and Azure lasts until
  then.

## Links

- F170, the framework's own Bicep; [Deploy the sites](../guides/deploy-the-sites.md),
  [The sites in Azure](../architecture/deployment-in-azure.md)
- [ADR 0076](0076-core-ships-no-agent.md), core ships no agent and names no product
- [ADR 0097](0097-puls-deploys-from-github-without-a-password.md), releases from GitHub without a password
- Azure Verified Modules: <https://azure.github.io/Azure-Verified-Modules/> · Deployment modes and what-if:
  <https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/deployment-modes>

---
[Docs index](../README.md) · [ADRs](README.md) · Related: [0130](0130-the-framework-is-published-as-packages-one-tag-each.md) · [0097](0097-puls-deploys-from-github-without-a-password.md)
