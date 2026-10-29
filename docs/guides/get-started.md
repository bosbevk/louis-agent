# Get started

> **How-to.** Build and run the agent on your own machine for the first time: the settings files, the build, the
> tests, the CLI, and the two Docker stacks. **Elsewhere:** what each setting means in `config/.env.example`; the
> models in [MODELS.md](../MODELS.md); the hosts in their [design docs](../projects/README.md).

Requires the .NET 10 SDK.

```bash
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets   # add ANTHROPIC_API_KEY and AGENT_API_KEY
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
dotnet run --project src/louis-agent.cli               # interactive CLI
```

Docker (web app + API on http://127.0.0.1:5080):

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

Puls (its web app on http://127.0.0.1:5070):

```bash
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
```

Always pass `--env-file config/.env`; compose needs it for `REPOSITORIES_PATH`. After changing
`config/.env.secrets`, recreate the containers with `up -d`: `restart` keeps the old environment, so the new key
never arrives.

When something refuses to start, [Common errors](common-errors.md) has the messages and their fixes; for logs and
the usage ledger, [Debug and logs](debug-and-logs.md).

---
[Guides](README.md) · [Docs index](../README.md)
