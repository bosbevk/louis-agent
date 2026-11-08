# 0097. Puls deploys from GitHub without a password

**Status:** Accepted · **Date:** 2026-11-07 (Louis: "let's do a git hub action then", after the FX63 fix waited for
his PC to deploy; his three picks on cards the same evening) · **Recorded:** 2026-11-07

## Context

Puls runs from Azure ([ADR 0095](0095-puls-runs-from-azure-and-nothing-points-at-the-pc.md)), but every release ran
`deploy.ps1 release` on Louis's PC, the only place signed in to Azure. A fix merged while he was away (FX63, the
message box) stayed off mypuls.co.za until he was back at that PC. `release` needs nothing kept off git: the image
builds in the registry, the app's secrets are already Key Vault references, and the rest it reads from Azure.

## Decision

1. **A GitHub Actions workflow deploys Puls** (`.github/workflows/deploy-puls.yml`): by itself after CI passes on
   `main` (a `workflow_run` on CI, only for its push to `main`, only on success), and from the *Run workflow* button.
   It checks out the commit CI passed, not what `main` holds by then. One deploy at a time; a second waits, none is
   cancelled half-way.
2. **It signs in by OpenID Connect, with no password stored.** A federated credential on the app registration
   `github-puls-deploy` trusts tokens GitHub signs for `repo:bosbevk/louis-agent:ref:refs/heads/main` only. The three
   ids are repository variables, not secrets.
3. **It runs `deploy.ps1 release` as it is,** in PowerShell 7 on the Ubuntu runner, so the PC and GitHub deploy the
   same way and there is one script to fix. The script's `\` paths resolve on Linux PowerShell (checked).
4. **Least privilege:** Contributor on `rg-puls-za` (build, update the app, read the deployment's outputs) and Key
   Vault Reader on `puls-keys` (the secrets' names, which `update` lists); nothing at subscription scope, no secret's
   value. `infra`, `vm`, `secrets` and `domain` stay on the PC.
5. **The rules hold it:** `DeployWorkflowTests` (no password, release only, after green CI on main, never cancelled).

## Changed, 2026-11-08 (FX93)

The deploy no longer waits for a CI run on `main`: it runs on the push that a merge is, and CI runs on pull requests
only (Louis: "we do not need a build and test on deploy, we only need it pull request build and test"). Every pull
request is tested with `main` merged in before it can merge, so a second run of the same tests on the merge commit
bought a quarter of an hour's delay and nothing else. The deploy checks out the merge commit itself.

## Consequences

- A green merge reaches mypuls.co.za in the time the image takes to build, without anyone's PC.
- Louis merging before CI ends no longer ships a red merge: the deploy waits for CI's verdict (until FX93: since
  then a merge needs a green pull request, and the deploy follows the merge at once).
- A run from any branch but `main` fails at sign-in: the trust names `main` only.
- The setup is once, by hand, in the deploy README (an app registration is outside the resource group the Bicep
  build owns). The framework's site (F61.6) gets its own trigger or job when it deploys beside Puls.
- Actions are pinned by major tag, as CI's are; pinning every workflow by commit SHA is one change for both.

## Links

- [Deploying from GitHub](../../../demos/puls/deploy/azure/README.md#deploying-from-github-f109) (the setup and the
  roll-back), [ADR 0095](0095-puls-runs-from-azure-and-nothing-points-at-the-pc.md), [ADR 0030](0030-build-or-adopt.md)
- [azure/login](https://github.com/Azure/login),
  [OpenID Connect in Azure](https://docs.github.com/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)

---
[Docs index](../README.md) · Related: [ADRs](README.md) · [Puls on Azure](../../../demos/puls/deploy/azure/README.md)
