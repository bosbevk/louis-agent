---
name: site-map
description: What each page of louis-agent.dev shows, and the docs it was written from. Read it before you explain what a page says, walk someone through a page, or answer from a page's sources.
metadata:
  level: agent
  load: on-demand
---
# The site you sit on

You are in the corner of every page of louis-agent.dev. Each page was written from the docs, so a question about
what a page says goes to its source. A question about how the framework is built is answered from the docs
themselves (your docs-map skill says where), since no page here explains the framework:

- `/`, the landing, offer first: a website with an agent, what the agent does for a business, where it runs, three
  businesses' agents at work, custom agents, and at the end of the tour a way to leave their details for a quote.
  Sources: `README.md`,
  `src/docs/projects/louis-agent.site.md`.
- `/framework/theme`: the theme's component guide: every control a site is built from, live, in the look the
  visitor picked. Sources: `src/web/louis-agent.web/Components/Site/Guide/`, ADR 0125.
- `/contact`: how to reach the project. The site's own words.
- `/demos`: the three businesses' demos in one place, each a real run recorded once and played back. The coding
  agent and the triage team are not on this page: they have their own, below. Send someone who asks what the
  framework can do, or for a demo, here, and explain each demo from its doc: what it shows, how it was made, what
  is real and what it would mean for their business. Sources: `demos/louis-agent/docs/demos.md`.
- `/guesthouse-demo`, `/restaurant-demo`, `/law-practice-demo`: three made-up businesses' agents (Seemeeu
  Guesthouse in Yzerfontein, Wheatfield Kitchen, Koringland Attorneys), each a real conversation recorded once and
  played on a mock of the business's own website: a customer asks, the agent answers from the business's own facts
  and passes a booking on after their yes, the guesthouse knows which room takes the dog, and the law practice never
  gives advice. The best answer to "what would it do for a business like mine?": send a restaurant, a lawyer or a
  guesthouse to theirs. Sources: `src/docs/projects/louis-agent.identity.showroom.md`.
- `/coding-agent`: the coding agent fixing a real bug, recorded once and played back in the chat's own view: it runs
  a small invoice matcher's tests, reads the code, fixes two bugs and runs the tests again, for about R0.10. Nothing
  runs while a visitor watches. Send someone who asks what the coding agent does here. Sources:
  `demos/louis-agent/docs/demos.md`, `src/docs/projects/louis-agent.site.md`, ADR 0108.
- `/triage-demo`: the triage team at work, recorded once and played back step by step: the made-up order service
  failed on a retired discount code, and the sorter routes it, the manager decides, the investigator finds the cause,
  the coder fixes it on a branch with a test, the code checks it, and the reviewer keeps it. The steps are on the
  page, so explain the run from them when asked what happened. Sources: `demos/louis-agent/docs/demos.md`,
  `demos/louis-agent/docs/projects/louis-agent.identity.triage.md`.
- `/chat` is the coding agent's chat, behind a sign-in, and not yours to run: say it is for the people the project
  signs in, and answer the question about the framework instead.
