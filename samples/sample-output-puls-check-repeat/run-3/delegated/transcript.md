## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["web"], "personality": "puls-researcher"}] · *22.2 s*

Die nuutste stabiele weergawe van .NET is **.NET 10**. Dit is 'n LTS-weergawe (langtermyn-ondersteuning) en is op **11 November 2025** vrygestel.

Daar is ook al 'n volgende een op pad: **.NET 11** is nou by release candidate-stadium (11.0.0-rc.2) en is geskeduleer vir GA op **10 November 2026**. Dit is dus nog nie die stabiele weergawe nie. Die nuutste patch vir .NET 10 is 10.0.13, wat op 13 Oktober 2026 uitgekom het.

Ondersteuning vir .NET 10 duur tot **November 2028**. Die amptelike bronne verskil effens oor die presiese einddatum: die launch-artikel sê 10 November 2028, terwyl die huidige aflaaibladsy en die dotnet/core-README 14 November 2028 sê. Ek sou die nuutste bladsye vertrou, maar as die presiese dag saak maak, gaan dit self op die Microsoft-bladsy na.

**Bronne:**
- https://dotnet.microsoft.com/en-us/download/dotnet (aflaaibladsy, wys 10.0.13 en 11.0.0-rc.2)
- https://github.com/dotnet/core/tree/main/release-notes (release-notes-indeks, vir die GA-datums)
- https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/ ("Announcing .NET 10", 11 November 2025)

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *10.8 s*

Die projek is 'n selfgehoste KI-kodeagent vir .NET. Dit werk met regte gereedskap op 'n repository: lêers, git, `dotnet`, Python, PowerShell, bash, webssoektogte, Azure DevOps en Paymo.

Jy praat daarmee deur JetBrains Rider, 'n webapp, 'n HTTP API of die terminal. Standaard loop dit op Claude (Anthropic), maar Ollama en OpenAI-versoenbare eindpunte werk ook.

Die projek bevat ook 'n orkestreerder-demonstrasie (proof of concept) wat foute in 'n mikrodiens kan opspoor en laat regmaak, plus dokumentasie, Docker-opstelling (`docker compose`) en 'n MCP-bediener. Sleutels en instellings staan in `config/.env` en `config/.env.secrets`.

Bron: `README.md` in die werkruimte. Die lêer is in Engels, so ek het die opsomming in Afrikaans gegee.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["git"], "personality": "puls-historian"}] · *19.7 s*

Die laaste drie commits op die huidige branch, nuutste eerste. Al drie is deur louis marais gemaak op 14 Oktober 2026.

**1. `785ea96` (00:55:22): "TODO: relook at how often Puls delegates"**
- Net `TODO.md`, 6 reëls bygevoeg.
- Dis 'n nota vir later. Die delegasie-tjek het tot dusver gevind dat delegeer duurder is as net een agent wat antwoord, behalwe vir chat en sommige webnavorsing.

**2. `3c8ee04` (00:50:52): "The delegation check runs each mode several times (--repeat N)"**
- `samples/puls-check/run_check.py`, +55 / −4.
- Die toets kan nou elke modus meermaal laat loop met `--repeat N`. Elke lopie skryf na `run-N/`, en daar is 'n opsomming (`summary.json` en `summary.md`) met gemiddelde, laagste en hoogste koste per modus en vraag.
- Die rede: een lopie per modus het die uitgangslyn met 15% laat wissel.

**3. `873e94d` (00:50:06): "FX14: a sub-session's prompt has a budget"**
- Voeg 'n prompt-begroting vir sub-sessies by, by verstek 100k tokens (`SUB_SESSION_PROMPT_BUDGET`; 0 skakel dit af).
- Die kern is in `ToolCallGuards.cs` en `AgentEngine.cs`. As die volgende prompt na verwagting die begroting oorskry, hou die versoek sy gereedskap maar gaan oor na toolmodus None, sodat die spesialis met wat hy het antwoord.
- Die rede: 'n lêer-spesialis het elke toetslêer gelees, die prompt het tot 298k tokens gegroei, en dit was duur.
- Ook gewysig: `AgentOptions.cs`, `config/.env.example`, `LogMessage.cs`, `TODO.md`, `docs/fixes/README.md`, en nuut: `docs/fixes/FX14-sub-session-prompt-budget.md` en `SubSessionPromptBudgetTests.cs`. Totaal 9 lêers, +209 / −4.

Let wel: die beskrywings is afgelei uit die commit-boodskappe en lêernaamme. Ek het nie die volledige diffs gelees nie. As jy presies wil weet wat in die kode verander het, sê net en ek kan die diff van een van die commits laat nagaan.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files", "python"], "personality": "puls-analyst"}] · *17.7 s*

Dit is 'n telling oor lêers, so ek gee dit aan die analis wat 'n skrip kan laat loop. Ek kry dan die getalle terug.Onder `/workspace/louis-agent.core` is daar **109 .cs-lêers**, en hulle het saam **11 125 reëls**.

As jy net die reëls tel waar daar iets staan (dus sonder leë reëls), is dit **9 384**.

Die `bin`- en `obj`-gidse is uitgesluit, asook verborge gidse, sodat gegenereerde lêers nie meetel nie. Geen lêer was onleesbaar as teks nie.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *8.5 s*

Hier is 'n paar opsies:

- **Skelm**: perfek vir 'n hond wat altyd iets uitgevreet het.
- **Bliksem**: vinnig, energiek en 'n bietjie onbetroubaar. Die Afrikaanse slang vir "vinnig" pas ook.
- **Flitsie**: vir die hond wat soos 'n flits oor die erf hardloop.
- **Sprinkaan**: want Jack Russells spring hoog en in alle rigtings.
- **Donner**: as hy hard blaf of by die deur instorm.
- **Boerewors**: 'n speelse een wat by die braai hoort. Hy sal waarskynlik ook probeer om van die grond af te eet.
- **Biltong**: as hy wit met bruin of swart kolle is en 'n bietjie taai is.
- **Sokkie**: as hy wit voete het, of altyd by jou enkels rondhang.
- **Boetie**: 'n maat wat altyd saam met jou rondloop.
- **Koekie**: soet genoeg om jou te laat vergeet van die gate in die tuin.

Wat is hy soos? As jy my sy kleur of sy grootste kattekwaad vertel, kan ek die lys kleiner en meer persoonlik maak.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* GetToday, StartSubSession · *Delegations:* [{"toolsets": ["web"], "personality": "puls-researcher"}] · *71.5 s*

**Die prima uitleenkoers is volgens my beste bevinding 10,75%.** Dit is 'n berekening, nie 'n direkte bevestiging nie.

**Hoe ek daaraan kom**
- Die SARB se beleidskoers (voorheen repo-rente) is volgens Moneyweb na die MPC-besluit van **23 September 2026** verhoog van 6,75% na **7,25%**, 'n verhoging van 0,50 persentasiepunt.
- Banke bereken prima gewoonlik as repo + 3,5 persentasiepunte. 7,25% + 3,5 = **10,75%**.

**Wanneer het dit laas verander?**
- Die laaste verandering was dus die verhoging op 23 September 2026. Die koers was daarvoor 6,75% (Moneyweb, 26 Maart 2026, het bevestig dat die SARB die koers toe op 6,75% gehou het).
- Ek kon nie bevestig op watter datum die nuwe prima koers by die banke van krag geword het nie. Gewoonlik pas banke die volgende dag aan, maar dit het ek nie in 'n bron gesien nie.
- Ek kon ook nie seker maak of daar in Mei of Julie 2026 iets verander het nie.

**Bronne**
- Moneyweb, "Sarb ups repo rate to 7.25%" (23 Sep 2026): https://www.moneyweb.co.za/news/economy/sarb-ups-repo-rate-to-7-25/
- Moneyweb, "Sarb holds rate steady at 6.75%" (26 Mar 2026): https://www.moneyweb.co.za/news/economy/sarb-holds-rate-steady-at-6-75/
- Moneyweb SARB-nuusblad: https://www.moneyweb.co.za/tag/sarb/
- SARB, monetêre beleid: https://www.resbank.co.za/en/home/what-we-do/monetary-policy

Die kanse is goed dat 10,75% korrek is, maar die artikels het ek net op opskrif-vlak gesien, nie volledig gelees nie. As jy seker moet wees (vir 'n lening of 'n kontrak), kyk gerus na die SARB se persverklaring van 23 September op resbank.co.za, en vra jou bank watter prima koers hulle nou gebruik.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *28.5 s*

In `config/.env.example` is er net **één** instelling wat direk oor die LLM gaan:

- **`LLM_PROFILE=anthropic`** (reël 7): kies watter profiel gebruik word. `anthropic` is Claude en vereis `ANTHROPIC_API_KEY` in `.env.secrets`. `ollama` is plaaslik en gratis. Volgens die kommentaar in reël 3–6 oorskryf 'n `LLM_*`-instelling wat jy in hierdie lêer skryf die profiel s'n.

Model, temperatuur, maksimum tokens, timeout, endpoint en API-sleutel staan nie in hierdie lêer nie. Die lêer verwys na:

- `config/.env.anthropic` en `config/.env.ollama`, waar die werklike LLM-instellings waarskynlik is.
- `.env.secrets`, vir `ANTHROPIC_API_KEY`.

Ek het daardie lêers nie gelees nie. As jy wil hê ek moet dit ook nagaan, sê net watter een, of ek kan al drie laat nagaan.

Die ander instellings wat in die lêer voorkom, gaan oor gebruik en koste, nie oor die model self nie. Die belangrikste is:

- **`USAGE_LEDGER`** (reël 12, uitgekommentarieer): skryf een logreël per modelversoek. Standaard aan.
- **`SUB_SESSION_PROMPT_BUDGET=100000`** (reël 42): die maksimum tokens vir 'n sub-sessie se prompt. `0` skakel dit af. Dit is die naaste aan 'n tokenlimiet in die lêer.

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["git"], "personality": "puls-historian"}] · *30.3 s*

**Louis Marais** het die meeste aan `AgentEngine.cs` verander: 32 van 33 commits, as ek die drie naamvariante saamtel.

Die laaste verandering was op **14 Oktober 2026**, commit `873e94d`, met die boodskap "FX14: a sub-session's prompt has a budget".

- Louis Marais: 27 commits
- louis marais (kleinletters): 5
- Louis: 1

Ek het aangeneem dat die drie variante dieselfde persoon is, maar ek het dit nie met e-posadresse nagegaan nie. Die datums en hashes kom uit `git log`, en die hashes is kort (7 karakters). Die geskiedenis volg hernoemings, so ouer commits van voor 'n eventuele hernoeming is ingesluit.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files", "python"], "personality": "puls-analyst"}] · *104.3 s*

Dit is 'n telling oor jou lêers, so ek gee dit aan 'n spesialis wat die kode kan deursoek en tel.Daar is **ongeveer 730 test-metodes** onder `tests/louis-agent.core.tests`. Die telling is nie 100% seker nie.

- **673 metodes** met `[Test]`
- **57 metodes** met `[TestCase]`, wat saam **247 `[TestCase]`-attribute** is. Elke attribute is 'n aparte geval, so die uitvoerbare toetsgevalle is 673 + 247 = **920**.
- Geen `[TestCaseSource]`, `[Theory]` of `[Fact]` nie.

Oor die onsekerheid: 'n ruwe teksoutelling vind 252 `[TestCase]`-attribute, en die skrip het 247 gekoppel aan metodes. Dit beteken 5 kon nie opgespoor word nie, so die 57 metodes en die 247 kan 'n paar af wees. Die 673 `[Test]`-telling stem wel met 'n aparte regex-telling. Die skrip is ook nie perfek met elke C#-vorm nie. `obj/`-lêers is uitgesluit, en 75 `.cs`-lêers is getel, waarvan 73 toetse bevat.

As jy 'n presiese getal nodig het, sal 'n `dotnet test --list-tests` in die projek dit met die compiler bevestig. Dan kan ek ook die per-lêer-tabel gee as jy dit wil hê.

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *2.4 s*

Hier is 'n kort boodskap vir jou ma:

*Ma, baie geluk met jou verjaardag! Dankie dat jy altyd daar is, met 'n warm hart, 'n goeie raad en die beste koskos. Ek hoop jou dag is so lekker en liefdevol soos jy vir ons almal is. Geniet elke oomblik van jou spesiale dag. Ek is lief vir jou!*

Wil jy dit meer persoonlik hê, met 'n herinnering of 'n grap wat net julle twee verstaan? Gee my net 'n bietjie inligting en ek pas dit aan.

