# Deploying the site to Azure Container Apps

> **How-to.** What runs in the cloud for `louis-agent.dev`, how to build and deploy it, and how the domain gets its
> certificate. **Elsewhere:** the site itself in [louis-agent.site](../../docs/projects/louis-agent.site.md), Puls's
> deploy in [demos/puls/deploy/azure](../../../puls/deploy/azure/README.md), the PC's tunnel it replaces in
> [docker/](../../docker/README.md) (kept until this is live), why it is Azure in the F61.4 plan until its ADR.

Everything here is in resource group `rg-louis-agent-za` (South Africa North). The site is static Razor pages that
spend nothing and keep nothing, so there is no secret, no store and no key anywhere in this deploy. **Nothing here
has run yet** (2026-11-08): the files are built, the first `infra` waits for Louis.

## What exists in Azure, once `deploy.ps1 infra` has run

| Resource | Name | What it is | Made by |
|---|---|---|---|
| Resource group | `rg-louis-agent-za` | The site's own group, apart from Puls's | `deploy.ps1 infra` |
| Registry | `acrlouisagentza` (Basic) | `louis-agent-site`, built by `deploy.ps1 build` from `demos/louis-agent/docker/images/Dockerfile.site` | the Bicep build |
| Log Analytics | `louis-agent-logs` | The environment's console logs, 30 days | the Bicep build |
| Environment | `louis-agent-env` | A Container Apps environment with a Consumption profile; its identity pulls from the registry | the Bicep build |
| Container App | `louis-agent-site` | The site on 8080 with `/health`; scales to zero when nobody reads it | `deploy.ps1 release` from `site.yaml` |

The Bicep build is `demos/louis-agent/config/azure/main.bicep`: the record of what Azure holds, deployed as
`louis-agent-build`, which creates what is missing and updates what exists.

## First time

1. `az login`, and `az bicep install` once; Docker is not needed, the registry builds the image.
2. `deploy.ps1 infra -WhatIf` to see what would be made, then `deploy.ps1 infra`.
3. `deploy.ps1 release`: builds the image (tagged with the date and time) and creates the app as revision `v001`.
4. `deploy.ps1 domain -WhatIf` prints the DNS records; put them in Cloudflare, DNS-only (grey cloud: a proxied record
   blocks the certificate):

   | Type | Name | Value |
   |---|---|---|
   | A | `@` | the environment's static IP, as printed |
   | TXT | `asuid` | the app's domain verification id, as printed |
   | CNAME | `www` | the app's own address (`louis-agent-site.<env>.southafricanorth.azurecontainerapps.io`) |
   | TXT | `asuid.www` | the same verification id |

   When they resolve (`nslookup louis-agent.dev`), `deploy.ps1 domain` adds the hostname and asks for its managed
   certificate, then `deploy.ps1 domain -Hostname www.louis-agent.dev` for `www`. The certificate takes a few minutes.
5. Then, and not before: the Cloudflare tunnel `louis-agent-site` and the PC's `louis-agent-production` compose
   group go (`docker compose -p louis-agent-production down`, the tunnel deleted in Zero Trust), with the compose
   file and its token line in the repo.

## Every-day deploy

```powershell
deploy\azure\deploy.ps1 release     # build (tagged date and time) and deploy the next revision, in one go
deploy\azure\deploy.ps1 logs        # -Follow to tail
```

`release` is `build` then `update` with the revision after the one Azure runs; the two verbs stay for a deploy of
an image already built. The app is up when `https://louis-agent.dev/health` answers 200.

## Cost

The environment's Consumption profile bills by the second while a replica runs, and the app scales to zero; the
registry's Basic tier and the workspace's first 5 GB a month are the fixed part. The real month is written here after
the first one.

---
[Docs index](../../../../src/docs/README.md) · Related: [Deployment view](../../../../src/docs/architecture/07-deployment-view.md) · [Puls on Azure](../../../puls/deploy/azure/README.md)
