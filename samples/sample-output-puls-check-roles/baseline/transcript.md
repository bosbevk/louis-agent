## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, FetchUrl, WebSearch, FetchUrl · *Delegations:* — · *19.5 s*

Die nuutste **stabiele** weergawe is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support), dus ondersteun Microsoft dit tot **10 November 2028**.

**Vrystelling:** .NET 10 is op **11 November 2025** amptelik vrygestel, saam met Visual Studio 2026 en .NET Conf 2025.

**Wat nog aan die gang is:** .NET 11 is nog 'n release candidate (RC), nie GA nie. Die amptelike aflaaibladsy wys 11.0.0-rc.2 as die nuutste RC. Dit is goed om te toets, maar moenie dit in produksie gebruik nie.

**Bronne:**
- Amptelike aflaaibladsy (dotnet.microsoft.com): https://dotnet.microsoft.com/en-us/download/dotnet. Hier staan .NET 10.0 as "(latest)" en .NET 11.0 as RC.
- .NET 10-aankondiging (.NET Blog, 11 November 2025): https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/. Hier kry jy die LTS-status en die ondersteuningsdatum.

Die aflaaibladsy se datums vir die patch-weergawes lyk vreemd en kan moontlik vanaf 'n cache kom. Vir die vrystellingsdatum van .NET 10 is die blogpos die betroubaarste bron.

Wil jy weet watter .NET SDK op jou masjien geïnstalleer is? Dit kan ek vir jou nagaan.

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *6.1 s*

**Louis Agent** is 'n self-gehoste AI-kodeagent vir .NET, gebou op Microsoft.Extensions.AI. Dit werk op jou repository met regte gereedskap (lêers, git, `dotnet`, Python, PowerShell, bash, webs\u00ear, Azure DevOps en Paymo) en standaard op Claude.

1. **Dit werk met jou kode en take.** Die agent kan lêers lees en wysig, git-veranderinge hersien en stage, bou en toets .NET-projekte, en werk-items in Azure DevOps en tydsregistrasie in Paymo bestuur. Jy kan dit van Rider, 'n webapp, 'n HTTP API of die terminal af gebruik.

2. **Dit is veilig en plaaslik.** Lêergereedskap bly binne die werkruimte en raak nie geheime-lêers (`.env`, sleutels, sertifikate) aan nie. Die API en webapp luister net op `127.0.0.1`, die webapp stoot nooit na afstand toe nie, en die agent stoot net as jy dit vra.

3. **Dit het 'n orkestreerder-demo.** `src/louis-agent.orchestrator` is 'n tweede agent wat foute in 'n mikrodiens monitor, die oplosbare foute laat regmaak (elk op sy eie tak, nooit gestoot nie) en die regstellings self verifieer. `samples/run-demo.ps1` wys dit in Docker met tien bugs.

Wil jy meer weet oor opstelling, dokumentasie of die demo? Dan kan ek daarvoor ook soek.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* GetLog, ShowCommit, ShowCommit, ShowCommit · *Delegations:* — · *12.4 s*

Die laaste drie commits, van nuutste na oudste:

**1. `4a8ba3f` – A specialist without a role gets one from its toolsets** (14 Okt 2026, 00:03)
- Die delegation-toets het gewys dat Puls net in een van vier delegasies 'n rol deurgee. Nou kry 'n sub-sessie sonder rol een outomaties, op grond van sy toolsets. Web → researcher, python/bash/powershell → analyst, git → historian, files → reader. 'n Rol wat die ouer self deurgee, bly soos dit is.
- Die reëls staan in `Skills/personalities/roles.json`. Die keuse self is in `AgentEngineFactory.cs` en `Personalities.cs`.
- Verander ook `AgentSessions.cs`, `AgentSessionsTests.cs`, `PersonalitiesTests.cs`, die docs (`CLAUDE.md`, `agents-skills.md`), die `.csproj` en `ModelRequestSnapshot.json`.
- 10 lêers, +92 / −6.

**2. `556e9d7` – The Puls delegation check: a baseline, then delegated, saved as a sample** (13 Okt 2026, 23:53)
- Nuwe `samples/puls-check/run_check.py`. Dit stel vyf vrae (web, lêers, geskiedenis, getalle, chat) twee keer aan Puls se API: eers aan een agent met al die toolsets, dan aan Puls met sy spesialiste. Dit lees dan die ledger terug en vergelyk tokens en koste.
- Die eerste uitslag: 60% minder tokens, maar 31% duurder. Goedkoper net vir web-navorsing en chat. Rolle is in net een van vier delegasies deurgegee.
- Die resultate is in `samples/sample-output-puls-check` gestoor, met `README.md`, transcripts, usage-lêers en `results.json`.
- 10 lêers, +744 reëls, niks verwyder nie.

**3. `8cdfa67` – PR #28 review: PDFs, location precision, the time zone, Done when, goal map** (13 Okt 2026, 23:46)
- `FetchUrl`: enige fout met 'n PDF lei nou tot `DocumentUnreadable`, want PdfPig gooi meer as een soort uitsondering. 'n Toets stuur 'n beskadigde PDF.
- Die blaser se posisie gaan na Nominatim met twee desimale (± 1 km, 'n dorp), nie vier nie.
- Die tydsone word by elke nuwe chat ingestuur, sodat `GetToday` in die gebruiker se tyd werk sonder 'n ligging. Die ligging word steeds net gestuur as dit ingestel is.
- "Done when" by F20, F21 en F22 is bygewerk, plus die F22-plan oor die tydsone-besluit. `GOAL.md` het ook veranderinge by skills 3, 4, 10 en 19.
- 11 lêers, +81 / −19.

Wil jy een van hulle in meer diepte hê, of die diff van een spesifieke lêer?

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* FindFiles, BashRun · *Delegations:* — · *10.5 s*

Daar is **109 .cs-lêers** onder `louis-agent.core` en hulle het saam **11 012 reëls**.

Die FindFiles-soekopdrag het 112 lêers gevind, maar 3 daarvan is onder `obj/` (gegenereerde build-lêers vir net-10.0). Ek het dié uitgesluit. As jy hulle ook wil tel, is dit 112 lêers en 11 050 reëls.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *9.2 s*

Hier is drie:

1. **Skelm**: Dit beteken so iets soos slinkse rakker. Perfek vir 'n hondjie wat jou sokkies steel en dan onskuldig kyk.
2. **Koeksister**, of net **Koeksie**: Klein, bruin, en taai aan die buitekant maar sag binne-in. Net soos 'n Jack Russell.
3. **Bliksem**: Liefdevolle vloek vir 'n hond wat die hele tuin rondjaag. As die bure dit hoor, sê jy dis sy naam.

As jy wil hê ek moet nog een bysit, sou ek **Sokkie** kies. Dis 'n sagte naam vir 'n hond wat sokkies bymekaarmaak.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* WebSearch, WebSearch, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, GetToday · *Delegations:* — · *27.9 s*

Ek kon nie die huidige prima-koers vir jou bevestig nie. Die soektjeneste het geweier, en die SARB se markkoersbladsy het die syfers nie gelaai nie. Vandag is 14 Oktober 2026, so enigiets wat ek hier kan vind, kan al verouderd wees.

**Wat ek wel kon kry:**

- **Wikipedia se prima-lysy (bron wat Trading Economics gebruik):** Suid-Afrika se prima-koers was **10,50%** as op **Julie 2025**. Dit is die laaste datum wat ek kon bevestig, en dit is al ver terug. Bron: https://en.wikipedia.org/wiki/Prime_rate
- **Uit my eie kennis (nie live nagegaan nie):** Die SARB het in November 2025 die repo-koers met 25 basispunte verlaag na **6,75%**. Dan sou prima **10,25%** wees, want prima is gewoonlik repo + 3,5%. Ek is redelik seker van die verlaging in November 2025, maar ek kan nie bevestig of daar sedertdien nog besluite was nie. Die MPC vergader gewoonlik ses keer per jaar, so daar kon al veranderinge wees.

**Wanneer het dit laas verander?** Die laaste verandering wat ek kon bevestig, was die verlaging in Julie 2025 (na 10,50%). Die laaste verandering wat ek uit my eie kennis ken, was November 2025 (na 10,25%). Daar kon sedertdien nog een of meer besluite gewees het.

**Hoe kry jy die korrekte antwoord vinnig:**

1. Gaan na die SARB se MPC-verklarings op **resbank.co.za** (soek na "monetary policy statement" of "MPC statement"). Die datum en die repo-koers staan in die eerste paragraaf.
2. Prima is repo + 3,5%, so tel 3,5 by die repo-koers.
3. Enige groot bank (FNB, Standard Bank, ABSA, Nedbank, Capitec) se webwerf wys ook die huidige prima-koers.

As jy wil hê ek moet dit weer probeer, sal ek 'n soekdiensverskaffer met 'n API-sleutel nodig hê. Die Google-opstelling in die webgids sal dit beter maak. Gee vir my gerus 'n seker bron (bv. 'n SARB-bladsy of MPC-verklaring), dan lees ek dit en gee jou die presiese datum.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* ReadWorkspaceFile, SearchWorkspace · *Delegations:* — · *14.6 s*

Ek kon nie `config/.env.example` self lees nie. Die lêer is geblokkeer vir my leestool, en ek gaan nie daarom kring nie. Ek het wel in die kode en die README gesoek, en dit is die LLM-instellings wat ek gevind het:

- **`LLM_PROFILE`**: Watter LLM-profiel gelaai word. `anthropic` (laai `config/.env.anthropic`, Claude Haiku 5.5) of `ollama` (laai `config/.env.ollama`).
- **`LLM_PROVIDER`**: Die verskaffer, byvoorbeeld `anthropic`, `ollama` of `openai-compatible`. Die profiel stel dit gewoonlik vir jou.
- **`LLM_MODEL`**: Watter model gebruik word, byvoorbeeld `claude-haiku-5-5` of `llama3.2`. Die profiel stel dit gewoonlik ook. As die model nie tool-calling ondersteun nie, kan die agent geen aksies doen nie.
- **`LLM_ENDPOINT`**: Die URL van die model se diens, nuttig vir Ollama of 'n ander OpenAI-versoenbare bediener.
- **`LLM_API_KEY`**: Die sleutel vir 'n algemene of OpenAI-versoenbare verskaffer.
- **`ANTHROPIC_API_KEY`**: Die Anthropic-sleutel, as jy Claude gebruik.
- **`LLM_SUPPORTS_TOOLS`**: Skakel tool-calling aan of af vir 'n model wat dit nie ondersteun nie.
- **`LLM_THINKING`**: Hoeveel sigbare redenering die model wys: `off`, `low`, `medium` of `high`. Standaard is `medium` vir Anthropic.
- **`LLM_CACHE`**: Prompt-caching op Anthropic. Standaard `on`.
- **`LLM_CACHE_TTL`**: Hoe lank die cache leef: `5m` (standaard) of `1h`. `1h` kos meer by die eerste keer, maar spaar as die prefix dikwels herhaal word.

Let daarop dat dit saamgestel is uit die README, die bronkode en die toets-lêers, nie uit die voorbeeld-lêer self nie. Die voorbeeld kan dus effens verskil, byvoorbeeld in watter sleutels teenwoordig is of watter standaardwaardes ingestel is.

As jy wil hê ek moet die presiese inhoud van `config/.env.example` nagaan, plak die relevante reëls hier, of kyk self gerus na die lêer.

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* FileHistory, Blame, BashRun, BashRun · *Delegations:* — · *35.0 s*

Louis Marais het die meeste daaraan verander. Hy het 30 van die 31 commits gemaak wat die lêer ooit aangeraak het, as jy die geskiedenis volg deur die lêer se naamsveranderinge heen. Die ander een is die oorspronklike commit van 6 Oktober 2026, wat as "Louis" geteken is. Dit lyk na dieselfde persoon, net met 'n ander Git-naam.

- **Laaste verandering:** 13 Oktober 2026, commit `b9287a1`, "F22 step 1: GetToday, for every session".
- **Lyne wat vandag nog daar staan:** Van die 330 lyne is 143 deur "Louis Marais" geskryf. Die ander 187 kom almal uit die oorspronklike commit, toe die lêer nog onder `tools/` was.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* BashRun, PythonRun · *Delegations:* — · *15.5 s*

Daar is **721 toetsmetodes** onder `tests/louis-agent.core.tests`, verspreid oor 72 `.cs`-lêers.

Dit is 'n telling van metodes, nie van reëls nie. 'n Metode met meerdere `[TestCase]`-reëls tel net een keer. Die ruwe telling van attribuutreëls is 901: 665 `[Test]` plus 236 `[TestCase]`. Die verskil is omdat sommige metodes meer as een `[TestCase]` het.

As jy die aantal individuele toetsgevalle wil hê, wat in die uitvoer wys, is dit meer as 901, want elke `[TestCase]`-reël is 'n aparte run. Ek kan dit uitwerk as jy wil.

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *3.3 s*

Hier is 'n kort boodskap vir jou ma:

**Liewe Ma,**

Baie geluk met jou verjaardag! Ek hoop jou dag is so lekker soos jy self is: warm, gul en vol lekker dinge. Dankie dat jy altyd daar is, met 'n raad, 'n knuffel of 'n pot koffie wanneer ek dit die nodigste het. Ek is so dankbaar vir jou.

Geniet die dag, Ma. Jy verdien elke bietjie van die lekkerte.

Baie liefde,
[Jou naam]

As jy wil hê dit moet meer grappig, meer emosioneel, of met 'n spesifieke herinnering van julle twee, sê net en ek pas dit aan.

