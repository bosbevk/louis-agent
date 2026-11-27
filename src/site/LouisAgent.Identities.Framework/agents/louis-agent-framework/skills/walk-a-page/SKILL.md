---
name: walk-a-page
description: How you walk a developer through the page they are on, one section per answer, or explain one section they pick. Read it when they ask for a walkthrough or a section, and when they come back to one.
metadata:
  level: agent
  load: on-demand
---
# Walking a page with them

A walkthrough goes one section per answer, in the page's order. One section is the same stop on its own.

## Never

1. **Never ask it in words.** Every stop's next step is a choice on a card.
2. **Never describe several sections in one answer without showing them.**

## Steps

1. Show that section with `ShowPage`, on its own, and wait for it to answer.
2. Then explain that section alone in two short sentences, as one developer to another, and end with the doc it was
   written from. What later sections show waits for their own stop, so a stop never runs on into the next one.
3. After those words, end with `Offer` and three choices: on to the next section, by its name (suggested), explain
   another section, or Stop here.

A stop is two replies, because `Offer` ends your answer: the first shows the section, the second says it and
offers the choices. Called in the same reply as `ShowPage`, it would send the card before any words.
4. When they say Stop here, say goodbye in a line and offer nothing.

## Coming back to it

When they come back to a walkthrough, look at the page they are on now. On the page you were walking, carry on from
the next section. On another page, say so in a few words and walk that page from its top.

## An example

On `/how-it-works` a developer asks for the walkthrough. The first stop shows one turn through every layer and says
"Your message goes to the API, and the engine runs one turn on it. Its tool loop asks the model and the packs until
the answer is ready." Then you offer the next section by its name, explain another, and Stop here.

## References

- Your `louis-agent-framework/site-pages` skill, the pages and their sections
- [`src/docs/projects/LouisAgent.Web.Framework.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/projects/LouisAgent.Web.Framework.md), how a page is put together
