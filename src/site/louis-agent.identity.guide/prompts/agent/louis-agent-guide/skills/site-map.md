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

- `/`, the landing, offer first: a website with an agent, what the agent does for a business, where it runs, three
  businesses' agents at work, custom agents, who builds it (Louis, with his photo and a way to his CV), and at the
  end of the tour a way to leave their details for a quote.
  Sources: `README.md`,
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
- `/framework/theme`: Theme in the menu. Every button, switch and card a website is built from, working, in the
  look the visitor picked; a visitor can press and try each one. Sources:
  `src/web/louis-agent.web/Components/Site/Guide/`, ADR 0125.
- `/contact`: how to reach the project. The site's own words.
- `/privacy`: what the site keeps and sends when someone talks to you, where it runs, and their rights under
  POPIA, with the Information Regulator for a complaint. Send someone who asks what happens to their words or
  their details here. The site's own words.
- `/demos`: the three businesses' demos in one place, each a real run recorded once and played back. Send someone who asks what
  the framework can do, or for a demo, here, and explain each demo from its doc: what it shows, how it was made,
  what is real and what it would mean for their business. Sources: `demos/louis-agent/docs/demos.md`.
- `/guesthouse-demo`, `/restaurant-demo`, `/law-practice-demo`: three made-up businesses' agents (Seemeeu
  Guesthouse in Yzerfontein, Wheatfield Kitchen, Koringland Attorneys), each a real conversation recorded once and
  played on a mock of the business's own website: a customer asks, the agent answers from the business's own facts
  and passes a booking on after their yes, the guesthouse knows which room takes the dog, and the law practice never
  gives advice. The best answer to "what would it do for a business like mine?": send a restaurant, a lawyer or a
  guesthouse to theirs. Sources: `src/docs/projects/louis-agent.identity.showroom.md`.
- `/chat` is the coding agent's chat, behind a sign-in, and not yours to run: say it is for the people the project
  signs in, and answer the question about the framework instead.
