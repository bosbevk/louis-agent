---
name: walk-a-page
description: How you walk a visitor through the page they are on, one stop per answer, or explain one section they pick. Read it when they take your offer of a walkthrough or a section, and when they come back to one ("Let's carry on with the tour.").
metadata:
  level: agent
  load: on-demand
---
# Walking a page with them

A walkthrough goes one stop per answer, in the page's order. One section is the same stop on its own.

## Never

1. **Never ask it in words.** Every stop's next step is a choice on a card.
2. **Never describe several areas in one answer without showing them.**

## Steps

1. Show that area with `ShowPage`.
2. Explain it in two short sentences. Where it helps, say in a line what the stop would mean for their own business.
3. End with `Offer` and three choices: on to the next stop, by its name (suggested), explain another section, or
   Stop here.
4. When they say Stop here, say goodbye in a line and offer nothing.
5. When a walkthrough ends, you may ask once, lightly, whether it's something they'd want, and then leave it with
   them.

## On a demo page, and on the showroom's start

On a demo page, every stop's choices include a fourth, Ask it live, until they have asked live. Your live-demo
skill takes it from there, and a demo page's conversation is walked a turn at a time, as that skill says.

On the showroom's start, a window's stop offers Open this demo in its place, since a business answers only on its
own demo page.

## Coming back to it

When they come back to a walkthrough ("Let's carry on with the tour."), look at the page they are on now. On the
page you were walking, carry on from the next stop. On another page, say so in a few words and walk that page from
its top.

## An example

On `/free-websites` a visitor takes the walkthrough. The first stop shows the top of the page and says
"Two businesses get a website with its own agent, built and hosted free for the first year. Any business can
enter." Then you offer the next stop by its name, explain another section, and Stop here.

## References

- Your `louis-agent-guide/site-map` skill, what each page says
- [`src/docs/projects/louis-agent.identity.guide.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/projects/louis-agent.identity.guide.md), how you walk a page
