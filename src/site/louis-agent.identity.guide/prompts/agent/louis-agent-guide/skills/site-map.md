---
name: site-map
description: What each page of louis-agent.dev shows, and the docs it was written from. Read it before you explain what a page says, walk someone through a page, or answer from a page's sources.
metadata:
  level: agent
  load: on-demand
---
# The site you sit on

You are in the corner of every page of louis-agent.dev. Each page was written from the docs, so a question about
what a page says goes to its source. Every page that explains a part shows an animated diagram of it near the top (an
identity's parts, a pack's shape, the repository's groups, one turn through the engine, your program, where a number
comes from, what is built on the framework), so when words are not enough, point at the page's diagram:

- `/`, the landing, offer first: a website with an agent from R500 a month, what the agent does for a business,
  how it works for them (three steps, then why to trust it), custom agents and the demos. Then one band of proof: it is
  built on a framework of its own, measured, with links into the framework's pages. Sources: `README.md`,
  `src/docs/projects/louis-agent.site.md`.
- `/framework/how-it-works`: the long film, one turn through every layer of core; the parts in the order a turn meets
  them; the rules every turn keeps. Sources: `src/docs/architecture/06-runtime-view.md`,
  `src/docs/architecture/08-crosscutting-concepts.md`, the ADRs 0004, 0005, 0007 and 0008.
- `/framework/architecture`: the short film of the whole framework in six scenes, the four concepts (the engine is
  how, an identity is who, a pack is what, a workflow is planned), and every project that ships, by family, as the
  repository is laid out. Sources: `src/docs/architecture/05-building-block-view.md`,
  `src/docs/projects/README.md`, each demo's `docs/projects/`
  and `packs/docs/projects/`.
- `/framework/identities`: what an identity holds, the principles every agent reads first shown as the engine holds
  them, and the three that exist. Sources: the ADRs 0017 to 0020 and 0027,
  `demos/puls/docs/projects/louis-agent.identity.puls.md`, `src/docs/projects/louis-agent.identity.guide.md`.
- `/framework/packs`: every pack and the rules a pack keeps. Sources: `packs/docs/projects/`, the ADRs 0027, 0028
  and 0073.
- `/framework/build-your-own`: the coding agent's real program and catalogue, whose is what, where the packages
  stand. Sources: `src/docs/guides/build-an-identity.md`, `src/docs/guides/build-a-pack.md`,
  `src/docs/guides/spawn-a-site.md`, ADR 0076.
- `/csharp-ai-agent-framework`: louis-agent as an AI agent framework in C#, beside Microsoft Agent Framework and
  Semantic Kernel, and which to pick. Lead with what sets it apart: it runs on your own hardware, in Docker with a
  local model through Ollama if you want, with no cloud agent service in between. Say of Microsoft's frameworks only
  what the page says, which is what their own documentation says, and never that one needs a cloud service. Sources:
  the page's own words and the Microsoft pages it links.
- `/framework/measured`: the numbers, in dollars and rand, the one rule the startup runs on (first principles: start
  from what is certainly true, measure before you assume, write down why, adopt what is proven and build only what
  nobody has) and what drives it (a stoic and Kierkegaard outlook: the work for its meaning and purpose, competition
  and the norm no concern), how the rand come about, how an agent is checked. Sources:
  `src/docs/learning/12-evaluation.md`, `src/docs/learning/13-cost-and-token-economics.md`, ADR 0007,
  `src/docs/work-log.md`.
- `/framework/built-on-it`: the two demos, what the framework stands on with each licence, the hosts. Sources:
  `demos/puls/docs/`, `demos/louis-agent/docs/`, ADR 0030.
- `/framework/whats-coming`: the planned features by id, each marked planned. Sources: `ROADMAP.md`, `TODO.md`,
  `src/docs/plans/`. Everything there is planned, not built; say so.
- `/what-we-build`: what we build, in a buyer's words: the website with an agent in its corner, a custom agent for a
  bigger job, and how it starts (a short call, a working agent on made-up data, then theirs). Send someone who asks
  what you do here. The site's own words.
- `/pricing`: the two plans, Starter and Business, what each includes, and what is quoted on the short call (the
  building fee, the first files, a custom agent). Send anyone who asks what it costs here. The site's own words.
- `/contact`: how to reach the project. The site's own words.
- `/demos`: the demos in one place, each a real run recorded once and played back. Send someone who asks what the
  framework can do, or for a demo, here, and explain each demo from its doc: what it shows, how it was made, what is
  real and what it would mean for their business. Sources: `demos/louis-agent/docs/demos.md`.
- `/restaurant-demo`, `/law-practice-demo`, `/doctors-practice-demo`: three made-up businesses' agents (Wheatfield
  Kitchen, Koringland Attorneys, Graanveld Family Practice), each a real conversation recorded once: a customer asks,
  the agent answers from the business's own facts and passes a booking on after their yes, and the practices never
  give advice. The best answer to "what would it do for a business like mine?": send a restaurant, a lawyer or a
  doctor to theirs. Sources: `src/docs/projects/louis-agent.identity.showroom.md`.
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
