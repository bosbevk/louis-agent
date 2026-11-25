# FX249 An environment without a network

A plan: how the two sites move from an environment joined to our own network, which Azure bills a load balancer and
its public IP for, to one on no network of ours, in a resource group of their own, so about R370 a month stops. This
file says what changes, then gives its seven steps.

![FX249: the two apps move to a group of their own on an environment with no network, the shared parts stay, old and new run side by side until the switch, then the old is deleted](diagrams/FX249-environment-without-a-network-overview.svg)

## Context

Azure's cost analysis on 2026-11-25, read by meter for the seven days to 2026-11-24: the load balancer's "Standard
Included LB Rules and Outbound Rules" R71 and its "Standard IPv4 Static Public IP" R14, R85 a week, about R370 a month,
the biggest line after the app's own time once FX248 took the builds off. Louis, of it: "we do it now".

What was looked up, on 2026-11-25:

- **Why it costs.** A workload-profile environment on a network of your own is billed a standard load balancer and
  its static public IPs on top of the apps ([billing](https://learn.microsoft.com/en-us/azure/container-apps/billing),
  [custom virtual networks](https://learn.microsoft.com/en-us/azure/container-apps/custom-virtual-networks)). Ours,
  `puls-env`, sits on `puls-vnet`'s `apps` subnet, there for Puls's VM, which is gone.
- **Why it can't be switched off.** An environment's network type can't be changed after it is made
  ([networking](https://learn.microsoft.com/en-us/azure/container-apps/networking)), so it means a new environment.
- **What ties to the environment** (`main.bicep`, `deploy.ps1`): the environment pulls the apps' images as itself,
  by a role assignment whose name is kept for life; the apps' share is linked to it by name (`louis-agent-vol`); its
  managed certificates for the three names are its own; and `deploy.ps1` reads its id and static IP from the infra
  deployment's outputs. An app's name is unique in its group and an app can't change environments, so `deploy.ps1
  update` refuses one that exists in another environment.

Louis's pick, 2026-11-25, of three ways to move (same names with an outage, new names beside the old, or a new
group): **a new resource group**. The apps keep their names, the old ones serve until the new ones answer, and the
switch is a DNS change.

## Design

### 1. A group of the apps' own

A new resource group, `rg-louis-agent-za`, holds the new environment, `louis-agent-env`, and the two apps, under their
names today. The environment is `puls-env` without its `vnetConfiguration` and its infrastructure group: the Consumption
profile alone, open to the internet, its telemetry agent and log workspace as before. Everything the apps share stays
in `rg-puls-za`: the registry, the vault, the storage and its share, the identities and Application Insights. An app
reaches them across groups as it does today across names.

![What the Bicep describes: main.bicep keeps the shared parts and deploys a module into the apps' group, the environment with no network and its share link; the registry gives it the right to pull, and the release identity gets Contributor on the new group](diagrams/FX249-environment-without-a-network-interface-design.svg)

### 2. The Bicep

`main.bicep` stays scoped to `rg-puls-za` and deploys a new module, `apps-environment.bicep`, into the apps' group: the
environment, its share link with the storage's key, and the two outputs `deploy.ps1` reads, `environmentId` and
`environmentStaticIp`, by the names they have always had. The registry's role assignments gain AcrPull for the new
environment's identity, under a name of its own, and the release identity gains Contributor on the new group, so the
workflow can create the apps there. The old environment, its share link, its pull right and the network leave the
file; Azure's incremental deploy leaves them standing until they are deleted at the end. Every name is a parameter
with its value in `main.bicepparam` (ADR 0131).

### 3. The script and the workflow

`deploy.ps1` names two groups: `$ResourceGroup`, the shared parts and the infra deployment, as today, and
`$AppsGroup`, where every `containerapp` command and the environment's certificates go. `infra` makes the apps' group
first, then deploys the Bicep with its what-if. The release workflow's summary lists the revisions in the apps' group.

![The architecture: Cloudflare points the names at the apps in the new group; they read the shared registry, vault and share in rg-puls-za; the old environment, its load balancer and IP and the network are deleted at the end](diagrams/FX249-environment-without-a-network-architecture.svg)

### 4. The move

The order is what keeps the sites up. Louis runs `deploy.ps1 infra` from this branch before it merges, so the new
group, environment and rights exist; the merge's own release then creates both apps in the new group, beside the old
ones, which keep serving every visitor. For each name, `deploy.ps1 domain` adds it to the new app and prints the
records; `louis-agent.dev` validates its certificate by a TXT record, so its certificate can be issued before its A
record moves, while `www` and `framework` validate by their CNAME, so theirs come in the minutes after their record
moves. Once both sites answer over HTTPS from the new environment, Louis deletes the old apps, `puls-env` (which takes
its load balancer, IP and managed group with it) and `puls-vnet`.

![The move: Louis runs infra, merges, the workflow releases both apps into the new group while the old serve; domain per name, the records in Cloudflare, then the old pieces deleted](diagrams/FX249-environment-without-a-network-communication.svg)

![What stands where during the move: both environments run, the old apps serving and the new ones waiting, both reading the same share](diagrams/FX249-environment-without-a-network-objects.svg)

### 5. What changes, where

| Type or file | Folder | Step |
|---|---|---|
| `apps-environment.bicep` (new), `main.bicep`, `main.bicepparam` | `src/site/deploy/azure/` | 2 |
| `BicepFileTests` (changed) | `src/tests/site/LouisAgent.Site.Tests/Deploy/` | 2 |
| `deploy.ps1` (changed: `$AppsGroup`, `infra` makes the group) | `src/site/deploy/azure/` | 3 |
| `DeployFilesTests` (changed) | `src/tests/site/LouisAgent.Site.Tests/Deploy/` | 3 |
| `deploy-sites.yml`'s summary, `DeployWorkflowTests` | `.github/workflows/`, the tests' `Deploy/` | 4 |
| *Deploy the sites*, its part on the move | `src/docs/guides/` | 5 |

### 6. What is reused

No new package or module: the environment stays a plain resource (its module has no setting for the telemetry agent),
the registry's and the role's modules are the ones `main.bicep` pins today, and the move runs on the verbs
`deploy.ps1` has, `infra`, `release` and `domain`. The deletes at the end are `az` commands in the guide, run by
Louis, since they can't be undone.

## Steps

Each step is one commit with its test, written first and seen failing, where the step changes a file a test holds.
Stop after each for Louis's review.

### Step 1: the plan is kept

This plan and its five pictures, its row in the plans' index, FX249's rows in the fixes table, the TODO and the work
log's open table. Checked by the Docs tests and `LouisAgent.DocDiagrams.Tests`.

### Step 2: the apps' environment in its own group, in Bicep

The tests first, in `BicepFileTests`: no `vnetConfiguration` anywhere; the environment is in `apps-environment.bicep`,
deployed with the apps' group as its scope; its share link and its telemetry agent go with it; the registry gives the
new environment's identity AcrPull and the release identity has Contributor on the apps' group; the network is gone
from the file; every new name a parameter with its value. Then the Bicep. Checked too by CI's Bicep check, which
builds the file.

### Step 3: the script names the apps' group

The tests first, in `DeployFilesTests`: every `containerapp` command and the environment's certificates take
`$AppsGroup`; `infra` makes the group before the deploy; the outputs keep their names. Then `deploy.ps1`.

### Step 4: the workflow's summary reads the apps' group

The test first, in `DeployWorkflowTests`: the summary lists the revisions in `rg-louis-agent-za`. Then the workflow.

### Step 5: the move, written down

A docs step: *Deploy the sites* gains the move as numbered steps, in the order above, with each command and what to
check after it, and the deletes at the end with what each removes. The design doc of the site's deploy says where each
part lives now.

### Step 6: the move, done

Louis runs it, in the order of step 5: `infra` from the branch, the merge, `domain` for each name, the records in
Cloudflare, the check, the deletes. Then the check is read back here, free: both sites answer `200` over HTTPS,
`az containerapp show` names `louis-agent-env` for both apps, and `puls-env`, `capp-svc-lb` and `puls-vnet` are gone.
What it shows goes in the fix's record.

### Step 7: the close

The fixes table's row done with its cause and its confirmation, the work log, the lesson, the docs of what moved, and
this plan deleted.

## Verification

No paid run. The Bicep check in CI builds the file; Louis's `infra` shows Azure's what-if before it deploys.

**Done when:** louis-agent.dev, www.louis-agent.dev and framework.louis-agent.dev answer over HTTPS from
`louis-agent-env` in `rg-louis-agent-za`; `puls-env`, its load balancer and IP and `puls-vnet` are deleted; and a day
later Azure's cost analysis shows no load balancer or public IP line.

## Concepts

| Step | What it teaches | Skill in [GOAL.md](../GOAL.md) |
|---|---|---|
| 2 | Infrastructure as code across two scopes, and what a fixed setting costs to change | 18 Testing and shipping |
| 4, 6 | A move with no outage: the new beside the old, then the switch | 18 Testing and shipping |
| all | Reading the bill by meter, and paying only for what is used | 13 Cost and token economics |

## Approved during the build

Nothing yet.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [Deploy the sites](../guides/deploy-the-sites.md)
