# Setup: Puls

> **Reference.** Puls's own files, first run and settings. **Elsewhere:** how a host loads settings, and every setting the
> code reads, in the framework's [Configuration](../../../src/docs/SETUP.md); what each file here is in
> [config/](../config/README.md).

Puls keeps its settings in `demos/puls/config/`. The stack sets `CONFIG_DIRECTORY` to it, and so does the API's `puls` profile.

## Files

| File | In git | Purpose |
|---|---|---|
| `.env` | ❌ git-ignored | Your non-secret settings |
| `.env.example` | ✅ tracked | Template for `.env`, with explanations |
| `.env.secrets` | ❌ git-ignored | Your API keys and other secrets |
| `.env.secrets.example` | ✅ tracked | Template listing every secret |
| `.env.mongo` | ❌ git-ignored | The benchmark store's root user, read only by `puls-mongo` (F31) |
| `.env.mongo.example` | ✅ tracked | Template for `.env.mongo` |
| `.env.searxng` | ❌ git-ignored | The secret of the SearXNG we run, read only by `puls-searxng` (F32); the stack won't start without it |
| `.env.searxng.example` | ✅ tracked | Template for `.env.searxng` |
| `.env.anthropic`, `.env.ollama-puls` | ✅ tracked | LLM profiles: the model settings `LLM_PROFILE` picks |
| `prices.json`, `rates.json` | ✅ tracked | Model prices and fallback exchange rates; the coding agent keeps a copy, held equal by a test |

## First run

```bash
cp demos/puls/config/.env.example demos/puls/config/.env
cp demos/puls/config/.env.secrets.example demos/puls/config/.env.secrets     # at least ANTHROPIC_API_KEY and AGENT_API_KEY
cp demos/puls/config/.env.mongo.example demos/puls/config/.env.mongo
cp demos/puls/config/.env.searxng.example demos/puls/config/.env.searxng
docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build
```

Open <http://127.0.0.1:5070>. After changing a secret, recreate the container with `up -d`; `restart` keeps the old environment.

What the stack keeps stays in this demo, git-ignored: `demos/puls/data/` (the eval store and what runs record) and
`demos/puls/logs/` (run logs and the usage ledger).

## Debugging in Rider

The API's `puls` profile (port 5071) sets what the stack sets, plus `IDENTITIES=puls`, its peers, roles, SearXNG at
`127.0.0.1:8888` and the browser at `127.0.0.1:9222` from this stack. `EVALS_MONGO_URL` in `.env.secrets` names `puls-mongo`,
the compose service, so for a local run set it to `127.0.0.1` in the shell. The web app is the program's project reference.

## Puls's own settings

These are read by this stack or by the packs Puls loads.

| Setting | Meaning |
|---|---|
| `PULS_PORT`, `PULS_WORKSPACE` | Puls (`demos/puls/docker/docker-compose.puls.yml`): its port (default `5070`, bound to `127.0.0.1`) and the folder mounted as its workspace (default the repo) |
| `PULS_MONGO_PORT`, `EVALS_MONGO_DATABASE` | The eval store (F31): `puls-mongo`'s host port (default `27017`, on `127.0.0.1`) and its database (the API's default `evals`; Puls's compose, its Azure app and its Rider profile name `puls_benchmarks`, where its runs are; `BENCHMARKS_MONGO_DATABASE`, the old name, is still read; the Mongo contract tests use `puls_benchmarks_test`) |
| `SEARXNG_URL` | The SearXNG we run (F32): `http://puls-searxng:8080` in the Puls stack (set there); from the host, for its live tests, `http://127.0.0.1:8888` |
| `SHOP_PRICES_MONGO_URL` | The MongoDB the `shop-prices` pack keeps its price tables in (F50.2, ADR 0069; a secret, `config/.env.secrets`). Unset, the tables it ships are read from memory |
| `SHOP_PRICES_MONGO_DATABASE` | That store's database (default `shop_prices`; the collection is `price_files`) |
| `RECIPE_SOURCES` | The recipe sites the `recipes` toolset may search (F35), comma-separated slugs from the pack's table (`foodandhome,thelazymakoti`), or `*` for every site in it. Unset, the toolset is off; it needs `BROWSER_URL` too, and a site searched through the web needs a search provider (ADR 0023) |
| `MONGO_INITDB_ROOT_USERNAME`, `MONGO_INITDB_ROOT_PASSWORD` | In `config/.env.mongo`, not here: the benchmark store's root user (`puls-mongo`, F31), created the first time it starts with an empty `data/mongo`; changing them later needs the folder emptied. Their own file, so the database container never sees the other keys |
| `EVALS_MONGO_URL` (was `BENCHMARKS_MONGO_URL`, still read; the Mongo contract tests read only the old name) | How Puls reaches the eval store: `mongodb://{user}:{password}@puls-mongo:27017/?authSource=admin` (the service name). From the host, for the Mongo contract tests, the same with `127.0.0.1`, set in the shell. Without it the portal starts no runs |

---
[Docs index](README.md) · Related: [Configuration](../../../src/docs/SETUP.md) · [Deploying to Azure](../deploy/azure/README.md)
