# Puls in Azure: the architecture

> **Explanation.** What runs where for `mypuls.co.za`, how the parts reach each other, where a secret travels and
> where a request goes, in diagrams. **Elsewhere:** the how-to in [README.md](README.md), the build's record in
> `demos/puls/config/azure/main.bicep`, the why in ADR 0096, the whole system's deployment view in
> [07-deployment-view](../../../../src/docs/architecture/07-deployment-view.md). Live since 2026-11-07 (F61.4).

## What runs where

One resource group, `rg-puls-za` in South Africa North. The app is the only thing with a public address; the three
services sit on a VM that nothing outside the network can reach; the accounts are on Azure SQL's free offer; every
secret value is in the vault.

```mermaid
flowchart LR
    person["A person's browser"] -->|"HTTPS mypuls.co.za, www (managed certificates)"| app
    subgraph rg["rg-puls-za"]
        subgraph vnet["puls-vnet 10.0.0.0/16"]
            subgraph apps["apps subnet 10.0.0.0/23, delegated to Container Apps"]
                env["puls-env (Consumption profile, static IP 4.253.73.61)"]
                app["puls-app: the Puls image, one container, revision vNNN"]
                env --- app
            end
            subgraph services["services subnet 10.0.2.0/24, NSG: 27017, 9222, 8080 from the apps subnet only"]
                vm["VM puls-services (Ubuntu 24.04, Standard_B2s, no public IP)\nDocker: mongo:9 :27017, headless-shell :9222, searxng :8080\ndata disk, XFS at /data"]
            end
        end
        sql["Azure SQL puls-sql-za: puls_accounts (free offer, pauses when idle)"]
        vault["Key Vault puls-keys: every secret value"]
        identity["puls-app-identity: Key Vault Secrets User"]
        insights["Application Insights puls-insights, on puls-logs"]
        registry["Registry acrpulsza: louis-agent-puls images"]
        share["pulsstorage: the share puls-app-workspace (config/, logs/, data/)"]
    end
    app -->|"private IP 10.0.2.4"| vm
    app -->|"TLS, Allow Azure services"| sql
    app -.->|"reads with"| identity
    identity -.-> vault
    app -->|"OpenTelemetry agent"| insights
    env -->|"AcrPull"| registry
    app -->|"mounted at /workspace"| share
```

## The build and the verbs

The template is the record: one deployment, `puls-build`, creates what is missing and updates what exists; the
verbs do what a template can't.

```mermaid
flowchart TB
    bicep["config/azure/main.bicep + main.bicepparam"] -->|"deploy.ps1 infra (what-if first)"| deployment["deployment puls-build: the network, the VM, the environment, insights, SQL, the identity"]
    deployment -->|"outputs: vmPrivateIp, environmentId, environmentStaticIp, sqlServerFqdn, appIdentityId, vaultUri"| verbs
    subgraph verbs["the verbs read the outputs back"]
        vm["vm: the compose, SearXNG's settings and the env files to the VM through run-command"]
        secrets["secrets: the values into the vault"]
        release["release: build the image in the registry, then update"]
        update["update: puls-app.yaml filled (tag, revision, the VM's IP, the environment, the identity, the vault's secrets, the providers), create or update"]
        domain["domain: the DNS records printed, the hostname added, the certificate asked for"]
    end
```

## A secret's path

No value is ever in a file under git or on the app: it goes from the git-ignored config files into the vault, and
the app's secrets are references the identity resolves.

```mermaid
sequenceDiagram
    participant L as Louis's PC (config/.env.secrets, .env.mongo, .env.sql)
    participant S as deploy.ps1 secrets
    participant V as Key Vault puls-keys
    participant U as deploy.ps1 update
    participant A as puls-app (identity puls-app-identity)
    L->>S: the values, and the VM's IP and the SQL server from the outputs
    S->>V: one secret per value (mongo-url and accounts-sql-url built here)
    U->>V: which names exist?
    U->>A: a secrets block of keyVaultUrl references, env lines only for secrets that exist
    A->>V: read each reference with the identity (Key Vault Secrets User)
```

## A chat, from the domain to the model

```mermaid
sequenceDiagram
    participant B as Browser
    participant CF as Cloudflare DNS (DNS-only records)
    participant A as puls-app
    participant SQL as Azure SQL
    participant VM as puls-services
    participant M as Anthropic
    B->>CF: mypuls.co.za?
    CF-->>B: 4.253.73.61 (A), www a CNAME to the app's own address
    B->>A: GET / over HTTPS
    A-->>B: the Puls web app
    B->>A: POST /account/login
    A->>SQL: the user (the database resumes after a pause; EF retries)
    A-->>B: Set-Cookie
    B->>A: POST /v1/sessions (cookie + X-Requested-With)
    A->>VM: 27017 Mongo (runs, prices, recipes), 9222 Chrome, 8080 SearXNG
    A->>M: the model's answer
    A-->>B: streamed
```

## What the network lets through

| From | To | Port | Allowed by |
|---|---|---|---|
| The internet | `puls-app` | 443 | the environment's ingress, with the managed certificates |
| The internet | the VM | any | nothing: no public IP, and the NSG denies all inbound but the rule below |
| The apps' subnet | the VM | 27017, 9222, 8080 | the NSG rule `apps-to-services` |
| Louis's IPv4 | the VM | 22 | only while `infra -Ip` set it; `infra` without `-Ip` removes it |
| `puls-app` | Azure SQL | 1433 | the server's "Allow Azure services" rule, TLS 1.2 |
| Louis's IPv4 | Azure SQL | 1433 | the server's rule `home`, from `deploy.ps1 sql-access` |

---
[Docs index](../../../../src/docs/README.md) · Related: [README](README.md) · [Deployment view](../../../../src/docs/architecture/07-deployment-view.md)
