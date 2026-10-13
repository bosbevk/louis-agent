## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, DotNetSdkInfo, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl · *Delegations:* — · *19.4 s*

Die nuutste **volledige (GA)** weergawe is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support), dus dit kry drie jaar ondersteuning. Die nuutste patch is **10.0.13**, en die releases-indeks lys die datum as **13 Oktober 2026**.

**Wanneer is .NET 10 vrygestel?** Volgens my eie kennis is .NET 10 op **11 November 2025** vrygestel, by .NET Conf. Die bladsye wat ek gelees het, het nie die GA-datum self gewys nie, so verifieer dit gerus.

Let ook op die volgende:
- **.NET 11** is nog 'n release candidate. Die download-bladsy wys **11.0.0-rc.2** (STS, go-live), so dit is nog nie GA nie.
- **.NET 9** en **.NET 8** is in onderhoudsfase en verval op 10 November 2026.
- Hierdie masjien het **.NET SDK 10.0.401** en runtime **10.0.12** geïnstalleer, wat by .NET 10 pas.

**Bronne:**
- https://dotnet.microsoft.com/en-us/download/dotnet (die oorsig van alle weergawes, ondersteuningsfases en datums)
- https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json (die amptelike releases-indeks, dieselfde data as die masjienleesbare bron)
- https://dotnet.microsoft.com/en-us/download/dotnet/10.0 (die .NET 10-aflaaibladsy; ek kon die GA-datum nie uit hierdie bladsy kry nie)

Die webwoordsoek het misluk (DuckDuckGo het geweier), so ek het die bronne direk gelees.

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *6.5 s*

Louis Agent is 'n selfgehoste KI-kodeagent vir .NET, gebou op Microsoft.Extensions.AI. Dit werk op jou repository met regte gereedskap (lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo) en praat met jou deur Rider, 'n webapp, 'n HTTP API of die terminal.

1. **Dit is 'n AI-assistent wat self werk.** Dit beantwoord vrae en voer take uit op die kode, soos lees, bou, toets, git-veranderinge hersien en commits maak. Antwoorde stroom in, insluitend die model se redenasie en elke gereedskapsoproep, en elke antwoord wys hoeveel tokens en koste dit gebruik het.

2. **Dit het veelvuldige ingange.** Dieselfde kern dryf 'n Rider-ACP-bediener, 'n Blazor WebAssembly-webapp met lêerbladeerder, git-veranderinge en takke, 'n JSON-API met SSE-stroming, 'n CLI en 'n MCP-bediener. Dit loop standaard op Claude (Anthropic), maar Ollama en OpenAI-versoenbare eindpunte werk ook.

3. **Dit is veilig en gekonfigureer met lêers.** Lêergereedskap bly binne die werkruimte en raak nie geheime lêers aan nie. Die webapp stoot nooit na afstand toe nie, en die API is net op `127.0.0.1` beskikbaar tensy jy `AGENT_API_KEY` stel. Instellings en geheime staan in `config/`, en die agent se persoonlikheid en instruksies is Markdown-lêers in `Skills/`. Dit bevat ook 'n orkestrator-demo wat foute in 'n mikrodiens regstel, elke regstelling op 'n eie tak vastlê, en dit self verifieer.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* ShowCommit, ShowCommit, ShowCommit · *Delegations:* — · *8.4 s*

Die laaste drie commits is almal van Louis Marais, op 14 Oktober 2026 tussen 00:50 en 00:55.

**1. `785ea96`: "TODO: relook at how often Puls delegates"** (00:55)
- Net `TODO.md` is verander, 6 nuwe reëls bygevoeg.
- Die notas sê dat elke delegasie-toets tot dusver goedkoper was om net een agent te laat antwoord, behalwe vir gesprekke en sommige webnavorsing. Dis dus 'n reminder om later weer na delegasie te kyk.

**2. `3c8ee04`: "The delegation check runs each mode several times (--repeat N)"** (00:50)
- `samples/puls-check/run_check.py`: 55 reëls bygevoeg, 4 uitgehaal.
- Een lopie per modus het die basislyn met 15% laat swaai, so die toets kan nou `--repeat N` gebruik. Elke lopie gaan in `run-N/`, en daar is `summary.json` en `summary.md` met gemiddelde, laagste en hoogste koste per modus en per vraag. Progress-reëls word nou geflush, so 'n agtergrondlopie wys waar dit is.

**3. `873e94d`: "FX14: a sub-session's prompt has a budget"** (00:50)
- 9 lêers, 209 reëls bygevoeg, 4 uitgehaal. Dis die grootste een.
- Probleem: 'n lêers-only spesialis het elke toetslêer gelees en sy prompt het tot 298k tokens gegroei. Bo 100k word elke versoek vyf keer duurder, en een vraag het $0.278 gekos.
- Oplossing: voor elke versoek in 'n sub-session skat die guard die volgende prompt-grootte. By `SUB_SESSION_PROMPT_BUDGET` (standaard 100k, 0 = af) hou die versoek sy tools, maar met tool mode None, so die spesialis antwoord met wat hy al het. Jou eie sessies het geen budget nie.
- Ook veranderd: `ToolCallGuards.cs` (42 reëls), `AgentOptions.cs` (12 nuwe config-reëls), `AgentEngine.cs`, `LogMessage.cs`, `config/.env.example`, 'n nuwe `docs/fixes/FX14-sub-session-prompt-budget.md` plus 'n inskrywing in `docs/fixes/README.md`, en nuwe toetse in `SubSessionPromptBudgetTests.cs` (83 reëls).

Wil jy die volledige diff van een van hulle sien, sê net watter een.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* BashRun · *Delegations:* — · *6.5 s*

Onder `louis-agent.core` is daar **112 `.cs`-lêers** met saam **11 134 reëls**.

Die ttel sluit `bin/` en `obj/` in. As jy net die bronkode wil tel, sonder build-uitset, is dit **109 lêers** met **11 096 reëls**. Die verskil is 3 lêers, meestal gegenereerde kode uit die build.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *6.4 s*

1. **Sjakkals**: Dis 'n speelse woordspeling op *Jack*, want 'n Jack Russell is darem maar net 'n klein jakkals met 'n baie groot persoonlikheid.
2. **Wors**: Kort, vet en lekker, en as hy die hele dag agter jou aan hardloop, sal hy jou ook lyk soos 'n boerewors wat op die braai draai.
3. **Bliksem**: Vir die hond wat aanhou om oor die werf te skiet. Net jammer dat jy hom nie in die hoenderhok se rigting moet roep nie.

As jou hond 'n rooi-bruin pels het, sal **Rooibos** ook pas.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* WebSearch, FetchUrl, WebSearch, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, WebSearch, FetchUrl · *Delegations:* — · *30.7 s*

Ek kon nie die huidige prima-koers bevestig nie, so ek gaan nie vir jou 'n getal raai nie.

**Wat ek probeer het:**
- Die SARB se **Current Market Rates**-bladsy (resbank.co.za) laai die syfers dinamies, so my leser kry net die bladsy se raamwerk sonder die waardes.
- Die webson-soekdiens het elke keer geweier (DuckDuckGo blokkeer die outomatiese soektog). Dit kan ek nie omseil nie.
- Die SARB se MPC-verklarings-bladsy het ook geen bruikbare inhoud gegee nie.

**Wat ek wel bevestig het:**
- Die SARB werk nou met die **SARB Policy Rate** (SPR), nie meer die "repo-koers" nie. Dit is sedert die oorgang na 'n surplus-raamwerk in 2022 so. Die prima-koers beweeg gewoonlik saam met hierdie rentekoers, meestal op 3,5 persentasiepunte bo.
- Die SARB kondig die beleidskoers elke **twee maande** aan, na afloop van die Monetêre Beleidskomitee (MPC) se vergadering.
- Die inflasiedoelwit is 3%, met 'n band van plus minus 1 persentasiepunt.

**Die laaste syfer wat ek kon vind:** Wikipedia se lys van prima-koerse (bron: Trading Economics en Wêreldbank) gee **10,50%** vir Suid-Afrika, met datum **Julie 2025**. Dit is hopelik nie meer die huidige koers nie, want die SARB het sedertdien moontlik weer die koers verander. Ek kan dit nie bevestig nie.

**Die vinnigste manier om die regte antwoord te kry:**
1. Gaan na die SARB se *Current Market Rates*-bladsy en kyk na die **PRIME**-reeks. Dit is die amptelike bron.
2. Of kyk by jou bank se webwerf of in hul leningsberekenaar, want die prima-koers wat hulle gebruik, is hul eie publikasie.

Bronne wat ek gelees het:
- https://www.resbank.co.za/en/home/what-we-do/monetary-policy
- https://www.resbank.co.za/en/home/what-we-do/statistics/key-statistics/current-market-rates
- https://en.wikipedia.org/wiki/Prime_rate (lys van prima-koerse, Suid-Afrika 10,50%, Julie 2025)

As jy wil hê ek moet weer probeer, sal dit help as die webson-soekdiens op Google gestel word (`WEB_SEARCH_PROVIDER=google` met die API-sleutel en soek-ID). Dan kan ek ook nuusbronne soek oor die laaste verandering.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *14.3 s*

Net een instelling is regtig aktief in die lêer, en die res is kommentaar. Hierdie gaan oor die LLM:

**Aktief**

- **`LLM_PROFILE=anthropic`**: kies watter LLM-instellings gebruik word. `anthropic` is Claude (vereis `ANTHROPIC_API_KEY` in `.env.secrets`). `ollama` is 'n plaaslike model wat gratis is. Jy kan dit ook vir een run oorskakel met `LLM_PROFILE=ollama dotnet run ...`.

**Kommentaar (nie aktief nie, maar dis LLM-verwant)**

- **`LLM_*`-instellings** (in die kommentaar genoem): as jy 'n `LLM_...`-waarde hier skryf, oorskryf dit die profiel s'n. Laat hulle dus uit tensy jy presies een wil vassit.
- **`USAGE_LEDGER`**: aan by verstek. Dit skryf een JSON-reël per model-versoek (tokens, model, tyd, nooit die prompt self nie) na `logs/usage-JJJJ-MM.jsonl`. Sit dit op `off` om dit af te skakel.
- **`LOG_DIRECTORY`**: waar die gebruiksboek geskryf word. Buite Docker moet dit gestel wees, anders is die boek af met 'n waarskuwing. 'n Absolute pad is veiliger.
- **`USAGE_IN_CHAT`**: wys elke beurt se tokens, koste en sessie-totaal in die chat (Rider).
- **`PRICES_FILE`** / **`PRICES_URL`**: waar die pryse vandaan kom. Standaard is `config/prices.json`.
- **`RATES_URL`**, **`RATES_FORMAT`**, **`RATES_FILE`**: wisselkoerse vir die koste-wys. Verstek is Frankfurter (ECB); `RATES_URL=off` gebruik net die lêer.
- **`HOST_CULTURE`** / **`DISPLAY_CURRENCY`**: in watter geld die koste gewys word (bv. `en-ZA` of `EUR`).
- **`SUB_SESSION_PROMPT_BUDGET`**: die maksimum tokens vir 'n subsessie se prompt (verstek 100 000). As die volgende versoek dit sou oorskry, antwoord die spesialis net uit wat hy alreeds het. `0` skakel dit af.

Wil jy hê ek moet ook kyk na `docs/MODELS.md` vir wat die modelle kos?

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* FileHistory · *Delegations:* — · *13.4 s*

Louis Marais het die meeste aan `AgentEngine.cs` verander. Hy is by **32 van die 33 commits** (ongeveer 97%). Die ander een is die eerste commit, "Louis Agent: self-hosted AI coding agent for .NET", van "Louis". Die naam is net in twee skrywe-variante in git aangeteken (*louis marais* en *Louis Marais*), maar dis dieselfde persoon.

Die laaste verandering was **14 Oktober 2026**, commit `873e94d`: "FX14: a sub-session's prompt has a budget".

Hierdie telling is per commit, nie per reël kode nie. As jy wil weet wie die meeste reëls bygedra het, kan ek dit ook nagaan.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* BashRun, PythonRun · *Delegations:* — · *22.8 s*

Daar is **731 toetsmetodes** onder `tests/louis-agent.core.tests`, verspreid oor 73 `.cs`-lêers.

- Die `[Test]`-attribuut kom 673 keer voor.
- Die `[TestCase]`-attribuut kom 252 keer voor. Een metode met meerdere `[TestCase]`-reëls tel as **een** toetsmetode, so die 731 is die werklike aantal metodes en nie die som van die twee nie.

Die grootste lêers is `DevOpsToolsTests.cs` (55), `PaymoToolsTests.cs` (52) en `WorkspaceToolsTests.cs` (38). Die telling is 'n skrip wat die kode lees, so as daar 'n baie ongewone metode-handtekening is, kan dit 'n enkele een mis. Vir 'n amptelike getal kan jy `dotnet test` in daardie projek laat loop en die "Total tests"-reël kyk.

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *2.8 s*

Hier is 'n kort boodskap vir jou ma:

---

Liewe Ma,

Baie geluk met jou verjaardag! Dankie dat jy altyd daar is, met 'n warm hart, 'n goeie raad en die beste koffie in die huis. Ek hoop jou dag is vol lag, lekker kos en mense wat jou graag sien. Geniet dit, want jy verdien elke bietjie daarvan.

Ek is baie lief vir jou.

Liefde,
[Jou naam]

---

Wil jy dit meer persoonlik hê, met 'n spesifieke herinnering of grap wat julle deel? Gee my net 'n paar besonderhede en ek maak dit reg.

