# FX21 · The browser reads shops' pages for a user without saying what it is

> **Status:** open · **Area:** core, the headless browser (`Tools/Web/Browsing/ChromeWebBrowser`), its settings;
> Puls's compose and Azure yaml · **Found:** 2026-10-24, the review of the CL19 branch · **Planned:** one step, so no
> plan file of its own; after CL19 is merged

## Symptom

The cook run of 2026-10-21 had some shop pages refuse the headless shell, and the follow-up that week made the browser
pass as a person's desktop Chrome: a desktop user agent, and `navigator.webdriver` answered `false` to a page's script.
Both answered a site's questions falsely. Louis's view, and the one this fix keeps: reading a page on a user's behalf is
legitimate (that is what a user agent is), so the browser should say so rather than hide. CL19 took the two lines out
again (the viewport stays); with them gone the browser sends Chrome's own headless user agent, which is truthful but
says nothing about who is reading or why, and gives a retailer nothing to allow or to contact.

## Cause

The browser has no notion of whose errand it runs. It is one shared process reading for every user, from one place,
and the only identity it can send is Chrome's own. Core may name no product (CL15, ADR 0027), so the honest string
cannot be written into the browser code either: it has to arrive as a setting from the identity that runs it.

## Fix

One step, test first:

- **`BROWSER_USER_AGENT`**, a new browser setting read with the others (`BROWSER_URL`, `PAGE_RENDERER`): when set,
  every page the browser opens sends it; unset, the browser sends Chrome's own, as it does today. The
  `navigator.webdriver` marker is never touched.
- **Puls sets it** in `docker/docker-compose.puls.yml` and `deploy/azure/puls-app.yaml`:
  `Puls/1.0 (+https://puls-app.…/about; reads a page on a user's behalf)`, the conventional form for a service that
  reads for its users: who is reading, where to learn more, and why. The code-review stack sets nothing.
- **Not the person.** The user agent reaches every site the page touches, trackers included, and lands in their logs;
  a name there is personal information sent to third parties for no gain (POPIA). The user's town says nothing new
  (the cook already gives the shop the town to find its branch) and would be useful context, but it needs the visit to
  receive the user's place as the shop tools receive `area`: a later step, its own TODO line.
- **Docs:** `config/.env.example` and `docs/SETUP.md` get the setting; the Azure how-to names the string.

## Tests

- `ChromeWebBrowserTests`: a visit with the setting sends that user agent (the fake page reads it back); a visit
  without it sends none of ours; `navigator.webdriver` is whatever the browser reports.
- `AgentOptionsTests` (or the browser options' tests): the setting is read, and empty means unset.
- The golden files don't move: no tool, description or prompt changes.

## Confirmed

Open. When done: one visit to a shop's page from the Puls stack, the request's `User-Agent` read from SearXNG's or the
shop's response headers where visible, else from a page the fake serves; and the next cook run's sourced-price count
beside the run of 2026-10-21, to see what the honest agent loses, if anything.

## Lesson

A disguise is a decision about the product, not a rendering fix: whether the agent tells sites what it is decides
what a retailer finds in their logs and whether the sold endpoint can say it never pretended. The principles that open
every prompt bind the code around the model too.

---
[Fixes](README.md) · [CL19 plan](../plans/CL19-config-by-place.md) · [ADR 0023](../adr/0023-copyleft-service-beside-us.md)
