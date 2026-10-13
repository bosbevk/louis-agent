## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, DotNetSdkInfo, FetchUrl, FetchUrl, GetToday, FetchUrl · *Delegations:* — · *16.7 s*

Die nuutste **stabiele** weergawe is **.NET 10.0.13**. Dit is 'n LTS-vrystelling (langtermyn-ondersteuning) wat op **13 Oktober 2026** uitgereik is. Dit is 'n sekuriteitsopdatering op .NET 10, en die ondersteuning daarvoor loop tot **14 November 2028**.

**Die volgende hoofweergawe, .NET 11, is nog nie klaar nie.** Die nuutste beskikbare is **11.0.0-rc.2** (Release Candidate). Dit is vir toetsing, nie vir produksie nie.

Plaaslik het jy .NET SDK **10.0.401** en runtime **10.0.12** geïnstalleer. Jy is dus 'n bietjie agter op die 10.0.13-opdatering, so dit is die moeite werd om op te gradeer as jy op produksie wil wees.

**Bronne** (die webs soektog het misluk, so ek het die amptelike bronne direk gelees):
- Releases-indeks: https://github.com/dotnet/core/blob/main/release-notes/releases-index.json
- .NET 10 vrystellingsmetadata: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- Allerlaaste aflaaibladsy: https://dotnet.microsoft.com/en-us/download/dotnet

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *4.9 s*

Louis Agent is 'n self-gehoste AI-kodeagent in .NET. Dit werk op jou repository met regte tools (lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo) en praat met jou deur Rider, 'n webapp, 'n HTTP API of die terminal. Dit gebruik standaard Claude, maar Ollama en OpenAI-kompatibele modelle werk ook.

Die agent is ook 'n demo: `src/louis-agent.orchestrator` is 'n tweede agent wat foute in 'n mikrodiens opspoor, besluit watter foute regstelbaar is, en elke regstelling op sy eie branch vasle (nie gestuur nie) en self toets. Die `samples/`-gids het 'n Docker-demo met tien bugs.

Jy kan dit ook met 'n goeie hoeveelheid kontrole gebruik: die webapp wys lêers, veranderinge en branches, en laat jou stage, commit en merge. Die agent stoot net as jy dit vra. Lêertools bly binne die werkruimte, sekrete word geweier, en die API en webapp luister net op `127.0.0.1`.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* ShowCommit, ShowCommit, ShowCommit · *Delegations:* — · *8.8 s*

Hier is die laaste drie commits, nuutste eerste. Almal is deur louis marais gemaak.

**1. `bb78ef7` (FX12): `.env`-templates kan gelees word**
- `SensitiveFiles` het elke naam wat met `.env` begin, geblokkeer. Dus kon die specialis en die basislyn nie `config/.env.example` lees nie.
- Nou is name wat in `.example`, `.sample` of `.template` eindig, toegelaat. Die regte `.env`-lêers en die LLM-profiele bly geblokkeer.
- Ook by: nuwe dokumentasie in `docs/fixes/FX12-env-templates-readable.md`, nuwe toetse in `SensitiveFilesTests.cs` en `WorkspaceToolsTests.cs`, en 'n bywerking van `TODO.md`.
- 9 lêers, 92 bygevoeg en 6 uitgevee.

**2. `69725b9` (FX11): 'n sub-session wat sonder antwoord eindig, word een keer weer gevra**
- As 'n specialis sy tool-rondes opgebruik en geen teks terugstuur nie, het Puls die hele taak aan 'n nuwe specialis gestuur. Dit het koste en werk verdubbel.
- `StartSubSession` stuur nou een opvolgvraag in dieselfde sessie, sodat die geskiedenis en die gekasde voorvoegsel hergebruik word. Twee leë antwoorde is 'n fout.
- Verander `AgentPeerTools.cs` en `ToolMessage.cs`, met nuwe toetse in `AgentPeerToolsTests.cs`, plus dokumentasie `FX11-empty-sub-session-answer.md`.
- 7 lêers, 158 bygevoeg en 12 uitgevee.

**3. `42282fa` (FX10): 'n sub-session-oproep sonder `agent` gebruik die enigste portier**
- Die parameter `agent` was verpligtend, so Puls se oproep is twee keer geweier en toe het Puls 'n naam "default" geraai.
- `agent` is nou opsioneel (na `message`, soos C# dit vereis). Die skema vereis net `toolsets` en `message`.
- Verander `AgentPeerTools.cs` en die toetse daarvoor, plus dokumentasie `FX10-optional-peer-name.md`.
- 7 lêers, 108 bygevoeg en 32 uitgevee.

Die drie commits hang saam: FX10 maak `agent` opsioneel, FX11 hanteer leë antwoorde van sub-sessions, en FX12 laat `.env`-templates lees.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* BashRun · *Delegations:* — · *6.2 s*

Daar is **112 `.cs`-lêers** onder `louis-agent.core`, en hulle het saam **11 083 reëls**.

Dit tel ook lêers in `bin` en `obj`. As jy net die bronkode wil tel, is dit **109 lêers met 11 045 reëls**. Die verskil is klein, want dit is waarskynlik net gegenereerde lêers.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *5.1 s*

Hier is drie:

1. **Jakkals**: Die voor-die-hand-liggende een, want "Jack" en "jakkals" klink mos amper dieselfde. Dis ook 'n slim, vinnige hond wat altyd iets in die oog het.
2. **Skelm**: Vir die hondjie wat by die braai die vleis probeer steel en dan onskuldig kyk. Hy gaan die naam verdien.
3. **Sprinkaan**: Want Jack Russells spring die hele dag hoog in die lug, net soos 'n sprinkaan in die veld.

As jy 'n sagter naam wil hê, is **Boela** ook 'n lekker klassieke Afrikaanse hondenaam.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* WebSearch, FetchUrl, FetchUrl, FetchUrl, WebSearch, FetchUrl, FetchUrl, FetchUrl, GetToday · *Delegations:* — · *30.0 s*

Ek kon nie die presiese huidige koers bevestig nie. Die webfunksie se soekdiens het gewerk nie ("DuckDuckGo refused the automated search"), en die SARB se "Current Market Rates"-bladsy het die syfers nie in die teks gewys nie. Ek sal dus nie 'n getal as seker voorhou nie.

**Wat ek weet (my kennis is nie tot vandag by nie):**
Die prima-koers is gewoonlik die SARB se **repo-koers + 3,5 persentasiepunte**. Die laaste keer wat ek die bewegings gesien het, was die repo-koers op ongeveer **6,75%**, wat prima op ongeveer **10,25%** sou plaas. Ek is nie seker van die presiese datum van die laaste verandering nie. Vandag is 14 Oktober 2026, so daar kon sedertdien al 'n verandering gewees het.

**Kyk self na die presiese getal en datum:**
- **SARB se beleidsbesluite (MPC-verklarings):** https://www.resbank.co.za/en/home/publications/statements/mpc-statements. Hier sien jy elke besluit en die datum daarvan.
- **SARB se Current Market Rates:** https://www.resbank.co.za/en/home/what-we-do/statistics/key-statistics/current-market-rates. Hier staan die prima-koers ("PRIME") en die beleidskoers.
- Jou bank se webwerf of jou bondslening-stawe wys ook die prima-koers wat hulle gebruik. Dit is die koers wat op jou eie lening van toepassing is.

As jy wil hê ek moet dit weer probeer, kan jy vir my 'n Google-soek-opstelling gee (`WEB_SEARCH_PROVIDER=google` met die API-sleutel en search engine ID). Dan kan ek die nuutste besluit en datum direk nagaan en jou met bronne terugkom.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *9.0 s*

Net **één** instelling in `config/.env.example` kies die LLM direk, maar daar is ook 'n groep wat oor die LLM se gebruik en koste gaan.

**Die LLM self**

- **`LLM_PROFILE=anthropic`**: Kies watter LLM-instellings gelaai word. `anthropic` gebruik Claude (`config/.env.anthropic`) en vereis `ANTHROPIC_API_KEY` in `.env.secrets`. `ollama` gebruik 'n plaaslike model (`config/.env.ollama`), wat gratis is. Jy kan dit hier verander, of net vir een run: `LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli`.
- Die kommentaar waarsku ook dat 'n `LLM_*`-instelling wat in hierdie lêer geskryf word, dié van die profiel oorskryf. Laat hulle dus uit tensy jy dit doelbewus wil vassit.

**Gebruik en koste (almal in kommentaar, dus opsioneel)**

- **`USAGE_LEDGER`** (standaard aan): Skryf een JSON-reël per model-versoek na `usage-JJJJ-MM.jsonl` (tokens, model, tyd; nie die prompt self nie). `off` skakel dit af.
- **`LOG_DIRECTORY`**: Waar die gebruikslêer gestoor word. Buite Docker moet dit gestel wees, anders is die ledger af met 'n waarskuwing. 'n Absolute pad is veiliger.
- **`USAGE_IN_CHAT`**: Wys na elke antwoord in Rider hoeveel tokens, koste en totale sessie-gebruik was.
- **`PRICES_FILE`** / **`PRICES_URL`**: Waar die pryse vandaan kom. Standaard is dit `config/prices.json`.
- **`HOST_CULTURE`** / **`DISPLAY_CURRENCY`**: Watter geldeenheid koste in gewys word. `DISPLAY_CURRENCY` oorskryf `HOST_CULTURE`.
- **`RATES_URL`**, **`RATES_FORMAT`**, **`RATES_FILE`**: Waar wisselkoerse vandaan kom. Standaard is dit Frankfurter (ECB se verwysingskoerse), met `config/rates.json` as terugval. `RATES_URL=off` gebruik net die lêer.

Die lêer self verwys ook na `docs/MODELS.md` vir die modelle en wat hulle kos, en `ANTHROPIC_API_KEY` hoort in `.env.secrets`, nie in hierdie lêer nie.

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* FileHistory · *Delegations:* — · *11.2 s*

**Louis Marais** het die meeste aan `AgentEngine.cs` verander: 31 van die 32 commits in die lêer se geskiedenis (die 32ste is die eerste commit, "Louis Agent: self-hosted AI coding agent for .NET", ook deur "Louis"). Die naam is in die log soms "louis marais" en soms "Louis Marais", maar dit lyk na dieselfde persoon.

Die laaste verandering was op **13 Oktober 2026**, commit `b9287a1`: "F22 step 1: GetToday, for every session".

Ek het net die aantal commits getel, nie die aantal reëls wat verander is nie. As jy wil weet wie die meeste kode geskryf het, kan ek dit ook nagaan.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* BashRun, BashRun, BashRun · *Delegations:* — · *13.5 s*

Daar is **912 toetsattribute** onder `tests/louis-agent.core.tests`:

- **669** `[Test]`
- **243** `[TestCase(...)]`

Daar is geen `[Fact]` of `[Theory]` nie, so dit is NUnit. Die 912 tel elke attribuut, nie elke metode nie. 'n Metode met drie `[TestCase]`-lyne tel dus as drie. Ek het nie die unieke metodes apart getel nie, so die werklike aantal metodes is 'n bietjie laer as 912. Die gids het 74 `.cs`-lêers (sonder `bin` en `obj`).

Die lêers met die meeste toetse is `PaymoToolsTests.cs` (57), `DevOpsToolsTests.cs` (55), `LlmOptionsTests.cs` (52) en `WebToolsTests.cs` (44).

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *3.3 s*

Hier is 'n kort boodskap vir jou ma:

*Ma,*

*Baie geluk met jou verjaardag! Dankie dat jy altyd daar is, met 'n sterk skouer, 'n warm kos en die beste raad, al weet ek nie altyd dadelik dit nie. Ek hoop jou dag is vol lag, rustigheid en al die dinge wat jy graag het. Ek is baie lief vir jou.*

*Alles van die beste,*
*[Jou naam]*

Wil jy hê ek moet dit meer grappig, meer formeel of persoonliker maak? Gee my net 'n bietjie meer inligting oor julle, soos 'n spesiale herinnering of 'n grap tussen julle, dan maak ek dit eg van julle af.

