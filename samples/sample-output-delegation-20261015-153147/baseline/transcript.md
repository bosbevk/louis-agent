## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, DotNetSdkInfo, FetchUrl · *Delegations:* — · *16.0 s*

Die nuutste stabiele weergawe van .NET is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support), en dit is vrygestel op **11 November 2025**.

- **Ondersteuning:** .NET 10 word ondersteun tot **November 2028**.
- **Ander ondersteunde weergawes:** .NET 9 (STS) tot November 2026, en .NET 8 (LTS) ook tot November 2026.
- **Op hierdie masjien:** Die .NET 10 SDK **10.0.401** en runtime **10.0.12** is geïnstalleer.

Ek het nie nagegaan of .NET 11 al in preview is nie. As jy vir produksiewerk beplan, is .NET 10 die veilige keuse.

**Bronne:**
- Microsoft Lifecycle, .NET and .NET Core: https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core
- Microsoft Learn, .NET releases, patches, and support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *7.4 s*

Die projek is **Louis Agent**, 'n selfgehoste KI-kodeagent in .NET, gebou op Microsoft.Extensions.AI. Dit werk op jou repository met regte gereedskap soos lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo. Jy praat daarmee via Rider, 'n webapp, 'n HTTP API of die terminal.

1. **Die kodeagent.** Antwoorde stroom in soos hulle geskryf word, insluitend die model se denkwerk en elke gereedskapsoproep. Elke antwoord wys ook wat dit gebruik en gekos het. Dit loop standaard op Claude, maar Ollama en OpenAI-versoenbare eindpunte werk ook.

2. **Puls, die algemene assistent.** Puls praat gewone Suid-Afrikaanse Afrikaans en het self geen gereedskap nie. Dit deel elke taak uit aan 'n spesialis (navorsing, lêers, git, berekeninge of algemene werk), en skryf dan die antwoord self. Die Werk-paneel wys elke delegasie terwyl dit gebeur, en 'n meting toon dat delegering gewoonlik ongeveer 'n derde duurder is as een agent wat alles self doen.

3. **Veiligheid en gebruik.** Lêerhulpmiddels bly binne die werkruimte en raak nooit geheime lêers aan nie. Modelteks word nooit in 'n opdragreël ingeplak nie, en alles luister net op `127.0.0.1`. Dit het ook 'n orkestrator-demo wat foute in 'n mikrodiens regstel, elke regstelling op eie tak vastlê en nie push nie. Begin met Docker: `config/.env` en `config/.env.secrets` opstel, dan die webapp op http://127.0.0.1:5080 laat loop.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* GetLog, ShowCommit, ShowCommit, ShowCommit · *Delegations:* — · *11.2 s*

Die laaste drie commits:

**1. `b7ad0ac`: docs(f27): the asks approved during the build** (Louis, 15 Okt 2026, 17:26)
- Net een lêer verander: `docs/plans/F27-admin-portal.md`, 7 reëls bygevoeg.
- Dis net dokumentasie: die goedgekeurde versoeke uit die bou is by die F27-plan aangeteken.

**2. `68dbaa5`: feat(f27): step 7, the live run as a tree** (Louis, 15 Okt 2026, 17:25)
- Twee lêers: `Pages/Admin/Benchmarks.razor` (143 bygevoeg, 80 uitgevee) en `wwwroot/css/app.css` (21 bygevoeg).
- Die tabel en die aparte kletslys is saamgevoeg in een boomstruktuur. Die vlakke is:
  - modus (met die toolsets, en koste, tokens en versoeke as dit klaar is)
  - gesprekke
  - beurte
  - die gesprek self, wat binne 'n beurt uittik
- Die huidige node op elke vlak bly oop, afgehandelde nodes vou toe.
- 'n Slegte gebeurtenis breek die replay nie meer af nie; dit gaan na die browser se console, en die totale word daar ook gelog.

**3. `ad68cfa`: docs(f25): a shop's own search result counts as a price** (Louis, 15 Okt 2026, 15:46)
- Twee lêers: `Skills/personalities/puls-cook.md` (6 bygevoeg, 2 uitgevee) en `samples/puls-cook-check/run_check.py` (een reël gewysig).
- Louis het gekies dat 'n winkel se eie soekresultaat as prys tel, as die teksversie van die resultaat die prys wys. Dit word gemerk as "van die soekindeks" met die datum.
- Die produkbladsy word net opgehaal as die resultaat geen prys wys nie.
- Die "unconfirmed"-regex in die toets tel nou ook 'unverified', 'snippet', 'soekresultate' en 'soekindeks'.

Die bron hierbo is die opsomming van lêers en reëls. As jy presies wil sien watter reëls verander het, kan ek die volle diff van enige van die drie wys.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* BashRun · *Delegations:* — · *6.8 s*

Onder `louis-agent.core` is daar **118 .cs-lêers** met saam **11 738 reëls** kode.

Ek het `bin`- en `obj`-gidse uitgesluit, dus gegenereerde lêers tel nie mee nie. Die reëlgetal tel alle reëls, insluitend leë reëls en kommentaar.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *6.8 s*

Hier is drie:

1. **Boela Bliksem.** "Boela" is 'n ou Afrikaanse naam, en "bliksem" is 'n skelmpie. Jack Russells is presies so: klein, vinnig en altyd besig met iets stout.
2. **Kiepie.** Dis 'n liefdevolle koosnaam vir iets klein, en hy sal dit nie eens agterkom dat hy ondeund is nie.
3. **Stoffel.** 'n Klassieke Afrikaanse naam met 'n ou-wêreld-charme. Dis perfek vir 'n hond wat dink hy is die baas van die stoep.

Wil jy een wat meer by die kleur of persoonlikheid van jou hond pas? Vertel my gerus.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* WebSearch, WebSearch, GetToday, FetchUrl · *Delegations:* — · *18.3 s*

Die huidige prima uitleenkoers is **10,75%**. Dit het op **25 September 2026** verander, toe die SARB se Monetary Policy Committee die repo-rentekoers met 25 basispunte na **7,25%** verhoog het.

**Hoe dit werk:** Prima is gewoonlik die repo-koers plus 3,5 persentasie punte. Met repo op 7,25% kom prima dus op 10,75% uit.

**Wat in 2026 gebeur het:**
- **Maart 2026:** Repo bly op 6,75%, so prima bly op 10,25%.
- **28 Mei 2026:** Repo is verhoog na 7%, en prima het na 10,50% gegaan.
- **23 Julie 2026:** Repo is op 7% gehou.
- **25 September 2026:** Repo is verhoog na 7,25%, en prima het na 10,75% gegaan. Die besluit was unaniem, en die SARB noem die globale olie-skok as rede.

Die volgende MPC-besluit is op **19 November 2026**.

**Bronne:**
- SARB, *Statement of the Monetary Policy Committee September 2026*: https://www.resbank.co.za/en/home/publications/publication-detail-pages/statements/monetary-policy-statements/2026/september (repo 7,25%, volgende besluit 19 November 2026)
- SARB, *Statement of the MPC July 2026* en *May 2026*: dieselfde domein, onder `.../monetary-policy-statements/2026/july` en `.../2026/may`
- Property24, *Interest rates drop: Repo at 6.75% and prime falls to 10.25%*: https://www.property24.com/articles/interest-rates-drop-repo-at-675-and-prime-falls-to-1025/32878 (die November 2025-verlaging)
- Trading Economics, Suid-Afrika rentekoers: https://tradingeconomics.com/south-africa/interest-rate

**Let wel:** Nedbank se bladsy wys nog 10,50%, maar dit is ouer as die September-besluit. Die SARB-bladsy is die betroubaarste bron. Jou bank kan ook 'n effektiewe datum of marge hê wat afwyk, so as jy 'n lening het, kyk na jou eie kontrakstaat of vra jou bank. Die SARB het in Februarie 2026 ook gepraat oor die moontlikheid om die primakoers heeltemal af te skaf. Ek het nie bevestiging gekry dat dit besluit is nie.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *10.9 s*

Net **één** instelling is 'n echte LLM-instelling: **`LLM_PROFILE`**. Die res raak die model net terloops.

**Die hoofinstelling**

- **`LLM_PROFILE=anthropic`**: kies watter model-lêer gebruik word. `anthropic` gebruik Claude en vereis `ANTHROPIC_API_KEY` in `.env.secrets`. `ollama` gebruik 'n plaaslike model en is gratis. Die profiel laai `config/.env.anthropic` of `config/.env.ollama`. Jy kan dit ook net vir een run verander, bv. `LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli`.
- Die kommentaar sê ook: as jy 'n `LLM_*`-instelling hier in die lêer skryf, oorskryf dit die profiel s'n. Los dit dus uit tensy jy 'n model wil vassit.

**Verwante instellings (koste en gebruik van LLM-versoeke)**

- **`USAGE_LEDGER`** (standaard aan): skryf een JSON-reël per modelversoek na `logs/usage-JJJJ-MM.jsonl` (tokens, model, tyd; nie die prompt self nie). Stel dit op `off` om dit af te skakel.
- **`LOG_DIRECTORY`**: waar die gebruikslogs gestoor word. Gebruik 'n absolute pad as jy buite Docker met `dotnet run` werk.
- **`USAGE_IN_CHAT`**: wys die tokens en koste na elke antwoord in die chat (Rider). Af as dit nie gestel is nie.
- **`PRICES_FILE`, `PRICES_URL`**: waar die prysetabel vandaan kom. Standaard is dit `config/prices.json`.
- **`DISPLAY_CURRENCY`, `HOST_CULTURE`**: in watter geldeenheid koste gewys word. `DISPLAY_CURRENCY=ZAR` sou dus rand gee.
- **`RATES_URL`, `RATES_FORMAT`, `RATES_FILE`**: wisselkoerse. Standaard haal die stelsel dit by Frankfurter (ECB) by opstart; as dit misluk, gebruik dit `config/rates.json`.
- **`SUB_SESSION_PROMPT_BUDGET=80000`**: die maksimum tokens wat 'n spesialis-subsessie se prompt mag bereik. As die volgende versoek dit sou oorskry, antwoord die spesialis met wat dit al het in plaas van om meer gereedskap te roep. `0` skakel dit af.

Die ander instellings in die lêer (agent-funksie, webfoutsoeker, Rider MCP, Docker-paaie, Azure DevOps) het nie met die model self te doen nie.

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* FileHistory, BashRun · *Delegations:* — · *13.3 s*

Louis Marais het die meeste aan `AgentEngine.cs` verander. Die laaste verandering was op **14 Oktober 2026**, commit `873e94d` ("FX14: a sub-session's prompt has a budget").

- **Commits:** 30 van die 33 is deur Louis Marais gemaak. Git sien "Louis Marais" en "louis marais" as twee name, so ek het hulle saamgetel.
- **Reëls:** Hy het ongeveer +313 en −936 reëls bygedra. Die grootste deel van die lêer se huidige inhoud kom dus van die soveelste herskryf.
- **Oorspronklike skepping:** Die eerste commit, "Louis Agent: self-hosted AI coding agent for .NET", is deur "Louis" gemaak en het die lêer met 943 reëls begin.

Die meeste werk aan die lêer het in Oktober 2026 plaasgevind, veral in die refactor-reekse (cl1, cl2, cl3) en die F-stappe rondom gereedskap, gebruik en sub-sessies.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* BashRun, PythonRun · *Delegations:* — · *15.0 s*

Daar is **982 toets-attributes** onder `tests/louis-agent.core.tests`, verdeel oor 76 `.cs`-lêers:

- **716** `[Test]`
- **266** `[TestCase(...)]`

Die 982 is nie dieselfde as die aantal toetsmetodes nie. As 'n metode meer as een `[TestCase]` het, tel dit elke keer. As ek elke metode net een keer tel, is daar **778 toetsmetodes**: 716 gewone `[Test]`-metodes plus 62 metodes wat met `[TestCase]` gedekoreer is.

Die lêers met die meeste toetse is `Tools/Integrations/Paymo/PaymoToolsTests.cs` (57), `Tools/Integrations/DevOps/DevOpsToolsTests.cs` (55), `Tools/Web/WebToolsTests.cs` (63) en `Config/LlmOptionsTests.cs` (52). Die telling is net die bronkode; ek het die tests nie uitgevoer nie.

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *2.7 s*

Hier's 'n warm boodskap vir jou ma:

**Haar verjaardag: Gelukkige verjaardag, Ma!**

Ma, baie geluk met jou verjaardag! Dankie dat jy altyd daar is, met 'n sagte woord, 'n warm kos en 'n oop hart. Ek hoop jou dag is so lekker soos jy vir ons almal is. Geniet elke oomblik, en weet dat ek jou baie liefhet.

Liefde,
[Jou naam]

Wil jy dit meer persoonlik hê, met 'n spesiale herinnering of 'n grappie tussen julle twee? Sê net, dan pas ek dit aan.

