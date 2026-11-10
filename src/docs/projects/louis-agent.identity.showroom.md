# louis-agent.identity.showroom — design

> **Explanation.** Who the site's showroom agents are: three made-up businesses' agents for the demo pages, and how
> each answers and passes a booking on. **Elsewhere:** the tool they pass it on with in
> [louis-agent.pack.leads](../../../packs/docs/projects/louis-agent.pack.leads.md); the site they show on in
> [louis-agent.site](louis-agent.site.md); the decisions in [ADR 0112](../adr/0112-the-site-sells-first.md) and
> [ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md).

> **Kind:** Class library (`net10.0`) · **Path:** `src/site/louis-agent.identity.showroom/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.site-api` (the site's host), behind its key

## Purpose

A business owner understands a website with an agent best by watching one answer a business like theirs (F128.2,
Louis's pick 4A: "not a bakery, say a laywers practice, restaurant, and a doctors practice"). These are three
made-up businesses' agents, recorded once each answering a customer and passing a booking on, and played on the
site's demo pages. Nothing runs for a visitor.

## How it works

- `ShowroomIdentity`: the base the three share, its name from `ShowroomIdentityName`, its voice
  `{name}/voice`, and one pack, `leads`. `RestaurantIdentity` (Wheatfield Kitchen), `LawPracticeIdentity`
  (Koringland Attorneys) and `DoctorsPracticeIdentity` (Graanveld Family Practice) each say only which they are.
- `prompts/agent/{name}/voice.md`: the business's made-up facts (hours, services, prices), how it answers (three
  short sentences, proper English or Afrikaans, a greeting once), and how it passes a booking on: name, contact and
  what they want, repeated back, and `SendLead` only after the customer's yes. Each says it is an AI and a made-up
  business when asked. The two practices never give advice; the doctor's sends an emergency to 10177 or 112.
- The site's host lists them after Louis, so he stays the default and the only one open to visitors
  (`OPEN_IDENTITY`); a showroom session needs the site's key. The leads pack's texts name no owner, so the same tool
  that sends Louis a lead sends a practice its booking request.

## Tests

`ShowroomIdentitiesTests` in `src/tests/site/louis-agent.site.tests/Showroom/` holds each business's name, voice
and pack, the honesty and yes-first rules in every voice, the practices' never-advise rules, and the site's
catalogue order; `SiteCatalogTests` holds that no identity on the site reaches a pack the guide doesn't load.

---
[Docs index](../README.md) · Related: [louis-agent.site](louis-agent.site.md) ·
[louis-agent.identity.guide](louis-agent.identity.guide.md)
