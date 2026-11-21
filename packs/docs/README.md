# The packs' docs

> **Reference.** What is written about the packs: how each one works, and the decisions only they need. **Elsewhere:**
> how to build one in [Build a pack](../../src/docs/guides/build-a-pack.md); why the packs are one area in
> [ADR 0090](../../src/docs/adr/0090-the-packs-are-one-area.md).

![The packs' docs: the general packs, the ones Puls uses, the ones the framework's site uses, and their three decisions](diagrams/README.svg)

A pack is what an agent can do: a folder of tools someone can take on its own. Every pack lives under `packs/tools/`,
and the rest of the area sits beside it with the same names as `src/`: `evals/` holds what proves a pack, `tests/` its
own tests, `web/` its own screens, and `docs/` this folder. Each pack's doc explains how it works and why it's built
that way, with its pictures. They fall into three groups.

## The general packs

The tools any agent may want, one pack each. They share one doc, because they share one shape.

| Pack | What it does | Its doc |
|---|---|---|
| files, git, dotnet, shell, web, paymo, devops | The workspace's files, git, .NET builds and tests, scripts in a shell, web search and fetch, Paymo time entries and Azure DevOps work items | [The general packs](projects/general-packs.md) |

## The packs Puls's cook uses

| Pack | What it does | Its doc |
|---|---|---|
| `LouisAgent.Packs.Recipes` | Finds and reads recipe pages from South African sites, and keeps what it reads in its own library | [Recipes](projects/LouisAgent.Packs.Recipes.md) |
| `LouisAgent.Packs.ShopPrices` | Estimates what the ingredients cost, from Stats SA's average prices | [Shop prices](projects/LouisAgent.Packs.ShopPrices.md) |
| `LouisAgent.Packs.WhereToBuy` | Names the nearest branch of a shop, from OpenStreetMap | [Where to buy](projects/LouisAgent.Packs.WhereToBuy.md) |
| `LouisAgent.Packs.Calculator` | Hands a sum to code, so the model doesn't do arithmetic in its head | [Calculator](projects/LouisAgent.Packs.Calculator.md) |

## The packs the framework's site and its demos use

| Pack | What it does | Its doc |
|---|---|---|
| `LouisAgent.Packs.Docs` | Lets the guide read a project's docs before it answers | [Docs](projects/LouisAgent.Packs.Docs.md) |
| `LouisAgent.Packs.Leads` | Tells the owner when someone is interested | [Leads](projects/LouisAgent.Packs.Leads.md) |
| `LouisAgent.Packs.Site` | Takes a visitor to a place on the site, and shows them cards and forms | [Site](projects/LouisAgent.Packs.Site.md) |
| `LouisAgent.Packs.Triage` | Gives a team that triages a service's errors the feed, the run's steps and a person to raise a case to | [Triage](projects/LouisAgent.Packs.Triage.md) |

## The decisions only the packs need

Three ADRs live here, beside the packs they bind. [ADR 0069](adr/0069-shop-prices-are-stats-sa-estimates.md) says
shop prices are estimates from Stats SA's averages. [ADR 0078](adr/0078-a-recipe-search-asks-several-sites.md) says a
recipe search asks several sites at once. [ADR 0082](adr/0082-the-recipe-library-is-an-open-feed.md) says the recipe
library keeps each new read for good, as an open feed. ADR numbers are shared with the framework's, so a number is
never used twice.

---
[Docs index](../../src/docs/README.md) · Related: [Build a pack](../../src/docs/guides/build-a-pack.md) · [ADRs](../../src/docs/adr/README.md)
