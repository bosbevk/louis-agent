# Proefkontrole (`stub`): gratis, geen model

## Wat dit meet

Niks van Puls nie: dit toets die portaal self. Die skrip druk 'n vaste lopie in die portaal se protokol sonder om 'n
sessie oop te maak of 'n model te vra, so dit kos niks. Gebruik dit om te sien of die stoor, die tabel, die verkenner
en die afspeel werk, voor 'n betaalde lopie.

## Die modes

| Modus | Gereedskap (net as etiket) |
|---|---|
| `puls-only` | `web` |
| `puls-cook` | `specialists`, `web` |

## Die gesprekke

| Gesprek | Beurte | Wat dit wys |
|---|---|---|
| `bobotie` | 2 (met 'n opvolg) | denke, teks, en in `puls-cook` 'n oorgawe aan die kok met sy eie gebeure |
| `cottage-pie` | 1 | dieselfde, een beurt |

## Die tellings

| Telling | Beteken |
|---|---|
| `routed_right` | Altyd ja: die lopie is vas |
| `turns` | Hoeveel beurte die gesprek gehad het |

## Wat dit kos

Niks: geen versoeke nie, so die koste wys as *—* (onbekend: die grootboek het niks vir die lopie nie).

## Met die hand

```
python3 benchmarks/stub/run_check.py --repeat 2
```
