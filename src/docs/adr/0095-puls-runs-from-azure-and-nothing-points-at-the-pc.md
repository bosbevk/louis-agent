# 0095. Puls runs from Azure, and nothing points at the PC

**Status:** Accepted · **Date:** 2026-11-07 (Louis's picks during F61 and F61.4: a VM for Mongo and the browser, a
Container Apps environment on a private network, Azure SQL's free offer, the build as a Bicep file in config, every
secret a Key Vault reference; "remove the old ts-forwarders when we are done") · **Recorded:** 2026-11-08 ·
**Supersedes** the reasons of F58 (the sites served from this PC through Cloudflare tunnels)

## Context

Until F61.4, Puls in Azure reached back to Louis's PC for its browser and its benchmark store through a Tailscale
sidecar (CL19), its ingress locked to one IP because the app answered without a key, and `mypuls.co.za` served a
placeholder from the PC through a Cloudflare tunnel (F58), since every chat spent the API key and there were no
accounts. A Container App can't hold Mongo (its disk is a file share, on which Mongo's engine fails), and the
browser's DevTools port must never be public, which is why the services had stayed at home. With accounts in place
(F61.1 to F61.3) and the key gate taking a signed-in person, the app could face the internet, and the services could
move to where the app is.

## Decision

1. **Puls runs from Azure in its own private network.** `puls-vnet` holds two subnets: the Container Apps
   environment `puls-env` on one, delegated to it, and the VM `puls-services` on the other, with no public IP and a
   security group that lets only the apps' subnet in on 27017, 9222 and 8080. The VM runs Mongo, the headless browser
   and SearXNG in Docker from one compose file, every port bound to its private address, Mongo's files on a data
   disk that outlives the VM. The accounts are on Azure SQL's free offer, reached over TLS with "Allow Azure
   services". Nothing in Azure points at the PC, and nothing on the PC serves a domain.
2. **The build is one Bicep file in config**, `demos/puls/config/azure/main.bicep`: a deployment creates what is
   missing and updates what exists, so the file is the record of what Azure holds, and its outputs (the VM's
   address, the environment, the SQL server, the identity, the vault) are what the script's verbs read back, never a
   value copied by hand. A verb is only what a template can't do: files to the VM, images, the app's revisions, the
   domain's certificate, the secrets.
3. **Every secret value is in Key Vault**, put there by `deploy.ps1 secrets` from the git-ignored config files; the
   app's secrets are references by URL, read with a user-assigned identity the template makes and grants Key Vault
   Secrets User; the yaml's secrets block is written from the names the vault holds, so it never names a secret the
   vault lacks and never holds a value. The SQL password and the SSH key reach the template as parameters from
   environment variables the verb sets.
4. **The old stays until the new answers, then goes in one step.** The sidecar, the SearXNG app, the placeholder, the
   tunnels and their keys were deleted, in Azure and in the repo, only after both domains served from Azure.
5. **The framework's static site is not a deployment of its own** (Louis, 2026-11-08): it merges into the
   louis-agent demo's web app as its layout page, keeping the site's theme, and that app is then deployed the way
   Puls is. `louis-agent.dev` points wherever that lands.

## Consequences

- The month's bill is the VM and its two disks, the one always-on replica, the registry's Basic tier and the
  workspace's first 5 GB; the database is free within its offer. Three always-on containers of the old deploy went
  with it. The real month is written in the deploy README after the first one.
- What the build taught, kept in the deploy README and the script: a multi-line script to `az vm run-command` goes
  as a file; Windows PowerShell turns a native command's redirected error text into a terminating error, so a read
  that may be empty runs under its own guard; a Container App's name is unique in its group and an app can't change
  environment; Azure's Ubuntu 24.04 image runs a 7.0 kernel on which MongoDB 8 refuses to start (SERVER-121912), so
  the VM runs Mongo 9 while the PC's stack keeps 8; an apex managed certificate validates with its own `_dnsauth`
  TXT record, the `asuid` record only proves the hostname; the libraries' `sasscompiler.json` had to be marked
  build-time only or `dotnet publish` refused (FX62).
- The VM is reachable by nobody from outside: Studio 3T has no path to Mongo (Tailscale on the VM, a public IP with
  SSH from one address, or Bastion were offered and declined for now); a look at the collections is a `mongosh`
  one-liner through run-command. Management Studio reaches Azure SQL once `deploy.ps1 sql-access` has let the PC's
  address through the server's firewall.
- Found and left as TODO rows: a guest should be sent to sign in before the chat (F107), and the admin portal should
  be a role of a signed-in person rather than a key (F108).

## Links

- [Deploying Puls to Azure](../../../demos/puls/deploy/azure/README.md) (the how-to),
  [the architecture in diagrams](../../../demos/puls/deploy/azure/ARCHITECTURE.md), [the deployment view](../architecture/07-deployment-view.md)
- [ADR 0074](0074-the-folders-say-what-ships.md) (a demo holds its own deploy), [ADR 0087](0087-identity-is-the-identity-server.md)
  and [ADR 0088](0088-a-tenant-on-every-account.md) (the accounts this build hosts), the parent plan's research record
  (why a VM, why not Container Apps for Mongo, the free offer's limits), kept in the F61 plan until it closes

---
[Docs index](../README.md) · Related: [ADRs](README.md) · [Puls on Azure](../../../demos/puls/deploy/azure/README.md)
