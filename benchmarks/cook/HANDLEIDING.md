# Kokkontrole (`cook`): Puls alleen teen Puls met die kok

## Wat dit meet

Wat voeg Puls se kok by? Dieselfde gesprekke word twee keer gevoer: eers deur Puls alleen, wat self kook en self
pryse soek, dan deur Puls soos gebou, wat 'n maaltyd aan die kok (`puls-cook`) oorgee. Wat getel word, is wat die
agente *gedoen* het (wie oorgegee het, watter webwerwe gelees is, hoeveel bestanddele geprys is), nie net wat hulle
geskryf het nie. Die koste kom uit die grootboek, nie uit die skrip nie.

## Die modes

| Modus | Puls se gereedskap | Die kok se gereedskap | Wat dit is |
|---|---|---|---|
| `puls-only` | `web` | — | Puls alleen: kan nie oorgee nie, kook en prys self |
| `puls-cook` | `specialists`, `web` | `web` | Puls soos gebou: die web self, kook na die kok |

`specialists` is nie iets wat die kok gebruik nie: dit is Puls se vermoë om oor te gee. Sonder dit kan Puls net self
kook, wat `puls-only` is. Die kok kry van Puls net `web` (die opgeneemde beurt wys `toolsets: ["web"]`).

## Die gesprekke

Elke gesprek is een sessie; 'n tweede boodskap is die opvolg.

| Gesprek | Waar | Wat gevra word | Reg is |
|---|---|---|---|
| `cook-af` | Stellenbosch | Ete vir vier onder R250, resep en inkopielys met pryse; dan: maak dit vir ses | die kok kry dit (in `puls-cook`), en die opvolg gaan terug na dieselfde kok |
| `cook-en` | Pretoria (in die boodskap) | 'n Uur se kosmaak vir twee, wat dit by die winkels kos; dan: hoender in plaas van vleis | soos hierbo, in Engels |
| `cook-shops` | Stellenbosch | Resep, elke bestanddeel se prys by twee winkels met skakels, een winkel vir alles, die takke, die goedkoopste mandjie | die kok kry dit, en prys by Suid-Afrikaanse winkels |
| `control-web` | Stellenbosch | Die huidige prima uitleenkoers, met bronne | Puls antwoord self (dit is nie kos nie) |

## Die tellings

Elke gesprek kry hierdie tellings, onder sy beurte in die lopie se tabel en in die verkenner:

| Telling | Beteken | ✓ wanneer |
|---|---|---|
| `role` | Die rol aan wie Puls in die eerste beurt oorgegee het, of leeg as Puls self geantwoord het | — |
| `routed_right` | Of die eerste beurt reg gestuur is | 'n maaltyd na `puls-cook` as die kok aan is; anders, en vir die kontrolevraag, Puls self; nooit as die beurt gefaal het nie |
| `answered_itself` | Puls het self geantwoord, sonder fout | — |
| `hosts` | Die webwerwe waarvan bladsye gelees is (Puls s'n en die kok s'n), in volgorde | — |
| `local_hosts` | Daarvan, die Suid-Afrikaanse (`.za`) | — |
| `shop_hosts` | Daarvan, die winkels (Checkers, Pick n Pay, Woolworths, Spar, Shoprite, Usave) | — |
| `sources_local` | Al die gelese werwe is Suid-Afrikaans | ja |
| `priced_at_shops` | Ten minste een winkel se bladsy is gelees, of sy katalogus gevra met 'n winkelinstrument (F34) | ja, waar daar 'n Checkers is |
| `shop_tool_calls` | Hoeveel keer die kok 'n winkelinstrument gebruik het (`FindProducts`, `SearchSpecials`, `SearchPromotions`, F34) | meer as 0 waar daar 'n Checkers is |
| `deal_tool_calls` | Daarvan, `SearchSpecials` en `SearchPromotions`: na spesiale gesoek | meer as 0 by elke ete met 'n winkelhulpmiddel: die kok soek eers spesiale |
| `deals_named` | Hoeveel rye op die lys 'n spesiaal langs die prys noem (`· BUY 2 FOR R45`, `SAVE`) | — |
| `untooled_shops` | Hoeveel winkels die lys prys waarvoor die kok geen instrument het nie (Louis, F34: net winkels met 'n instrument). Die portaal wys dit rooi | 0 |
| `store_for_area` | Hoeveel van die kok se winkelsoektogte die winkel beantwoord het wat die gebruiker se dorp bedien: die instrument se eerste reël noem die winkel, nagegaan teen Checkers se eie winkel-id (Louis: "hoe weet ons die adres is gebruik?") | meer as 0 in Malmesbury |
| `store_default` | Hoeveel die winkel se verstekwinkel beantwoord het: geen dorp nie, geen winkel daar nie, of die dorp s'n is nie gebruik nie | 0 in Malmesbury |
| `no_catalogue_line` | Die antwoord sê geen winkel naby die gebruiker het nog 'n katalogus hier nie (die lys sonder pryse) | ja in Darling (`cook-no-shop`), nee elders |
| `ingredients` | Hoeveel bestanddele op die lys is: die tabel se rye (die kok se eie *Geprys: n van m*-reël is sy bewering, nie die telling nie) | — |
| `priced` | Hoeveel daarvan 'n prys het **met die skakel waar dit vandaan kom**: net 'n prys met sy bron tel (F31 stap 11b: 'n prys uit die geheue is nie 'n antwoord nie). Die skakel moet na die winkel van daardie kolom gaan (Checkers se prys na `checkers.co.za`); 'n resep se skakel of 'n ander winkel s'n tel nie, en 'n verwysingskakel (`[R12,99][1]`) is sy URL | — |
| `priced_ratio` | `priced` gedeel deur `ingredients` | groen vanaf 0.8, rooi onder 0.5 |
| `unsourced` | Hoeveel pryse sonder 'n skakel gegee is: geraai, uit die geheue. Dit tel nooit as geprys nie; die portaal wys dit oranje (*geskat*) | 0 |
| `per_kg` | Hoeveel pryse met hul skakel 'n prys per kg is in plaas van 'n pak s'n: die sel sê *per kg* (`R99,99/kg`), of dit is een bedrag vir iets wat per gewig verkoop word (Checkers se skakel eindig op `KG`), waar 'n pak 'n reeks kos (`R87,99–R103,99`). Dit tel nie as geprys of geraai nie; die portaal wys dit oranje (PR #58 se resensie: die lopie het 'n pak van 1–1,2 kg teen sy kiloprys geprys) | 0 |
| `rand_amounts` | Hoeveel randbedrae die antwoorde gee | — |
| `unconfirmed_marks` | Hoe dikwels die antwoorde *onbekend*, *geskat* of *soekindeks* sê: eerlikheid oor wat nie bevestig is nie | — |
| `errors` | Hoeveel beurte gefaal het | 0 |
| `follow_up` | Wat met die opvolg gebeur het: *continued* (dieselfde kok), *new sub-session* (’n nuwe kok) of *answered itself* | *continued* |
| `follow_up_handle` | Die kok se handvatsel wat die opvolg voortgesit het | — |

## Wat dit kos

Dit spandeer geld: elke beurt vra Claude. Die eerste betaalde lopie (F31 stap 11b, `474faef30ff0`, 1 herhaling, 12
beurte) het **$0.0446** (R0.73) gekos, 49 versoeke op Claude Haiku 5.5: `puls-only` $0.013, `puls-cook` $0.032. Maar
daardie lopie het sonder websoektogte geloop (Tavily se krediete was op), so 'n lopie mét soektogte sal meer kos.

## Hoe om dit te lees

- **Op die bladsy:** die lopie se tabel wys elke beurt soos dit klaarkom; die tellings staan onder elke gesprek.
- **In die verkenner** (klik die lopie in die lys): die koste per modus en per gesprek langs mekaar, dan 'n gesprek
  se beurte op 'n tydlyn van modelversoeke, dan die beurt self, afgespeel soos dit getik is, en een versoek se rekord.
- Vergelyk `puls-only` met `puls-cook` op dieselfde gesprek: kos die kok meer, en prys hy meer bestanddele by
  plaaslike winkels?

## Met die hand

Met die Puls-houers aan, van die repo se wortel af:

```
BENCHMARK_RUN=local PULS_API=http://127.0.0.1:5070/ python3 benchmarks/cook/run_check.py --repeat 1
```

Sonder die portaal word die lopie nie gestoor nie; die grootboek het steeds sy koste onder die etiket `local`.
