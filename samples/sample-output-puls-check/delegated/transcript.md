## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* StartSubSession, GetToday · *Specialists:* — · *23.8 s*

Die nuutste stabiele weergawe is **.NET 10.0.13** (LTS). Dit is vandag, 13 Oktober 2026, vrygestel as 'n sekuriteitopdatering.

- **Oorspronklike .NET 10 GA:** 11 November 2025.
- **Ondersteuning:** Aktiewe ondersteuning tot **14 November 2028**, want dit is 'n LTS-weergawe (drie jaar).
- **Ander lyne:** .NET 9.0.21 en .NET 8.0.32 is albei in onderhoudsmodus en eindig op 10 November 2026.
- **Preview:** .NET 11.0.0-rc.2 is uit. .NET 11 self word by .NET Conf, 10 tot 13 November 2026, verwag.

**Bronne**
- https://dotnet.microsoft.com/en-us/download/dotnet (aflaaiblad, wys 10.0 as die nuutste)
- https://dotnet.microsoft.com/en-us/download/dotnet/10.0 (10.0.13, 13 Oktober 2026)
- https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json (amptelike releases-indeks: 10.0.13, eol 2028-11-14)
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core (ondersteuningsbeleid; GA-datum van .NET 10)

Let wel: ek het self nie die bladsye oopgemaak nie. Die navorser het dit gedoen. Die ondersteuningsbeleid-bladsy is 'n snapshot van September, so die patch-tabel daar wys nog 10.0.12. Die indeks en aflaaiblad wys 10.0.13, en ek het die nuwer bronne gebruik. As jy presies wil seker wees, kyk gerus self na die aflaaiblad.

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* StartSubSession · *Specialists:* — · *12.1 s*

Die projek is **Louis Agent**. Hier is die README in drie punte:

- **Wat dit is:** 'n Selfgehoste AI-kodeeragent vir .NET, gebou op Microsoft.Extensions.AI. Die agent werk op jou repository met regte gereedskap: lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo. Standaard gebruik dit Claude, maar Ollama en OpenAI-versoenbare eindpunte werk ook.
- **Hoe jy dit gebruik:** Daar is vyf gashere (Rider via ACP, 'n webapp, 'n HTTP API, 'n CLI en 'n MCP-bediener), en almal deel dieselfde `louis-agent.core` met die enjin, gereedskap, vaardighede en verskaffers.
- **Orkestreerder-demo (proof of concept):** `src/louis-agent.orchestrator` is 'n tweede agent wat 'n mikrodiens se foute dophou. Dit laat louis-agent die oplosbare foute herstel, met een tak en commit per herstel. Niks word gepush nie, en die orkestreerder verifieer elke herstel self.

Bron: `README.md` in die werkruimte. Ek het net daardie lêer gelees, nie die verwysde `docs/`- of `samples/`-lêers nie.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* StartSubSession · *Specialists:* — · *31.2 s*

Die laaste drie commits is almal deur louis marais op 13 Oktober 2026 gemaak, op die branch `feature/F20-puls-look` in `bosbevk/louis-agent`.

**1. `8cdfa67`** (23:46): *PR #28 review: PDFs, location precision, the time zone, Done when, goal map*
- **PDF-lees:** `WebTools.cs` gee nou `DocumentUnreadable` terug as 'n PDF nie gelees kan word nie, en daar is 'n toets vir 'n beskadigde PDF bygevoeg.
- **Ligging:** koördinate word in `ChatPage.razor` en `chat.js` tot twee desimale afgerond (ongeveer 1 km) voordat dit na Nominatim gaan.
- **Tydsone:** `Program.cs` sluit nou die tydsone by elke nuwe chat in, en `AgentSessionsTests.cs` is daarvoor aangepas.
- **Dokumentasie:** "Done when"-afdelings is by F20, F21, F22 bygevoeg, en `GOAL.md` se vaardighede 3, 4, 10 en 19 is opgedateer.

**2. `d28c274`** (23:44): *Merge the PR #27 review fixes from the F5 branch*
- Dit is 'n samevoeging van `e808040` en `3a92bf7` van die F5-branch. Daar was konflikte in `ARCHITECTURE.md` en `AgentEngineFactory.cs`.

**3. `e808040`** (23:42): *PR #27 review: engines evicted, a rolling limit without a turn, unreadable replies*
- **Engines:** `AgentEngineFactory.cs` verwyder nou die minst onlangs gebruikte profiel as die fabriek vol is, in plaas van om nuwe toolset-lyste te weier.
- **Peer-oproepe:** `AgentPeerTools.cs` beperk `StartSubSession` buite 'n beurt tot 3 per minuut. 'n Peer-antwoord wat nie geldige JSON is nie, gee nou 'n fout terug.
- **Toetse:** nuwe toetse in `AgentEngineFactoryTests.cs` en `AgentPeerToolsTests.cs`, plus 'n bygewerkte snapshot in `ToolMessageSnapshot.json`.

Let wel: my specialist het net die commit-boodskappe en die lêerstatistieke gelees, nie die kode-diffs self nie. Die beskrywings hierbo is dus afgelei uit die boodskappe, en ek kan nie sekerwees dat elke lêer bygevoeg, verwyder of gewysig is nie. As jy presies wil weet wat in die kode verander het, kan ek 'n diff laat nagaan.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* StartSubSession · *Specialists:* — · *36.4 s*

Daar is **109 .cs-lêers** onder `louis-agent.core/`, met **ongeveer 11 000 reëls** altesaam. Van daardie reëls is 9 274 nie-leë reëls.

- **Wat ingesluit is:** alle .cs-lêers in die map en submappe, behalwe `bin/` en `obj/`.
- **Wat getel is:** alle reëls, ook leë reëls en kommentaar. Die 11 000 is dus die ruwe telling.
- **Die tests-map** (`tests/louis-agent.core.tests/`) is nie ingesluit nie, want dit is 'n ander projek.

Die grootste lêers:

1. `Tools/Integrations/Paymo/PaymoTools.cs`: 851 reëls
2. `Messages/ToolMessage.cs`: 668 reëls
3. `Tools/Workspace/WorkspaceTools.cs`: 560 reëls
4. `Tools/Integrations/DevOps/DevOpsTools.cs`: 547 reëls
5. `Hosting/AgentHost.cs`: 405 reëls

Een ding wil ek eerlik noem: die specialis se verslag is nie heeltemal konsekwent oor die gegenereerde `obj/`-lêers nie. Die script sluit `obj/` uit, maar die verslag praat ook van drie gegenereerde lêers wat "weggelaat" is. Die 109-telling kan dus effens verskil as jy presies wil weet wat ingesluit is. As jy die presiese lys van al 109 lêers met elkeen se reëltelling wil hê, kan ek dit laat uitvoer.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Specialists:* — · *7.3 s*

Hier is 'n paar wat by 'n Jack Russell pas:

- **Blits**: die lewe self, altyd op pad na iets.
- **Skoppie**: hy spring en skop soos 'n bok.
- **Bokkie**: 'n ou klassieker vir 'n klein hond met 'n groot persoonlikheid.
- **Boela**: klink soos 'n oom wat altyd 'n plan het.
- **Kallie**: vriendelik en 'n bietjie ondeund.
- **Biltong**: droog, taai en lekker, en mens sal dit altyd wil eet.
- **Vetkoek**: as hy rond en gelukkig lyk na 'n paar koekies.
- **Koeksister**: taai aan die boelie se kant en mooi soet.
- **Dolf**: vir 'n hond wat altyd die naweek se braai wil inspekteer.
- **Sakkie**: rustig, maar net tot hy die kat sien.
- **Tollie**: kort, lekker en maklik om te roep.

As ek een moet kies vir 'n Jack Russell wat nie stilsit nie: **Blits**. As hy 'n rustige kant het, **Biltong**.

