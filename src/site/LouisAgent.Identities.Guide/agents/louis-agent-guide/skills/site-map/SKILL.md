---
name: site-map
description: What each page of louis-agent.dev shows. Read it before you explain what a page says, walk someone through a page, or send someone to the page that answers them.
metadata:
  level: agent
  load: on-demand
---
# The site you sit on

You are in the corner of every page of louis-agent.dev. Each entry below says what a page shows, so a question about a
page is answered from its entry, and the page is the link you give. No page here explains how the framework is built:
that is answered from the docs themselves, and your docs-map skill says where.

## Never

1. **Never say a page shows what its entry here doesn't say.** Read the entry first.
2. **Never call a planned feature built.** What is planned is said to be planned.
3. **Never run `/chat` for anyone.** It belongs to the coding agent, behind a sign-in.

## The pages

- `/`, the landing, offer first: a website with an agent, then what the agent does for a business, where it runs,
  three businesses' agents at work, custom agents, who builds it (Louis, with his photo and a way to his CV), and at the end of the tour a way to leave their
  details for a quote.
- `/framework/theme`: Theme in the menu. Every button, switch and card a website is built from, working, in the
  look the visitor picked; a visitor can press and try each one.
- `/contact`: how to reach the project. The site's own words.
- `/privacy`: what the site keeps and sends when someone talks to you, where it runs, and their rights under
  POPIA, with the Information Regulator for a complaint. Send someone who asks what happens to their words or
  their details here. The site's own words.
- `/trust`: the technical facts a business owner asks before trusting the site, in five parts (where it runs, your
  data with every company that handles it, AI safety, if something breaks, and what a business should ask for before
  it pays), each fact marked In place or Not yet. Send someone who asks about security, POPIA, backups, encryption
  or what you may do here, and say plainly what is Not yet. The site's own words.
- `/demos`: the three businesses' demos in one place, each a real run recorded once and played back. Send someone who asks what
  the framework can do, or for a demo, here, and explain each demo from its doc: what it shows, how it was made,
  what is real and what it would mean for their business.
- `/guesthouse-demo`, `/restaurant-demo`, `/law-practice-demo`: three made-up businesses' agents (Seemeeu
  Guesthouse in Yzerfontein, Wheatfield Kitchen, Koringland Attorneys), each a real conversation recorded once and
  played on a mock of the business's own website: a customer asks, the agent answers from the business's own facts
  and passes a booking on after their yes, the guesthouse knows which room takes the dog, and the law practice never
  gives advice. The best answer to "what would it do for a business like mine?": send a restaurant, a lawyer or a
  guesthouse to theirs.
- `/chat` is the coding agent's chat, behind a sign-in, and not yours to run: say it is for the people the project
  signs in, and answer the question about the framework instead.

## An example

A visitor on `/trust` asks you to walk them through it. You read this map's entry for the
page first, so each stop says what the page really says, then you walk it.

## References

- [`src/docs/projects/LouisAgent.Site.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/projects/LouisAgent.Site.md), the site's design doc
