# deploy/: what runs somewhere other than this PC

> **Reference.** One folder per place the agent is deployed to, each with its definitions, its images and its how-to.
> Nothing in `deploy/` holds a secret value: secrets are set on the target from the git-ignored files in `config/`.

| Folder | Place | Read |
|---|---|---|
| [azure/](azure/README.md) | Puls on Azure Container Apps (`rg-puls-za`): the app yamls, the Tailscale sidecar, `deploy.ps1` | [Deploying Puls to Azure](azure/README.md) |

What runs on this PC is in [docker/](../docker/README.md); what the app reads wherever it runs is in
[config/](../config/README.md).
