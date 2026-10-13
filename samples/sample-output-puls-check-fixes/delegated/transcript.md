## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* StartSubSession, GetToday · *Delegations:* [{"toolsets": ["web"], "personality": "puls-researcher"}] · *34.9 s*

Die nuutste stabiele (GA) weergawe is **.NET 10**. Die hoofweergawe **.NET 10.0.0 is op 11 November 2025 vrygestel**. Die nuutste patch is **10.0.13**, uitgereik op 13 Oktober 2026, en dit is 'n sekuriteitsopdatering.

- **Ondersteuning:** .NET 10 is 'n LTS-weergawe (langtermyn-ondersteuning) en word ondersteun tot **14 November 2028**.
- **.NET 11:** Daar is wel 'n nuwer weergawe, 11.0.0-rc.2, maar dis 'n release candidate en nog nie GA nie. Dis dus nie die een om in produksie te gebruik nie.
- **.NET 8 en .NET 9:** Albei is in die onderhoudsfase en verval in November 2026.

**Bronne**
- https://github.com/dotnet/core/blob/main/release-notes/releases-index.json (die weergawe-indeks: 10.0.13, 2026-10-13, LTS, EOL 2028-11-14)
- https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json (10.0-kanaal se metadata)
- https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md (datums van 10.0.0, 11 November 2025)
- https://learn.microsoft.com/en-us/dotnet/core/releases-and-support (ondersteuningstabel; die bladsy se lys kan effe agter wees)
- https://dotnet.microsoft.com/en-us/download/dotnet (RC-banner vir .NET 11)

**Waarvan ek nie seker is nie:** Ek kon nie die amptelike aankondiging-blogpos vir .NET 10 oopmaak nie. Die 11 November 2025-datum kom uit die release-notes op GitHub, wat 'n amptelike bron is. Ek het ook nie die hele 10.0 `releases.json` gelees nie, so die CVE-lys vir 10.0.13 is nie nagegaan nie.

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *10.6 s*

Die projek is **Louis Agent**, volgens `README.md` in die werkruimte. Drie punte:

1. **'n Selfgehoste KI-kodeagent vir .NET.** Dit is gebou op Microsoft.Extensions.AI en werk met regte gereedskap op jou repository: lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo.
2. **Jy praat daarmee op verskillende maniere.** Dit kan deur JetBrains Rider, 'n webapp, 'n HTTP-API of die terminal (CLI). Al hierdie gashere deel dieselfde kern, `louis-agent.core`.
3. **Modelkeuse is oop.** Standaard loop dit op Claude (Anthropic), maar Ollama en OpenAI-kompatibele eindpunte werk ook.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["git"], "personality": "puls-historian"}] · *28.3 s*

Die laaste drie commits in die `louis-agent`-repository (branch `feature/F20-puls-look`) is almal regstellings op die agent-stelsel, elk met dokumentasie en toetse. Hulle is almal van 14 Oktober 2026, deur louis marais:

1. **`bb78ef7`** (00:26), *FX12: .env-templates kan gelees word*
   - Lêers wat met `.env` begin, word nie meer outomaties geblokkeer nie, maar net as hulle op `.example`, `.sample` of `.template` eindig.
   - Die regte `.env`-lêers en LLM-profiele bly geblokkeer.
   - Veranderde lêers o.a. `Guardrails/SensitiveFiles.cs`, plus nuwe toetse en `docs/fixes/FX12-env-templates-readable.md`. 9 lêers, +92 / −6.

2. **`69725b9`** (00:24), *FX11: 'n sub-sessie wat sonder antwoord eindig, word een keer weer gevra*
   - As `StartSubSession` leeg terugkom, stuur dit een opvolgvraag in dieselfde sessie.
   - As dit weer leeg is, kry die model 'n fout wat sê om nie die taak weer te stuur nie.
   - Veranderde lêers o.a. `Tools/Agents/AgentPeerTools.cs` en `Messages/ToolMessage.cs`, plus toetse en `docs/fixes/FX11-...md`. 7 lêers, +158 / −12.

3. **`42282fa`** (00:20), *FX10: `agent` is opsioneel in sub-sessie-oproepe*
   - As jy nie 'n peer-naam gee nie, gebruik dit die enigste peer. Voorheen het die oproep twee keer misluk en het Puls 'n naam probeer raai.
   - Veranderde lêers o.a. `Tools/Agents/AgentPeerTools.cs` en `Skills/agents-skills.md`, plus toetse en `docs/fixes/FX10-...md`. 7 lêers, +108 / −32.

Let wel: die opsommings is afgelei uit die commit-boodskappe, lêernaamme en statistieke. Die sub-sessie het nie die presiese kode-diff gelees nie, so die detail van die C#-veranderinge is nie bevestig nie. As jy presies wil weet watter reëls verander is, kan ek dit nagaan.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files", "bash"], "personality": "puls-analyst"}] · *27.3 s*

Ek laat 'n analis dit tel, want ek kan self nie by die lêers uitkom nie.Onder **louis-agent.core** (`/workspace/louis-agent.core`) is daar **109 .cs-lêers** met saam **11 074 reëls**.

- Die telling sluit bin/, obj/ en .git/ uit. Daar is nog 3 .cs-lêers in daardie gidse (38 reëls), wat ek nie by die hoofgetal getel het nie.
- Blanko reëls en kommentaar is ingesluit, en toetsprojekte ook.
- 29 lêers het geen afsluitende newline nie, so `wc -l` sou hul laaste reël mis. Dit is reggemaak.

Die analis het die telling met 'n bash-skrip gedoen. Ek het nie self die lêers oopgemaak om dit na te gaan nie. As jy net die bronkode wil hê sonder toetse of gegenereerde lêers, kan ek dit ook uitsorteer.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *9.3 s*

Hier is 'n paar wat by 'n Jack Russell pas:

- **Jakkals**: die voor die hand liggende een. Hulle is oorspronklik vir jaggeld gekweek, en hierdie hond het die jakkals se slimheid.
- **Skobbejaak**: die ondeunde karakter uit ons kinderstories. As hy die sokkie van jou voet steel, is die naam verdien.
- **Blits**: want hulle is vinnig soos weerlig, veral as hulle die tuin in hardloop.
- **Koos**: 'n ou, eg Afrikaanse naam met 'n warm gevoel. Lekker vir 'n hond wat altyd met 'n grynsie kyk.
- **Sjakkie**: 'n speelse verwysing na "Jack", en dit klink soos 'n kwaai hond wat eintlik 'n teddiebeer is.
- **Bokkie**: 'n soet koosnaam wat jy by 'n klein hondjie kan roep as hy by jou op die bank kom klim.
- **Kaneel**: as hy bruin en wit gevlek is, soos 'n koeksister met suiker.
- **Boela**: 'n ou dorpie-naam wat vir 'n stoute ou hondjie perfek is.

Mý keuse? **Skobbejaak**, want dis 'n naam wat jy ook met 'n stem vol vermaak kan roep, en hy sal jou altyd laat lag.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["web"], "personality": "puls-researcher"}] · *61.1 s*

Ek kan die huidige prima koers nie met 'n bron bevestig nie. Die soekdiens was geblokkeer, en die banke se tariefbladsye en die SARB se markkoersbladsy het nie data gegee nie.

**Wat ek wel weet:**

- Prima is gewoonlik die SARB se repo-koers plus 3,5 persentasiepunte.
- Die repo was 6,75% by die SARB se besluit op 26 Maart 2026 (Moneyweb: "Sarb holds rate steady at 6.75%"). Dit dui daarop dat die prima koers 10,25% was.
- Die laaste verandering wat ek kan nagaan, was 'n verlaging van 25 basispunte na 6,75% by die MPC-vergadering van Julie 2025 (ongeveer 31 Julie). Prima het toe na 10,25% gegaan.
- Wikipedia se prima-bladsy wys 10,50% vir "Julie 2025". Dit stem nie met die repo-geskiedenis ooreen nie, so moenie daarop staatmaak nie.

**Wat ek nie kon nagaan nie:** of die SARB se MPC-besluite van Mei, Julie of September 2026 die repo verander het. As een daarvan 'n verandering was, is die prima koers anders as 10,25%.

**Bronne:**

- Moneyweb, 26 Maart 2026: https://www.moneyweb.co.za/news/economy/sarb-holds-rate-steady-at-6-75/ (ek kon net leserskommentaar lees, nie die artikel self nie)
- Wikipedia, South African Reserve Bank: https://en.wikipedia.org/wiki/South_African_Reserve_Bank
- SARB markkoerse: https://www.resbank.co.za/en/home/what-we-do/statistics/key-statistics/current-market-rates (laai sonder waardes)

**Vir 'n betroubare antwoord** kyk ek die SARB se MPC-verklarings van 2026 of 'n bank se amptelike tariefblad (bv. Absa of FNB se "prime rate"-bladsy). Ek kan dit weer probeer as jy wil, of jy kan self in een van daardie bladsye kyk, wat die vinnigste is.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *24.5 s*

In `config/.env.example` is daar maar **een** instelling wat direk oor die LLM gaan:

- **`LLM_PROFILE`** (reël 7), wat op `anthropic` staan. Dit kies watter LLM-profiel gebruik word: `config/.env.anthropic` (Claude, het `ANTHROPIC_API_KEY` nodig in `.env.secrets`) of `config/.env.ollama` (plaaslik en gratis). Jy kan dit in die lêer verander, of vir een keer in die shell instel, bv. `LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli`.

Die kommentaar in reël 6 sê ook dat enige `LLM_*`-instelling wat jy in hierdie lêer skryf, dié van die profiel oorskryf. Laat hulle dus weg, tensy jy een bewustelik wil vashou.

Die model, base URL, temperatuur, max tokens, timeout en die API-sleutel self staan **nie** in hierdie lêer nie. Die API-sleutel word net genoem, die waarde is in `.env.secrets`. Die model en die ander parameters sal waarskynlik in `config/.env.anthropic` of `config/.env.ollama` wees. Dit het ek nog nie nagegaan nie. Sê maar as ek dit moet doen.

Daar is ook instellings wat oor die LLM se gebruik en koste gaan, nie oor die LLM self nie:

- **`USAGE_LEDGER`** (reël 12, uitgekommentarieer): skryf een JSON-reël per modelversoek na `usage-JJJJ-MM.jsonl` in `LOG_DIRECTORY`.
- **`LOG_DIRECTORY`** (reël 13): die gids vir daardie gebruikslêer.
- **`USAGE_IN_CHAT`** (reël 16): wys gebruik en koste in die chat ná elke antwoord.
- **`PRICES_FILE`** (reël 18) en **`PRICES_URL`** (reël 20): waar die pryslys vandaan kom.
- **`HOST_CULTURE`** (reël 23) en **`DISPLAY_CURRENCY`** (reël 25): die geldeenheid vir die koste. Albei is uitgekommentarieer, en `DISPLAY_CURRENCY` oorskryf `HOST_CULTURE`.
- **`RATES_URL`**, **`RATES_FORMAT`** en **`RATES_FILE`** (reëls 28–30): die wisselkoerse.

*Bron: `config/.env.example`, gelees deur 'n files-subsessie.*

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["git", "files"], "personality": "puls-historian"}] · *26.2 s*

Dit is 'n vraag oor die geskiedenis van die lêer, so ek stuur dit na 'n spesialis wat die git-geskiedenis kan nagaan.Louis Marais het die meeste aan `louis-agent.core/Agent/AgentEngine.cs` verander: 31 van die 32 commits op die lêer. Die enigste ander commit is die oorspronklike een van "Louis" (`574d89c`).

Die laaste verandering was commit `b9287a1` op 13 Oktober 2026, met die boodskap "F22 step 1: GetToday, for every session".

Twee dinge om na te kyk:
- Die spesialis het "louis marais" (kleinletters, 4 commits) en "Louis Marais" (27 commits) saamgetel. Dit is 'n afleiding uit die naam; ek kon nie bevestig dat dit dieselfde persoon is nie.
- Die datums is net dagdatums. Die tyd van die laaste commit het ek nie.

Ek het nie die gewysigde reëls per persoon getel nie, so ek kan nie sê wie die meeste kode bygedra het in terme van reëls nie, net in terme van commits.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *176.0 s*

Daar is **725 toetsmetodes** onder `tests/louis-agent.core.tests`:

- **669** is met `[Test]` geannoteer.
- **56** is met `[TestCase]` geannoteer. Daar is 243 `[TestCase]`-attribute op hulle, want sommige metodes het meer as een.

Ek het die `[Test]`-telling per lêer bymekaargetel en dit kom presies op 669 uit. Die 56 `[TestCase]`-metodes het ek ook nagegaan. Die tellings is uit die volledige lêers gelees, nie uit soektreffers nie, en `obj/`-gidse is uitgesluit.

Die 725 sluit ook 5 `[Ignore]`-metodes (in `Tools/Git/GitToolsTests.cs`) en 3 `[Explicit]`-metodes in. As jy net die wat regtig loop wil tel, trek daardie 8 af.

Die grootste lêers is `DevOpsToolsTests.cs` (55), `PaymoToolsTests.cs` (52) en `WorkspaceToolsTests.cs` (38).

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *2.6 s*

Hier is 'n kort boodskap wat jy kan gebruik:

**Gelukkige verjaardag, Ma!**

Baie geluk met jou dag. Dankie dat jy altyd so lief vir ons sorg, vir die raad wat jy gee en vir die warmte van jou huis. Ek hoop jy geniet vandag elke oomblik, want jy verdien dit ten volle.

Ek is baie lief vir jou. Geniet jou dag!

Met liefde,
[Jou naam]

Wil jy dit meer persoonlik hê, met 'n herinnering of 'n grap wat net julle twee verstaan? Sê net wat, dan maak ek dit reg.

