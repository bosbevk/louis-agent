# The packs' docs

> **Reference.** What is written about the packs: how each one works, and the decisions only they need. **Elsewhere:**
> how to build one in [Build a pack](../../src/docs/guides/build-a-pack.md); why the packs are one area in
> [ADR 0090](../../src/docs/adr/0090-the-packs-are-one-area.md).

![The packs' docs: the general packs, the calculator, the ones the framework's site uses, and the three that moved with Puls](diagrams/README.svg)

A pack is what an agent can do: a folder of tools someone can take on its own. Every pack lives under `packs/tools/`,
and the rest of the area sits beside it with the same names as `src/`: `evals/` holds what proves a pack when one has
checks of its own (none does today), `tests/` its own tests, `web/` its own screens, and `docs/` this folder. Each
pack's doc explains how it works and why it's built that way, with its pictures. They fall into three groups.

## The general packs

The tools any agent may want, one pack each. They share one doc, because they share one shape.

| Pack | What it does | Its doc |
|---|---|---|
| files, git, dotnet, shell, web | The workspace's files, git, .NET builds and tests, scripts in a shell, and web search and fetch | [The general packs](projects/general-packs.md) |

## A pack for sums

| Pack | What it does | Its doc |
|---|---|---|
| `LouisAgent.Packs.Calculator` | Hands a sum to code, so the model doesn't do arithmetic in its head | [Calculator](projects/LouisAgent.Packs.Calculator.md) |

## The packs the framework's site and its demos use

| Pack | What it does | Its doc |
|---|---|---|
| `LouisAgent.Packs.Docs` | Lets the guide read a project's docs before it answers | [Docs](projects/LouisAgent.Packs.Docs.md) |
| `LouisAgent.Packs.Leads` | Tells the owner when someone is interested | [Leads](projects/LouisAgent.Packs.Leads.md) |
| `LouisAgent.Packs.Site` | Takes a visitor to a place on the site, and shows them cards and forms | [Site](projects/LouisAgent.Packs.Site.md) |
| `LouisAgent.Packs.Triage` | Gives a team that triages a service's errors the feed, the run's steps and a person to raise a case to | [Triage](projects/LouisAgent.Packs.Triage.md) |

## The packs that moved with Puls

The recipes, shop-prices and where-to-buy packs were Puls's cook's, and they live with Puls now, in
[its own repository](https://github.com/bosbevk/puls), with the three decisions only they needed:
[ADR 0069](https://github.com/bosbevk/puls/blob/main/docs/adr/0069-shop-prices-are-stats-sa-estimates.md) on shop
prices as Stats SA's estimates, [ADR 0078](https://github.com/bosbevk/puls/blob/main/docs/adr/0078-a-recipe-search-asks-several-sites.md)
on a recipe search asking several sites, and
[ADR 0082](https://github.com/bosbevk/puls/blob/main/docs/adr/0082-the-recipe-library-is-an-open-feed.md) on the
recipe library as an open feed. ADR numbers are shared with the framework's, so those numbers are never used again.

---
[Docs index](../../src/docs/README.md) · Related: [Build a pack](../../src/docs/guides/build-a-pack.md) · [ADRs](../../src/docs/adr/README.md)
