## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, GetToday, FetchUrl, FetchUrl, FetchUrl · *Delegations:* — · *16.5 s*

Die nuutste **stabiele** weergawe van .NET is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support), so dit kry drie jaar ondersteuning. Volgens my kennis is .NET 10 op **11 November 2025** vrygestel. Die bladsy wat ek gelees het, wys nie die oorspronklike vrystellingsdatum nie, so daardie datum is uit my geheue, nie uit 'n bron wat ek vandag nagegaan het nie.

Die stand vandag (14 Oktober 2026), volgens die Microsoft-afraaiblad:

- **.NET 10.0** (LTS, aktief ondersteun): laaste vrystelling 10.0.13, op 13 Oktober 2026. Ondersteuning eindig op 14 November 2028.
- **.NET 9.0** (STS, in onderhoud): laaste vrystelling 9.0.21, op 13 Oktober 2026. Ondersteuning eindig op 10 November 2026, dus hou jou projekte daarop dop.
- **.NET 11.0** is nog nie GA nie. Dit is in Release Candidate-fase, met 11.0.0-rc.2 wat op 13 Oktober 2026 uitgekom het.

Bronne:
- https://dotnet.microsoft.com/en-us/download/dotnet (die afraaiblad met weergawes, datums en ondersteuningstatus)
- https://dotnet.microsoft.com/en-us/download/dotnet/10.0 (die .NET 10-aflaaibladsy; ek het dit gekry, maar die deel wat ek kon lees, het nie die GA-datum gewys nie)

Ek kon nie 'n soekenjin gebruik nie, want DuckDuckGo het die soektog geblokkeer, en die GitHub-releases-indeks het 404 gegee. As jy presies die GA-datum van .NET 10 moet bevestig, is https://github.com/dotnet/core/tree/main/release-notes die plek om te kyk.

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *5.7 s*

**Louis Agent** is 'n self-hosted AI-kodeagent vir .NET, gebou op Microsoft.Extensions.AI. Dit werk op jou repository met regte tools (lêers, git, `dotnet`, Python, PowerShell, bash, webson­oek, Azure DevOps en Paymo) en praat met jou via Rider, 'n web-app, 'n HTTP API of die terminal. Standaard gebruik dit Claude, maar Ollama en OpenAI-kompatibele endpoints werk ook.

- **Vele maniere om dit te gebruik:** Daar is 'n Rider-koppeling (ACP), 'n Blazor-web-app met lêerblaaier, git-veranderinge en takke, 'n JSON API met streaming-events, 'n CLI en 'n MCP-server. Almal deel dieselfde `louis-agent.core`.

- **Vinnige begin en konfigurasie:** Dit loop in Docker (`docker compose ... up -d --build api`), en jy open dan http://127.0.0.1:5080. Instellings en geheime sleutels staan in `config/.env` en `config/.env.secrets`, en elke antwoord wys ook wat dit gekos het.

- **Veiligheid en 'n demo:** Lêer-tools bly binne die werkruimte en raak nie geheime lêers aan nie. Die web-app stoot nooit na afstand toe nie; die agent doen dit net as jy vra. Daar is ook 'n orkestrator-demo waarin twee agente tien foute in 'n mikrodiens regmaak, elke fix op sy eie tak, en dan kan jy die fixes self nasien en saamvoeg.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* ShowCommit, ShowCommit, ShowCommit · *Delegations:* — · *8.6 s*

Hier is die laaste drie commits, almal deur Louis Marais op 14 Oktober 2026:

**1. `785ea96`, 00:55: TODO: relook at how often Puls delegates**
- Verander net `TODO.md` (6 nuwe reëls).
- Die nota sê elke delegasie-toets tot dusver het gevind dat delegeer duurder is as wanneer een agent self antwoord. Die uitsondering is chat en sommige webnavorsing.

**2. `3c8ee04`, 00:50: The delegation check runs each mode several times (--repeat N)**
- Verander `samples/puls-check/run_check.py` (55 bymekaar bygevoeg, 4 uit).
- Die toets kan nou elke modus `N` keer laat loop. Elke lopie skryf na `run-N/`, plus `summary.json` en `summary.md`, met die gemiddelde, laagste en hoogste koste per modus en per vraag.
- Die vorige rede: een lopie per modus het die basislyn met 15% laat wissel, dus was dit te onseker om op koste te besluit.
- Voortgangslyne word nou ge-flush, so 'n agtergrondlopie wys waar dit is.

**3. `873e94d`, 00:50: FX14: a sub-session's prompt has a budget**
- Verander 9 lêers (209 bygevoeg, 4 uit), insluitend `ToolCallGuards.cs`, `AgentOptions.cs`, `AgentEngine.cs`, `config/.env.example`, nuwe dokumentasie in `docs/fixes/FX14-sub-session-prompt-budget.md` en 'n nuwe toetslêer `SubSessionPromptBudgetTests.cs`.
- Probleem: 'n lêer-spesialis het elke toetslêer gelees en sy prompt het tot 298k tokens gegroei. Anderkant 100k kos elke versoek ongeveer vyf keer meer ($0.278 vir een vraag).
- Oplossing: voor elke versoek skat die guard die volgende prompt-grootte. By `SUB_SESSION_PROMPT_BUDGET` (standaard 100k, 0 = af) hou die versoek sy gereedskap maar met tool mode `None`, sodat die spesialis antwoord met wat hy al het. Die gebruiker se eie sessies het geen budget nie.

Die twee nuutste commits is albei oor die delegasie-toets. Die eerste een sê die resultate tot dusver dui daarop dat Puls minder moet delegeer, en die tweede een is die gereedskap om dit behoorlik te meet.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* BashRun · *Delegations:* — · *6.3 s*

Daar is **112 .cs-lêers** onder `louis-agent.core`, met saam **11 134 reëls**.

As ek die `bin`- en `obj`-gidse uitsluit (build-uitset, nie jou eie kode nie), bly **109 lêers** en **11 096 reëls** oor.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *7.8 s*

Hier is drie wenke:

1. **Jakkals.** Dis die duidelikste knipoog: Jack Russell, Jakkals. Die jakkals is ook 'n slim, lewendige klein hondjie in die veld.
2. **Boela.** 'n Ou-skool Afrikaanse naam wat by 'n hond met 'n baie hoë dunk van homself pas. "Boel" is ook 'n ou vriend, so hy is seker van 'n warm ontvangs.
3. **Blitsie.** Van "blitsig", vinnig. Jack Russells is die blitsigste honde wat daar is, so hy sal die naam waarmaak.

Watter een klink na hom?

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* WebSearch, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, FetchUrl, GetToday · *Delegations:* — · *29.1 s*

**Kort antwoord:** Ek kan nie met vertroue die huidige prima-koers bevestig nie. Die bronne wat ek vandag kon oopmaak, is ouer as vandag (14 Oktober 2026), en die SARB-webblad het die koerse nie in die teks gewys nie.

**Wat ek wel gevind het:**
- Wikipedia se lys van prima-koerse (gebaseer op Trading Economics en die Wêreldbank) wys **10,50%** vir Suid-Afrika, met datum **Julie 2025**. Dit is die laaste datum wat ek kon sien.
- Dit pas by die SARB se repo-koers van 7,00% destyds. Prima is gewoonlik die repo-koers plus 3,5 persentasiepunte.

**Wat ek uit my eie kennis weet:** Die SARB het in November 2025 weer met 25 basispunte verlaag. Dit sou die repo na 6,75% en prima na 10,25% gebring het. Ek is nie seker van enige veranderinge in 2026 nie, so moet dit asseblief nie as die huidige koers neem sonder om te bevestig nie.

**Hoe jy die presiese antwoord kry:**
1. **SARB se Current Market Rates-bladsy:** https://www.resbank.co.za/en/home/what-we-do/statistics/key-statistics/current-market-rates. Die prima-koers en die datum van laaste verandering staan daar, maar die bladsy het vir my nie die syfers gewys nie. Dit is die beste bron as jy dit op jou eie rekenaar oopmaak.
2. **SARB se MPC-verklarings:** https://www.resbank.co.za/en/home/publications/statements/mpc-statements. Hier sien jy presies watter besluite die Monetêre Beleidskomitee in 2026 geneem het. Prima verander net wanneer die repo-koers verander.
3. **Jou bank se webblad** (byvoorbeeld FNB, Absa, Standard Bank of Nedbank). Hulle wys die prima-koers wat hulle werklik gebruik. Ek kon FNB se bladsy nie oopmaak nie, want dit het 'n sekuriteitskontrole gehad.

Wil jy hê ek moet die SARB-bladsy weer probeer, of sal jy die syfers self nagaan? Dan kan ek help om die verandering in 2026 uit die MPC-verklarings uit te pluis.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* ReadWorkspaceFile · *Delegations:* — · *9.9 s*

Net **een** instelling in die fil kies die LLM direk: **`LLM_PROFILE=anthropic`**. Dit kies watter profiel gelaai word:

- **`anthropic`**: Claude. Dit vereis `ANTHROPIC_API_KEY` in `.env.secrets`. Die instellings is in `config/.env.anthropic`.
- **`ollama`**: lokaal en gratis. Die instellings is in `config/.env.ollama`.

Jy kan dit hier verander, of net vir een keer in die shell, byvoorbeeld `LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli`.

Die fil waarsku ook dat enige `LLM_*`-instelling wat hier geskryf word, die profiel se waarde oorskryf. Hier is dit net `LLM_PROFILE`, dus niks anders word oorskryf nie.

Die res hang wel aan die LLM, maar gaan oor koste, gebruik en budget eerder as oor die model self:

- **`USAGE_LEDGER`** (standaard aan): skryf een JSON-reël per modelversoek (tokens, model, tyd, nooit die prompt-teks nie) na `logs/usage-JJJJ-MM.jsonl`.
- **`LOG_DIRECTORY`**: waar die gebruiksboek gestoor word. Buite Docker moet dit gestel word, anders is die boek af.
- **`USAGE_IN_CHAT`**: wys die tokens en koste na elke antwoord in Rider se chat.
- **`PRICES_FILE` / `PRICES_URL`**: waar die pryse vandaan kom. Standaard is dit `config/prices.json`.
- **`HOST_CULTURE` / `DISPLAY_CURRENCY`**: in watter geldeenheid koste gewys word. `HOST_CULTURE` is byvoorbeeld `en-ZA`, en `DISPLAY_CURRENCY` oorskryf dit (bv. `EUR`).
- **`RATES_URL` / `RATES_FORMAT` / `RATES_FILE`**: waar die wisselkoerse vandaan kom. Standaard is dit Frankfurter (ECB), met `config/rates.json` as terugval. `RATES_URL=off` gebruik net die lêer.
- **`SUB_SESSION_PROMPT_BUDGET`** (standaard 100000): as 'n spesialis se prompt die budget sou oorskry, antwoord dit eerder uit wat dit al het, sonder om nog gereedskap te roep. `0` skakel dit af.

Die modelle en wat hulle kos, staan in `docs/MODELS.md`. Wil jy hê ek moet 'n spesifieke een van hierdie nagaan, of die `.env.anthropic`- of `.env.ollama`-lêer ook lees?

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* FileHistory · *Delegations:* — · *15.8 s*

**Louis Marais** het die meeste aan `AgentEngine.cs` verander. Hy is in 32 van die 33 commits, en die eerste commit is onder "Louis" gemaak, waarskynlik dieselfde persoon met 'n kortere naam.

Die laaste verandering was **14 Oktober 2026**, commit `873e94d`: *"FX14: a sub-session's prompt has a budget"*.

Dit tel commits, nie reëls kode nie. As jy wil weet wie die meeste reëls in die huidige lêer geskryf het, kan ek `git blame` laat loop.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* BashRun, BashRun · *Delegations:* — · *13.3 s*

Daar is **925** toetsattribute onder `tests/louis-agent.core.tests`:

- **673** `[Test]`
- **252** `[TestCase]`

Die tellings kom uit 75 `.cs`-lêers, met `bin` en `obj` uitgesluit. Ek het net lyne getel wat met die attribuut begin, so 'n attribuut wat op dieselfde lyn as iets anders staan, sal nie gereken word nie.

Let wel: een metode met drie `[TestCase]`-attribute tel as drie hier. As jy die getal van *unieke* toetsmetodes wil hê, moet ek die metodenaam na elke `[TestCase]` ook tel. Die grootste lêers is `LlmOptionsTests.cs` (52 attribute), `PaymoToolsTests.cs` (57) en `DevOpsToolsTests.cs` (55).

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *2.9 s*

Hier's 'n warm boodskap vir jou ma:

---

Liewe Ma,

Happy verjaardag! Ek hoop jou dag is so lekker soos jy dit verdien. Dankie vir alles wat jy al vir ons gedoen het, die liefde, die geduld en die kos wat altyd net reg proe. Ek is so dankbaar om jou as my ma te hê.

Geniet jou dag ten volle, en mag die jaar voor jou vol vrede, gesondheid en baie rede tot lag wees.

Baie liefde,
[Jou naam]

---

As jy wil hê dit moet meer persoonlik klink, vertel my gerus iets spesifiek oor jou ma ('n grap, 'n herinnering of iets wat sy graag doen) en ek pas dit aan.

