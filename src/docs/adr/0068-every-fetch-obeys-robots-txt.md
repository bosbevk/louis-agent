# 0068. Every fetch obeys robots.txt: a site's no is checked in code, on every request

**Status:** Accepted · **Date:** 2026-11-03 (decided, F35 plan approved on the recommended picks; H2 and I1 on
2026-11-04) · **Recorded:** 2026-11-04

## Context

The browser says what it is ([ADR 0038](0038-the-browser-says-what-it-is.md)), but nothing in core reads what a site
says back. `robots.txt` is the file where a site tells automated readers which paths to leave alone. The cook's prompt
carries a hand-kept list of nine sites that refuse readers, which code can't check and a prompt can forget. Louis's
pick (F35, finding E3): the check belongs on every fetch, not on a person at the moment a site is turned on.

Two things the plain rules didn't settle. The first is which group of the file binds us: RFC 9309 picks the group for
our own name, else `*`. But a site that blocks GPTBot and ClaudeBot by name and lets `*` in has said it doesn't want AI
readers, and we are one (H2). The second is that a page can say the same thing itself, in a meta tag or a response
header, with no `robots.txt` involved (I1).

## Decision

*Every page core fetches or opens for a tool obeys the site's `robots.txt`, and the page's own no-AI signals.* It is
checked in code at the one place every request passes, so no prompt has to keep a list.

- **Which rules bind us (H2, N2).** The group for `louis-agent` if the file has one. Otherwise the `*` group and the
  groups of the AI readers it names (GPTBot, ClaudeBot, anthropic-ai, Claude-Web, CCBot, Google-Extended,
  Applebot-Extended, Bytespider, PerplexityBot, Meta-ExternalAgent), all together, the strictest of them: a path any
  of them may not read, we don't read, and each group is read on its own, so one group's Allow never undoes another's
  Disallow. An AI-reader group never takes the place of `*`, so what a site closes to everyone stays closed to us
  (N2). This goes beyond the RFC on purpose. The list is code, in `RobotsTxt`, so a change to it is a commit someone
  reviews, not a setting.
- **How a rule is read.** The longest pattern that fits the path and query wins, `Allow` on a tie, none fitting means
  allowed; `*` is any run of characters and a final `$` the end. A plain walk, no regular expression
  ([ADR 0057](0057-regex-is-the-last-option.md)). `Crawl-delay` and sitemaps stay with the collecting job.
- **When it is read.** Once a day per site (scheme, host and port), through the recorded fetcher, so a replay never
  opens the network. `robots.txt` itself is always read and never checked. A missing file (4xx) allows all. A file we
  couldn't read (5xx, no answer) refuses the site's pages for an hour, then it is tried again (G1).
- **The page's own signals (I1).** After the fetch, before the page is read, the answer is checked for four signals
  that need no extra request: a robots meta tag with `noai`, an `X-Robots-Tag` header with `noai`,
  `Content-Signal: ai-input=no` (Cloudflare's content signals) and `tdm-reservation: 1`. `ai-train=no` alone does not
  refuse us: we read a page to answer a question, we don't train on it, so a site that only says no to training is
  one we may read. `noimageai` is about pictures and doesn't refuse either.
- **Where it sits.** One `RobotsPolicy` per process, carried on `ToolsetContext` beside the browser and the replay
  store. `PageFetcher` asks it on the first address and on every redirect hop, beside the address guard, and checks
  the final answer; `RobotsWebBrowser` (a decorator, as `RecordedWebBrowser` is) asks it before a visit and reads the
  page's meta tag after. It sits outside the recorded browser, so a replay of an old trial that has no `robots.txt`
  recording misses loudly instead of passing as allowed ([ADR 0049](0049-nothing-is-compatible-before-a-sale.md)).
  The policy reads `robots.txt` with a fetcher that has no policy, so the check can't loop.

The refusal reaches the model as text, with the reason, as a private address does: `RobotsDisallows`,
`RobotsUnreachable` and `PageRefusesAiReaders`.

## Consequences

- A site that says no is never read by any tool, whatever a prompt says; the cook's hand-kept list goes in F35.2.
- One more request per site per day, and one per hour for a site whose file is down.
- A recorded trial from before this needs re-recording, since its `robots.txt` was never saved (ADR 0049: no lenient
  path). The cook eval's old recordings are re-recorded in F35.2's paid run.
- A site that blocks by status (403) or a captcha isn't `robots.txt`'s business: that stays a refused page, as today.
- The policy checks the page the browser is asked to open, not the images, scripts and frames that page loads: those
  are what any visitor's browser loads for it. A page Chrome is redirected to on another host is not checked again.
- The list of AI readers and the four signals are ours to keep up as the web changes.

## Links

The plan, [F35-recipe-sources.md](../../../demos/puls/docs/plans/F35-recipe-sources.md) (finding E3, G1, H2 and I1), step 2 of F35.1.
[RFC 9309](https://datatracker.ietf.org/doc/html/rfc9309), the Robots Exclusion Protocol. Also
[ADR 0005](0005-guard-the-context.md) (failures carry their reason) and [ADR 0067](0067-a-pages-own-data-before-its-text.md).

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
