# Recordings

> **How-to.** What the site's recorded coding session runs on, and how to record it again. **Elsewhere:** why the
> public watches a recording instead of the live agent in [ADR 0108](../../../src/docs/adr/0108-the-public-watches-the-coding-agent-recorded.md);
> the component that plays it in [the web's design doc](../../../src/docs/projects/louis-agent.web.md).

The site's `/coding-agent` page plays `wwwroot/recordings/coding-session.sse` from the code-review app: the stream the
API sent while the coding agent fixed a real bug, saved as it came. Nothing runs when a visitor watches it.

**The sample.** [`samples/invoice-matcher/`](../samples/README.md#invoice-matcher) is a small C# library that matches
bank payments to invoices, with two bugs its own tests catch: a lower-case reference is missed, and `INV-104` takes the
payment meant for `INV-1042`. It sits outside the solution on purpose, so its failing tests never run in CI.

**Recording again.** It is one paid run on Haiku, a few cents, so ask Louis first. Build the API image, then run the
script from the repo root:

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env build api
bash demos/louis-agent/recordings/record.sh
```

The script copies the sample into a fresh git repository and runs the API's own image on it, open on localhost only,
with the model key and nothing else from `config/`. The host reads every `.env` file in its config folder, so the
script never mounts `config/` itself. It saves the stream over the old recording. Read the new file for anything
private before you commit it. If the question changes, change it on the page too.

---
[Docs index](../docs/README.md) · Related: [the site's project doc](../docs/projects/louis-agent.code-review.md)
