# F170 The framework's own Bicep

A plan, for Louis's approval: a Bicep file of the framework's own that describes what louis-agent.dev and
framework.louis-agent.dev run on in Azure, so the environment they live in can be rebuilt from code, and the trust
page's *Rebuilt from code* can say it's in place again. Nine steps, one commit each, on
`feature/F170-the-frameworks-bicep`. Louis made four picks on 2026-11-22 (*Louis's picks*), and the plan is drawn on
them.

![The overview: one Bicep file on Microsoft's verified modules, our own names in a parameters file, checked on every pull request, proved against what stands, then the trust page in place](diagrams/F170-the-frameworks-bicep-overview.svg)

## Context

The two sites run in the resource group `rg-puls-za`, in the Container Apps environment `puls-env`. That
environment, and most of what sits around it, was built once from Puls's Bicep file, `main.bicep`, which left with
Puls on 2026-11-21 and now lives at `config/azure/main.bicep` in the `puls` repository. What the sites still need
from that build, `deploy.ps1` reads back from the outputs Azure recorded for it, under the deployment name
`puls-build`: the environment's id and its static address, the identity that reads the vault, and the vault's
address. So the sites release every merge, but nothing in this repository could make their environment again.
FX215 said so on the trust page: *Rebuilt from code* became *Not yet*, with "the environment they run in was built
once from code that is being retired, so rebuilding it from nothing isn't one command yet."

On 2026-11-20 Louis asked for this feature beside the packages: a Bicep file of the framework's own for what the two
sites use in Azure, so *Rebuilt from code* can be in place again.

What was looked up, on 2026-11-22. In Azure, read-only: the resource group holds the registry `acrpulsza` (Basic, no
admin user), the log workspace `puls-logs` (PerGB2018, 30 days), Application Insights `puls-insights`, the network
`puls-vnet` (10.0.0.0/16, with an `apps` subnet at 10.0.0.0/23 delegated to Container Apps and an empty `services`
subnet at 10.0.2.0/24 that held the old VM), the environment `puls-env` (on the `apps` subnet, not internal, the
Consumption profile, not zone-redundant, app logs to the workspace, and its telemetry agent sending logs and traces
to Application Insights), the vault `puls-keys` (Standard, role-based access, soft delete with 90 days, no purge
protection), the storage account `pulsstorage` (StorageV2, Standard_LRS) with the share `louis-agent-workspace`
(5 GiB) linked to the environment as `louis-agent-vol`, read and write, and the identity `louis-agent-app-identity`,
which holds *Key Vault Secrets User* on the vault. The apps pull their images as the environment's own identity
(`identity: system-environment` in their YAML), and that identity holds *AcrPull* on the registry. Three more
*AcrPull* rights point at identities that no longer exist. Puls's coming-soon page, `puls-coming-soon`, is Puls's,
and this file leaves it alone.

On the web: Azure's incremental mode, the default, leaves what a file doesn't name as it is, and `what-if` shows
what a deployment would create, change or delete before it runs
([deployment modes](https://learn.microsoft.com/en-us/azure/azure-resource-manager/templates/deployment-modes)). Azure
can't rename a resource ([naming](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming)).
Microsoft's Azure Verified Modules have a published module for each of the nine resources, at the versions read from
Microsoft's registry that day ([the index](https://azure.github.io/Azure-Verified-Modules/indexes/bicep/bicep-resource-modules/)),
referenced as `br/public:avm/res/<path>:<version>` ([Bicep modules](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/modules)),
from an MIT repository Microsoft maintains. In GitHub: the deploy workflow signs in with OIDC, as an identity trusted
only for pushes to `main`, which holds *Contributor* on the resource group and *Key Vault Reader* on the vault.
Contributor can't make the role assignments this file declares. On this PC, the Bicep CLI is 0.49.2.

What could not be confirmed is said where it matters: the modules' defaults against what stands are proved by
`what-if` in each step, and a rebuild from nothing is proved only if Louis chooses the throwaway run in
*Verification*.

## Louis's picks

**1. How the file is written: A, Microsoft's Azure Verified Modules, each pinned to a version.** One `module` per
resource from Bicep's public registry, chosen over plain Bicep resources of our own (B), which is what aged into a
preview API in Puls's file, and over Azure.Provisioning in C# (C), a new package and a program for the same Bicep.
The versions: environment 0.17.0, registry 0.13.2, vault 0.14.3, storage 0.33.2, identity 0.6.0, Application
Insights 0.8.0, log workspace 0.16.2 and network 0.10.2. The container-app module isn't used, since the apps stay
with `deploy.ps1` (pick 3).

**2. The names: A, as parameters, with today's names in one parameters file.** The file names nothing itself;
`main.bicepparam` beside it holds `puls-env`, `acrpulsza` and the rest for our own instance. Chosen over today's names
written into the file (B), which would put a product's name in a framework file, and over new names with everything
rebuilt beside the old (C), a migration with downtime risk, new certificates and a vault name held for 90 days.

**3. What the file covers: A, the infrastructure only.** The apps and their domains stay with `deploy.ps1`, which
releases them every merge from tested scripts; it reads this deployment's outputs in place of `puls-build`'s.
Chosen over the apps too (B), where a Bicep deploy and a release would both own the apps, and over the domains and
certificates too (C), whose one-pass binding is unconfirmed and which has been seen removing a domain a template
left out.

**4. How it's proved and run: A, CI builds and lints it on every pull request, and Louis deploys it by hand.** The
check runs offline with no secret; the first deploy is a `what-if` by hand against what stands; a deploy is
`az deployment group create`, run by Louis as Owner. Chosen over a `what-if` on every merge too (B), which needs a
rule for when drift fails a release, and over a deploy on every merge (C), which would give CI's identity the right
to grant rights.

## Design

### What the file makes

One file, `src/site/deploy/azure/main.bicep`, beside the script and the two apps' YAML, since it is the two sites'
infrastructure and nothing else. It takes every name as a parameter and declares, through the modules, what the
sites stand on, in the order each needs the one before it.

The log workspace comes first, and Application Insights on it. Then the network with its one `apps` subnet, delegated
to Container Apps, and the environment on that subnet, sending its app logs to the workspace and its telemetry
agent's logs and traces to Application Insights. Then the registry, with *AcrPull* for the environment's own
identity, which is how the apps pull their images. Then the vault, role-based, and the identity the apps read it
with, holding *Key Vault Secrets User* on it. Last, the storage account, its share, and the environment's link to
the share, which the apps mount for their logs. Its outputs are the ones `deploy.ps1` reads today, by the same names:
`environmentId`, `environmentStaticIp`, `codingAppIdentityId` and `vaultUri`.

`main.bicepparam` holds our own instance's values: today's names, the region `southafricanorth`, the address ranges,
and the share's 5 GiB. Another business or a spawned site writes its own parameters file and deploys the same Bicep.

![The architecture: the Bicep file and its parameters, the modules it calls from Microsoft's registry, and the resources each makes in the resource group, with deploy.ps1 reading the outputs](diagrams/F170-the-frameworks-bicep-architecture.svg)

### Matching what stands

A module's defaults are Microsoft's safe ones, and some differ from what stands: the vault's module turns purge
protection on, for one, where `puls-keys` has none, and once on it can never be turned off. So each step that adds a
resource runs `what-if` against `rg-puls-za` and sets every property the module would change to what stands. The
step is done when `what-if` shows nothing to create, nothing to delete, and no change but those Azure reports on
every run (its own read-only properties). A change that is truly wanted is put to Louis as a finding, never slipped in.

Two leftovers would show as deletions, since a file that describes what the sites use names neither: the empty
`services` subnet, and the three *AcrPull* rights of identities that no longer exist. Removing is a fix of its own, so
**FX221** takes them out first, by hand and on Louis's word, since it deletes in Azure. The subnet matters most: the
network's module declares its subnets whole, so a network without `services` would delete it on the first deploy.

### How it is deployed and released

A new verb, `deploy.ps1 infra`, runs `what-if` and shows it, then deploys the file under the deployment name
`louis-agent-infra` once Louis says yes at the prompt. The other verbs read their outputs from that deployment in
place of `puls-build`. The deploy needs the rights to make role assignments, so it runs from Louis's own sign-in,
never from CI; CI only builds and lints the file.

Rebuilding the sites from nothing is then four commands, each already scripted: `deploy.ps1 infra`, then
`deploy.ps1 secrets`, `release` and `domain` for each app, with the DNS records added in Cloudflare by hand, as today.
The deploy guide says it in that order.

![A deploy: Louis runs infra, sees what-if, says yes; Azure records the outputs; a release reads them](diagrams/F170-the-frameworks-bicep-communication-a-deploy-then-a-release.svg)

### The four pictures

![The interface design: the file's parameters, the modules it calls, its outputs, and the script verbs that read them](diagrams/F170-the-frameworks-bicep-interface-design.svg)

The *interface* here is the file's: its parameters, the modules it calls with the inputs each needs, and its outputs,
which are the contract `deploy.ps1` reads.

![Objects in Azure: one resource group, the environment holding the share's link and its own identity, the apps' identity reading the vault](diagrams/F170-the-frameworks-bicep-objects-in-azure.svg)

### Namespace map

| What | Folder | Step |
|---|---|---|
| `main.bicep` | `src/site/deploy/azure/` | 1 to 6 |
| `main.bicepparam` | `src/site/deploy/azure/` | 1 to 6 |
| `bicepconfig.json` (the linter's rules) | `src/site/deploy/azure/` | 1 |
| The CI job that builds and lints | `.github/workflows/ci.yml` | 1 |
| `BicepFileTests` (test) | `src/tests/site/LouisAgent.Site.Tests/Deploy/` | 1 to 6 |
| The `infra` verb, and the outputs read from `louis-agent-infra` | `src/site/deploy/azure/deploy.ps1` | 7 |
| `DeployFilesTests` (test), the verbs it allows | `src/tests/site/LouisAgent.Site.Tests/Deploy/` | 7 |
| `TrustFacts.IfSomethingBreaks`, *Rebuilt from code* | `src/site/LouisAgent.Site/Content/Trust/` | 8 |
| `TrustPageTests` (test) | `src/tests/site/LouisAgent.Site.Tests/Pages/About/` | 8 |

No C# type is added or moved; `TrustFacts` changes one fact.

### Folder trees

`src/site/deploy/azure/` today holds `deploy.ps1`, `louis-agent-app.yaml` and `louis-agent-framework-app.yaml`.
After, it also holds `main.bicep`, `main.bicepparam` and `bicepconfig.json`.

### What is reused

Microsoft's verified modules for every resource (pick 1), the Bicep CLI that the Azure CLI carries, Azure's own
`what-if`, the existing `deploy.ps1` and its verbs, the deploy guide, and the trust page's rule that a fact in place
is held to a file of the repository (`TrustPageTests`). No package is added to the solution.

## Steps

One commit each; a review stop after each. The Bicep's own check is `az bicep build`, and the test is a C# rule test
in `BicepFileTests`, written first and seen failing.

0. **Before step 1, FX221 on its own branch:** the empty `services` subnet and the three *AcrPull* rights of
   identities that no longer exist, removed by hand on Louis's word; its records in the fixes table, the work log and
   the lessons.
1. **The file, its parameters, and the CI check.** `main.bicep` with its parameters and the log workspace and
   Application Insights; `main.bicepparam` with today's names; `bicepconfig.json`; a job in `ci.yml` that runs
   `az bicep build` and `az bicep lint` on the file. Test: `BicepFileTests` holds that the file names no product (no
   `puls` in `main.bicep`), that every parameter it declares has its value in `main.bicepparam`, and that each
   module is pinned to a version. Then `what-if` shows the two resources unchanged.
2. **The network and the environment.** The `apps` subnet, delegated, and the environment on it with its logs and
   its telemetry agent. Test: the rule tests still hold; `what-if` shows both unchanged.
3. **The registry, and the environment's right to pull.** Test: `what-if` shows the registry and the role unchanged.
4. **The vault, and the apps' identity with its right to read it.** Purge protection and soft delete set to what
   stands. Test: `what-if` unchanged; `BicepFileTests` holds that purge protection is not turned on, since that can't
   be undone.
5. **The storage, its share, and the environment's link to it.** Test: `what-if` unchanged.
6. **The outputs.** By the names `deploy.ps1` reads. Test: `BicepFileTests` holds that every output the script reads
   is declared. Then one full `what-if` of the whole file against `rg-puls-za`: nothing to create, delete or change,
   kept in the commit's body.
7. **The `infra` verb, and the script reading `louis-agent-infra`.** `deploy.ps1 infra` shows `what-if` and deploys
   on a yes; `Get-Output` reads the new deployment. Then Louis runs `deploy.ps1 infra` once, which records the
   outputs, and a `release` of each app proves the script reads them. Test: `DeployFilesTests` allows the new verb
   and finds no `puls-build` left.
8. **The trust page in place.** *Rebuilt from code* turns *Done*, held to `src/site/deploy/azure/main.bicep`, with
   words that say what a rebuild is. Test: `TrustPageTests`, which holds a fact in place to a file that exists.
9. **The docs, and closed.** The deploy guide's *rebuild from nothing* in its four commands, the architecture's
   *deployment in Azure* and the site's design doc with their pictures, the write-up, and the plan folded and deleted
   (`/finish-a-feature`). If the decision should bind later work, an ADR is proposed to Louis first. Test: the docs'
   tests.

## Verification

- **The tests** of each step, and the full suite before the pull request, after merging `main`.
- **Free checks by hand:** `what-if` after steps 1 to 6, each one against what stands, and the release of both apps
  after step 7, with louis-agent.dev and framework.louis-agent.dev answering 200 after it.
- **A rebuild from nothing, on Louis's go:** the same file deployed to a throwaway resource group with names of its
  own in a second parameters file, an app released to it, then the group deleted. It spends a little, the registry's
  and the environment's hours, so it waits for his word, and its cost is reported after.
- **Done when:** `main.bicep` and its parameters describe everything the two sites stand on, `what-if` against
  `rg-puls-za` shows no change, `deploy.ps1` reads `louis-agent-infra` and both sites release from it, CI builds and
  lints the file on every pull request, and the trust page says *Rebuilt from code* is in place.

## Concepts

- **Infrastructure as code** (steps 1 to 6): what runs is described in a file that is checked, versioned and
  reviewed like code, so it can be made again. GOAL.md skill 18, *Testing and shipping*.
- **Reuse a maintained module** (pick 1): Microsoft keeps the API versions and safe defaults, and we pin the version
  so nothing changes under us. Skill 18.
- **Desired state and drift** (`what-if`, steps 1 to 6): the file says what should be, and Azure says how far what
  is differs. Skill 14, *Reliability*.
- **Least privilege** (pick 4): CI keeps only the rights a release needs, and the right to grant rights stays with a
  person. Skill 15, *Safety and security*.

## Approved during the build

Nothing yet.

## The records

In the commit that saves this plan: its row in the plans' index (`README.md` here), its row in `TODO.md` under
*Next*, and its row in the work log's open table, with the count above that table.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [Deploy the sites](../guides/deploy-the-sites.md) · [Deployment in Azure](../architecture/deployment-in-azure.md)
