# The framework's site's settings

> **Reference.** What `src/site/config/` holds for the framework's website (F124) and its host, `louis-agent.site-api`.
> **Elsewhere:** running it in [the site's compose file](../docker/docker-compose.yml), the coding agent's settings in
> [its demo's config](../../../demos/louis-agent/config/README.md).

| File | In git | What it holds |
|---|---|---|
| `.env.anthropic` | yes | The guide's model: Claude Haiku 5.5 through the Anthropic API |
| `.env.example` | yes | The optional settings, to copy into `.env` |
| `.env` | no | Your optional settings |
| `.env.secrets.example` | yes | The secrets the site needs, with no values |
| `.env.secrets` | no | `ANTHROPIC_API_KEY`, `AGENT_API_KEY`, and `RESEND_API_KEY` for the lead mail |
| `prices.json`, `rates.json` | yes | The site's own price and rate tables for its usage ledger; the image carries them |

The site's `.env.secrets` is its own: copy `.env.secrets.example` and fill in the values. The guide answers only with
`ANTHROPIC_API_KEY` set, and the host starts only with `AGENT_API_KEY`.

---
[Docs index](../../docs/README.md) · Related: [Get started](../../docs/guides/get-started.md)
