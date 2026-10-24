# 0038. The browser says what it is: it never disguises itself to a site

**Status:** Accepted · **Date:** 2026-10-24 (Louis, after the review of CL19) · **Recorded:** 2026-10-24

## Context

The headless browser reads shops' and recipe sites' pages for Puls's users (F32, F33). After a cook run on 2026-10-21
in which some shop pages refused it, a follow-up made it pass as a person's desktop Chrome: a desktop user agent, and
`navigator.webdriver` answered `false` to a page's script. Both answered a site's questions falsely. The branch's review
raised it, and the question was argued through: reading a page on someone's behalf is legitimate (that is what a user
agent is), but the purpose does not make the answers true, and Puls is one shared browser reading for every user, from
one place, which is exactly what a site's check asks about. The principles that open every prompt say never lie or
cheat ([0020](0020-principles-for-every-agent.md)); the code around the model is bound by them too.

## Decision

- The browser never disguises itself. It sends either Chrome's own user agent or a name that says what it is, and it
  never alters what the browser reports about automation (`navigator.webdriver` and the like).
- An identity that reads for users names itself through `BROWSER_USER_AGENT` (FX21), in the form services that read
  for their users use: who reads, a public page where a site owner can learn what is read and how to reach us, and why
  (`Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/docs/SITE_OWNERS.md; reads a page on a user's behalf)`).
  Core carries no such name (0027).
- The string never carries a person: it reaches every site and tracker a page touches. The user's town may be added
  later, as the shop tools already pass it; a name or an address never.
- A site that refuses automated readers refuses this one, and that is its call. The fallbacks stay what they are: the
  catalogue tools, and estimates that say so. Access a site grants is asked for through the page in
  [SITE_OWNERS.md](../SITE_OWNERS.md), never taken by pretending.

## Consequences

- Some pages that render for a disguised browser won't for this one; the next cook run measures what that costs in
  sourced prices, and the answer may be a retailer's feed or permission (the TODO's *More shops*), not a disguise.
- The viewport is set to a normal screen size, which is not a disguise: it changes how a page lays out, not what the
  browser says it is.
- Bot detection is an arms race this project is not in; what the browser sends is one tracked setting, reviewed like
  any other.

## Links

- [FX21 The browser says what it is](../fixes/FX21-the-browser-says-what-it-is.md): the setting, the tests, what was
  confirmed.
- [CL19](../plans/CL19-config-by-place.md), *Approved during the build*: the disguise taken out.
- [0020](0020-principles-for-every-agent.md): the principles; [0023](0023-copyleft-service-beside-us.md): the sold
  endpoint keeps licensed sources.
