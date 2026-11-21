# Get started

> **How-to.** Build and run the agent on your own machine for the first time: the settings files, the build, the
> tests, the CLI, and the two Docker stacks. **Elsewhere:** what each setting means in each demo's
> `config/.env.example`; the models in [MODELS.md](../MODELS.md); the hosts in their [design docs](../projects/README.md).

![Your first run: the settings, the build and test, and the CLI, then the coding agent's Docker stack](diagrams/get-started.svg)

You need the .NET 10 SDK. Everything below runs from the repo's root folder.

## Build and run the coding agent

![The four steps: copy the settings, build and test, start the CLI, turn on the commit template](diagrams/get-started-flow.svg)

1. **Copy the settings files** of the coding agent's demo, and put your `ANTHROPIC_API_KEY` and `AGENT_API_KEY` in
   the secrets file:

   ```bash
   cp demos/louis-agent/config/.env.example demos/louis-agent/config/.env
   cp demos/louis-agent/config/.env.secrets.example demos/louis-agent/config/.env.secrets
   ```

2. **Build the solution, and run core's tests:**

   ```bash
   dotnet build louis-agent-solution.sln
   dotnet test src/tests/engine/LouisAgent.Core.Tests
   ```

3. **Start the interactive CLI:**

   ```bash
   dotnet run --project demos/louis-agent/hosts/LouisAgent.CodingCli
   ```

4. **Turn on the commit template**, so `git commit` shows you the shape a commit message takes:

   ```bash
   git config commit.template .gitmessage
   ```

## Run the Docker stacks

The coding agent's web app and API answer on `http://127.0.0.1:5082`:

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api
```

Always pass `--env-file demos/{demo}/config/.env`, because compose needs it for `REPOSITORIES_PATH`. When you change
`config/.env.secrets`, recreate the containers with `up -d`. A `restart` keeps the old environment, so the new key
never arrives.

## When something goes wrong

If something refuses to start, [Common errors](common-errors.md) lists the messages and their fixes. For the logs and
the usage ledger, see [Debug and logs](debug-and-logs.md).

---
[Guides](README.md) · [Docs index](../README.md)
