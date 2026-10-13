## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, DotNetSdkInfo, FetchUrl, FetchUrl, FetchUrl, FetchUrl, GetToday, FetchUrl · *Delegations:* — · *24.7 s*

Die nuutste **volledige (GA) weergawe** van .NET is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support), en Microsoft het dit op **11 November 2025** vrygestel.

**Stand van sake vandag (14 Oktober 2026):**

- **.NET 10.0.13** is die nuutste patch. Die download-bladsy dateer dit 13 Oktober 2026, dus het dit gister uitgekom.
- **.NET 11.0** is nog in Release Candidate-fase (11.0.0-rc.2). Dit is nie die finale weergawe nie. Ek het nie 'n finale GA-datum gevind nie.
- **.NET 9.0** se ondersteuning eindig op 10 November 2026. **.NET 8.0** (ook LTS) eindig op dieselfde datum volgens die download-bladsy. Die 8.0-datum wat op die bladsy staan, is onseker, so kontroleer dit as dit saak maak.

**Ondersteuning vir .NET 10:** Die aankondiging sê dit loop tot 10 November 2028. Die download-bladsy wys 14 November 2028, so daar is 'n klein verskil tussen die twee bronne. Gaan die amptelike ondersteuningsbeleid na as jy presies moet weet.

**Iets om te let op in hierdie werkspasie:** Die masjien het die **.NET 10.0.401 SDK** en **runtime 10.0.12**. Dit is een patch agter op 10.0.13. Vir die meeste werk maak dit nie saak nie, maar jy kan opdateer as jy die nuutste sekuriteitsregstellings wil hê.

**Bronne:**
- Download-bladsy (weergawes, ondersteuning, RC-inligting): https://dotnet.microsoft.com/en-us/download/dotnet
- .NET 10-weergawe-bladsy: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- "Announcing .NET 10" op die .NET-blog (vrystellingsdatum, LTS-status): https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/

'n Woordjie oor die bronne: die webpoging om te soek het misluk, so ek het die amptelike bladsye direk gelees. Die datums kom dus van daardie bladsye self.

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *5.9 s*

Die projek is **Louis Agent**, 'n selfgehoste KI-kodeagent vir .NET. Hy werk op jou repo met regte gereedskap (lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo) en praat met jou deur Rider, 'n webapp, 'n HTTP API of die terminaal.

1. **Jy kan dit op verskillende maniere gebruik.** Daar is 'n Rider-koppelvlak (ACP), 'n Blazor-webapp met lêerblaaier, git-veranderinge en takke, 'n HTTP API met gestroomde antwoorde, 'n CLI en 'n MCP-bediener. Almal deel dieselfde kern in `louis-agent.core`.

2. **Dit loop standaard op Claude (Anthropic)**, maar Ollama en OpenAI-kompatibele eindpunte werk ook. Elke antwoord wys ook watter gereedskap gebruik is en wat dit gekos het.

3. **Dit is veilig en maklik om op te stel.** Lêers buite die werkruimte en geheime lêers word geblokkeer, en die API bind net op `127.0.0.1`. Die webapp stoot nooit na afstand toe nie. Opstel gaan via Docker: kopieer die `.env`-voorbeelde, sit jou Anthropic-sleutel in, en begin met `docker compose`. Daarna is die webapp op http://127.0.0.1:5080.

Daar is ook 'n proof-of-concept-demo (`src/louis-agent.orchestrator`) waar twee agente tien foute in 'n mikrodiens regmaak, elke regstelling op eie tak, sonder om iets te stoot.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* GetLog, ShowCommit, ShowCommit, ShowCommit · *Delegations:* — · *11.5 s*

Hier is die laaste drie commits, nuutste eerste. Almal is deur Louis Marais gemaak op 14 Oktober 2026.

**1. `3c8ee04`: Die delegasie-toets loop elke modus verskeie kere (`--repeat N`)**
- Een lopie per modus het die basislyn tot 15% tussen lopies laat swaai, dus was dit te onseker om oor delegasie op koste te besluit.
- `run_check.py` het 'n `--repeat N`-opsie gekry. Elke lopie skryf na `run-N/`, en daar is `summary.json` en `summary.md` met die gemiddelde, laagste en hoogste koste per modus en per vraag.
- Voortgangsreëls word nou gespoel, so 'n agtergrondlopie wys waar dit is.
- Lêer: `samples/puls-check/run_check.py` (55 bygevoeg, 4 uitgehaal).

**2. `873e94d`: FX14: 'n Sub-sessie se prompt het 'n begroting**
- 'n Lêers-only spesialis het elke toetslêer gelees, en die prompt het tot 298k tokens gegroei. Na 100k word elke versoek vyf keer duurder. Een vraag het so $0.278 gekos.
- Die wag skat nou die volgende prompt voor elke versoek. Met `SUB_SESSION_PROMPT_BUDGET` (standaard 100k, 0 is af) hou die versoek sy gereedskap, maar met toolmodus `None`, so die spesialis antwoord met wat hy al het.
- Die gebruiker se eie sessies het geen begroting nie.
- 9 lêers verander (209 bygevoeg). Die kern is `ToolCallGuards.cs`, `AgentOptions.cs` en `AgentEngine.cs`. Daar is ook 'n nuwe toetslêer, `SubSessionPromptBudgetTests.cs`, en `docs/fixes/FX14-sub-session-prompt-budget.md`.

**3. `30f1b3d`: FX13: elke spesialis, en Puls wanneer dit kies, hoor wat gereedskapsoproepe kos**
- Die instruksies het vae "wees doeltreffend" vervang met 'n konkrete reël. In elke `puls-*`-rol gebruik 'n spesialis die minste oproepe, omdat elke resultaat op elke latere versoek weer betaal word.
- In `agents-skills.md` is 'n sub-sessie koud begin, en die antwoord word twee keer geskryf, so die reël is om een keer per taak te delegeer met die gereedskap wat die antwoord in die minste oproepe gee.
- 9 lêers verander, insluitend die vyf `puls-*`-persoonlikhede, `agents-skills.md`, `PersonalitiesTests.cs` en `ModelRequestSnapshot.json`.

Wil jy een van hulle in meer detail sien, of die diff vir een van die lêers?

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* BashRun · *Delegations:* — · *6.7 s*

Daar is **109 .cs-lêers** onder `louis-agent.core`, met **11 096 reëls** altesaam.

Ek het die `bin`- en `obj`-gidse uitgesluit, want dis build-uitset en nie jou eie kode nie. Daar is nog 3 gegenereerde .cs-lêers in daardie gidse, wat nie by die telling ingesluit is nie.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *5.7 s*

Hier is drie:

1. **Skollie**: 'n Jack Russell is 'n klein skelm met 'n lopie in sy oë, so dis perfek. Dit beteken ook "rascal", en hy gaan dit elke dag verdien.
2. **Boela**: 'n Ou Afrikaanse naam wat klink soos 'n oom wat altyd 'n sak lekkers in sy broekssak het. Die hond gaan dit nooit glo nie, maar hy gaan darem sy naam ken.
3. **Klonkie**: Vir 'n hond wat by elke deur klop, elke bal agterna jaag en nie wil stilsit nie. Klonkie beteken ook 'n klein ding met baie geraas.

As jy wil hê hy moet 'n meisie wees, sal ek vir jou nog drie gee. Watter ry hom die beste?

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* WebSearch, FetchUrl, FetchUrl, FetchUrl, WebSearch, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl · *Delegations:* — · *39.9 s*

Die websoektoestel is geblokkeer, so ek gaan direk na die bronne self kyk.**Die huidige prima uitleenkoers is 10,75%.** Dit is my beste afleiding, nie 'n direkte lesing nie. Prima is by konvensie die SARB se repo- (of beleidskoers-) koers plus 3,5 persentasiepunte. Die beleidskoers is 7,25%, so prima is 10,75%.

**Wanneer het dit laas verander?**
- Die MPK het die beleidskoers met 25 basispunte na **7,25%** verhoog. Volgens die SARB het dit **effektief vanaf 25 September 2026** geword, en die besluit was eenparig.
- Die vorige MPK-verklaring (Julie 2026) het die koers nog op 7% gehad. Prima sou toe 10,5% gewees het.
- Die Oktober 2026-hersiening sê die koers is oor April tot Oktober 2026 altesaam 50 basispunte verhoog.
- Die volgende MPK-aankondiging is op **19 November 2026**.

**Bronne:**
- SARB, *Statement of the Monetary Policy Committee, September 2026*: https://www.resbank.co.za/en/home/publications/publication-detail-pages/statements/monetary-policy-statements/2026/september (beleidskoers 7,25%, effektief 25 September; volgende aankondiging 19 November 2026)
- SARB, *Statement of the Monetary Policy Committee, Julie 2026*: https://www.resbank.co.za/en/home/publications/publication-detail-pages/statements/monetary-policy-statements/2026/july (beleidskoers toe 7%)
- SARB, *Monetary Policy Review, Oktober 2026*: https://www.resbank.co.za/en/home/publications/publication-detail-pages/reviews/monetary-policy-review/2026/october (kumulatiewe verhoging van 50 bp tot 7,25%)

**Twee kanttekeninge:**
- Die SARB se "Current Market Rates"-bladsy wat die prima wys, laai met JavaScript en ek kon die getal nie direk lees nie. Die soekfunksie was ook geblokkeer, so ek het nie 'n bank se prima-tabel nagegaan nie. As jy 'n lening of bedrag moet bereken, kyk die prima by jou bank (of die SARB se markkoersbladsy) voordat jy daarop staatmaak.
- Die 25 September-datum kom uit die SARB se eie woorde. Die MPK-besluit self is ook op 23 September aangekondig volgens die aankondigingskedule, so as jy 'n presiese datum vir 'n dokument nodig het, kyk die amptelike PDF.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *10.2 s*

Daar is net **één** instelling wat werklik aan staan en direk oor die LLM gaan. Die res is kommentaar of verwant:

**Aktief:**
- **`LLM_PROFILE=anthropic`**: kies watter model-profiel gebruik word. `anthropic` gebruik Claude en vereis `ANTHROPIC_API_KEY` in `.env.secrets`. `ollama` gebruik 'n plaaslike model wat gratis is. Jy kan dit ook vir een run oorskryf, byvoorbeeld `LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli`.

**Kommentaar, dus nie aktief nie:**
- **`LLM_*`-instellings (algemeen):** as jy so 'n instelling hier skryf, oorskryf dit die profiel s'n. Laat hulle dus uit tensy jy 'n spesifieke waarde wil vaslê.
- **`USAGE_LEDGER`** (standaard aan): skryf een JSON-reël per modelversoek (tokens, model, tyd, nie die prompt self nie) na `logs/usage-JJJJ-MM.jsonl`.
- **`LOG_DIRECTORY`**: waar die gebruiksboek gestoor word. Buite Docker moet dit gestel wees, anders is die boek af met 'n waarskuwing.
- **`USAGE_IN_CHAT`**: wys elke beurt se tokens, koste en sessie-totaal in die chat.
- **`PRICES_FILE` / `PRICES_URL`**: waar die pryse vandaan kom. Standaard is dit `config/prices.json`.
- **`HOST_CULTURE` / `DISPLAY_CURRENCY`**: in watter geld koste gewys word (bv. `en-ZA` of `ZAR`).
- **`RATES_URL` / `RATES_FORMAT` / `RATES_FILE`**: waar wisselkoerse vandaan kom. Standaard is dit Frankfurter (ECB), met `config/rates.json` as terugval. `RATES_URL=off` gebruik net die lêer.
- **`SUB_SESSION_PROMPT_BUDGET`** (standaard 100000): as 'n sub-sessie se prompt hierdie grens sou oorskry, antwoord die spesialis uit wat hy reeds het in plaas van meer tools te roep. `0` skakel dit af.

Die bykomende instellings **`AGENT_FUNCTION`**, **`AGENT_PEERS`** en die MCP-instellings gaan oor die agent self en nie oor die model nie. Die modelkeuse en pryse word in `docs/MODELS.md` en `config/prices.json` verduidelik.

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* FileHistory · *Delegations:* — · *11.5 s*

**Louis Marais** het die meeste aan `AgentEngine.cs` verander. Die laaste verandering was op **14 Oktober 2026**, commit `873e94d`: "FX14: a sub-session's prompt has a budget".

- Die lêer het 33 commits gehad, en Louis is by 32 daarvan betrokke. Sy naam verskyn as "louis marais" en "Louis Marais", so ek neem aan dit is dieselfde persoon. Die oorblywende commit is die aanvanklike een, `574d89c`, deur "Louis".
- Ek het getel volgens aantal commits, nie volgens hoeveel reëls verander is nie. As jy wil weet wie die meeste reëls bygedra het, kan ek dit nagaan.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* BashRun, BashRun, PythonRun · *Delegations:* — · *14.6 s*

Daar is **731 toetsmetodes** onder `tests/louis-agent.core.tests`.

- **673** metodes met `[Test]`
- **58** metodes met `[TestCase]`. Dit is 252 datarye, dus gemiddeld sowat 4 gevalle per metode.

Ek het 75 `.cs`-lêers nagegaan en `bin`/`obj` oorgeslaan. Daar is geen `[Fact]` of `[Theory]` nie, so dit is NUnit. Die 252 `[TestCase]`-reëls sou jy as 'n ruwe telling kon gebruik, maar die 731 is die werklike aantal metodes wat die runner sien.

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *3.1 s*

Hier is 'n kort boodskap vir jou ma:

**Liewe Ma,**

Happy verjaardag! Ek hoop jy het 'n heerlike dag vandag, met baie liefde, lag en 'n bietjie spoil. Dankie dat jy altyd daar is, vir die raad, die knuffels en die kos wat net soos joune smaak. Ek is so trots om jou as my ma te hê.

Geniet elke oomblik van jou dag!

Baie liefde,
[Jou naam]

As jy wil hê dit moet meer persoonlik wees, vertel my gerus iets van haar, soos 'n grap wat julle deel of iets wat sy geniet, dan maak ek dit spesiaal vir haar.

