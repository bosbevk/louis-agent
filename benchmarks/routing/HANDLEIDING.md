# Roeteringkontrole (`routing`): een agent teen Puls soos gebou

## Wat dit meet

Stuur Puls elke soort vraag na die regte plek? Twaalf vrae, twee van elke soort, elk in sy eie sessie. Net die kok is
aan (F29), so 'n maaltyd moet na `puls-cook` gaan, en elke ander vraag is reg as Puls dit self beantwoord: die web met
sy eie `web`, die res deur te sê wat dit nog nie kan doen nie. Wat getel word, is die oproep wat gemaak is, nie die
woorde nie. Die koste per modus kom uit die grootboek.

## Die modes

| Modus | Gereedskap | Wat dit is |
|---|---|---|
| `baseline` | elke stel wat hierdie instansie het, behalwe `specialists` en `minions` | een agent wat alles self doen en niks kan oorgee nie |
| `delegated` | `specialists`, `web` | Puls soos gebou; die kok wat dit kry, kry net `web` |

Die `baseline` se stelle kom uit `GET /toolsets` wanneer die lopie begin: Paymo en DevOps is daarby as hul sleutels
gestel is.

## Die vrae

| Soort | Vrae | Reg in `delegated` |
|---|---|---|
| `web`, `web-2` | Die nuutste .NET; die prima uitleenkoers | Puls self |
| `files`, `files-2` | Som README.md op; die LLM-instellings in .env.example | Puls self |
| `git`, `git-2` | Die laaste drie commits; wie AgentEngine.cs die meeste verander het | Puls self |
| `numbers`, `numbers-2` | Hoeveel .cs-lêers en reëls; hoeveel toetsmetodes | Puls self |
| `chat`, `chat-2` | Name vir 'n Jack Russell; 'n verjaardagboodskap | Puls self |
| `cook`, `cook-2` | Ete vir vier in Stellenbosch; ete vir twee in Pretoria | `puls-cook` |

## Die tellings

| Telling | Beteken | ✓ wanneer |
|---|---|---|
| `role` | Die rol aan wie Puls oorgegee het, of leeg as Puls self geantwoord het | — |
| `expected_role` | Die rol wat reg sou wees: `puls-cook` vir kos in `delegated`, anders leeg | — |
| `routed_right` | `role` is `expected_role`, en die beurt het nie gefaal nie | ja |
| `answered_itself` | Puls het self geantwoord, sonder fout | — |
| `delegations` | Hoeveel keer Puls oorgegee het (`StartSubSession` of `ContinueSubSession`) | 0 of 1 |
| `puls_calls` | Puls se eie gereedskapoproepe | — |
| `specialist_calls` | Die spesialis se oproepe | — |
| `hosts` | Die webwerwe waarvan bladsye gelees is | — |
| `errors` | 1 as die beurt gefaal het | 0 |

Die `baseline` kan nie oorgee nie, so dit is reg tensy dit gefaal het: dit meet wat dieselfde vrae sonder Puls se
oorgawe kos.

## Wat dit kos

Dit spandeer geld: 24 vrae per herhaling op Claude. Die `baseline` se prompt is groter (al sy stelle se gidse), so
elke vraag kos daar meer; die eerste betaalde lopie se werklike koste kom hier.

## Hoe om dit te lees

- **Op die bladsy:** onder elke vraag staan of dit reg gestuur is.
- **In die verkenner:** die telling (*n / 12 reg gestuur*) en koste per modus op die teëls, dan per vraag langs
  mekaar, dan die beurt self, afgespeel soos dit getik is.

## Met die hand

```
BENCHMARK_RUN=local PULS_API=http://127.0.0.1:5070/ python3 benchmarks/routing/run_check.py --repeat 1
```
