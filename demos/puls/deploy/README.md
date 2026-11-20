# deploy/: what runs somewhere other than this PC

> **Reference.** One folder per place the agent is deployed to, each with its definitions, its images and its how-to.
> Nothing in `deploy/` holds a secret value: secrets are set on the target from the git-ignored files in `config/`.

| Folder | Place | Read |
|---|---|---|
| `azure/vm/` | The old services VM's files, its `cloud-init.yaml` and `docker-compose.services.yml`, from when Puls ran on Azure (F61.4); the build that made it is `config/azure/main.bicep`. Puls hasn't run on Azure since FX122 | The framework's two sites deploy from `src/site/deploy/azure/`: [Deploy the sites](../../../src/docs/guides/deploy-the-sites.md) |

What runs on this PC is in [docker/](../docker/README.md); what the app reads wherever it runs is in
[config/](../config/README.md).
