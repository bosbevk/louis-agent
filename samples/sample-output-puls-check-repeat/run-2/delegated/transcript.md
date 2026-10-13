## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["web"], "personality": "puls-researcher"}] · *23.3 s*

Die nuutste GA-weergawe (algemeen beskikbaar, nie preview nie) is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support) en is op **11 November 2025** vrygestel.

- **Ondersteuning:** Drie jaar. Die amptelike blogpos sê dit eindig op 10 November 2028, maar die downloadblad sê 14 November 2028. Die verskil is vier dae, so as dit vir jou belangrik is, kyk na die amptelike bladsy self.
- **Nuwer preview:** .NET 11 is nog nie GA nie. Die nuutste is 11.0.0-rc.2 (release candidate, 13 Oktober 2026). Ek het nie 'n GA-datum vir .NET 11 gevind nie.
- **Ander lyne:** .NET 9 (STS) eindig op 10 November 2026. .NET 8 (LTS) is in onderhoud.

Die datum van vandag, volgens die navorsing, is 14 Oktober 2026. Ek het self nie die bladsye oopgemaak nie; die navorsingsagent het dit gedoen, en die bronne is:

- https://dotnet.microsoft.com/en-us/download/dotnet (downloadblad met die weergawetabel)
- https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/ ("Announcing .NET 10", 11 November 2025)

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *12.7 s*

Die README.md in die werkruimte beskryf **Louis Agent**, 'n self-gehoste KI-kodeeragent vir .NET, gebou op Microsoft.Extensions.AI.

1. **Dit werk op jou repository met gereedskap.** Die agent kan lêers lees, git gebruik, `dotnet`, Python, PowerShell, bash, websoektogte doen, Azure DevOps en Paymo aanspreek.
2. **Jy praat daarmee vanaf verskeie plekke.** Dit kan via Rider, 'n webapp, 'n HTTP-API of die terminal. Standaard loop dit op Claude (Anthropic), maar Ollama en OpenAI-versoenbare eindpunte werk ook.
3. **Dit loop maklik in Docker.** Kopieer die `config/.env`-voorbeelde, stel jou Anthropic API-sleutel en repository-paaie in, en begin dit met `docker compose`. Die webapp is dan op http://127.0.0.1:5080. Vir ontwikkeling het jy die .NET 10 SDK nodig.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["git"], "personality": "puls-historian"}] · *20.5 s*

Ek gaan die git-geskiedenis laat nagaan.Die laaste drie commits in hierdie repository (almal deur Louis Marais op 14 Oktober 2026):

1. **`785ea96`** (00:55): Net 'n nota by `TODO.md` gevoeg. Dit sê dat elke delegasie-toets tot dusver duurder was as een agent wat self antwoord, behalwe by chat en sommige webnavorsing. Geen kode het verander nie.

2. **`3c8ee04`** (00:50): `samples/puls-check/run_check.py` kan nou die delegasie-toets meer as een keer per modus laat loop met `--repeat N`. Elke lopie skryf na `run-N/`, en `summary.json` en `summary.md` wys gemiddelde, laagste en hoogste koste per modus en vraag. Die rede: een lopie per modus het die basislyn met 15% laat wissel.

3. **`873e94d`** (00:50): Die grootste een, 9 lêers (+209, −4). Sub-sessies van spesialiste kry nou 'n prompt-begroting. As die prompt `SUB_SESSION_PROMPT_BUDGET` (standaard 100k tokens, 0 skakel dit af) oorskry, hou die versoek sy gereedskap, maar die spesialis moet met wat hy al het antwoord. Die rede: 'n lêer-spesialis het 298k tokens bereik en elke versoek daarna was vyf keer duurder. Die kode is in `ToolCallGuards.cs`, met toetse en dokumentasie in `docs/fixes/FX14`.

Een ding om te weet: my gereedskap wys net die lêerstatistiek, nie die volledige diff nie. Die opsommings van commits 2 en 3 is dus uit die boodskappe en lêernames afgelei. Ek het die kode self nie gelees nie, en die toetse is nie nagegaan nie. As jy presies wil weet wat in `ToolCallGuards.cs` verander het, kan ek dit laat nagaan.

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files", "python"], "personality": "puls-analyst"}] · *21.0 s*

Ek kan nie self by die lêers uitkom nie, so ek stuur dit na 'n analis wat 'n skrip kan laat tel.Daar is **109 .cs-lêers** onder `/workspace/louis-agent.core`, en hulle het saam **11 125 reëls**.

Die telling sluit die 3 .cs-lêers onder `obj` uit, want dis gegenereerde lêers. As jy álles wil tel, insluitend die `obj`-lêers, is dit 112 lêers. Die reëltelling sluit leë reëls en kommentaar in, en ek het nie die 3 `obj`-lêers se reëls by die 11 125 getel nie.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Delegations:* — · *5.3 s*

Hier is drie:

1. **Skelm**: Jack Russells is slim en steeks, en hulle maak altyd 'n plan om by die hek uit te kom.
2. **Bliksem**: vinnig soos weerlig, en lief gesê, soos 'n mens 'n stout hond se naam sal roep.
3. **Boetie**: want hy gaan die klein broertjie wees wat sy eie baas wil wees.

As jy een wil hê wat by 'n Jack Russell se energie pas, gaan ek vir Bliksem. Die naam is 'n wegholdraaier, maar hy kry dit reg.

## web-2: Wat is die huidige prima uitleenkoers in Suid-Afrika, en wanneer het dit laas verander? Gee jou bronne.

*Tools:* GetToday, StartSubSession · *Delegations:* [{"toolsets": ["web"], "personality": "puls-researcher"}] · *49.0 s*

Die prima koers is na alle waarskynlikheid **10,75%**, maar ek kon dit nie direk by 'n bank of by die Reserwebank bevestig nie. Dit is 'n afleiding.

**Hoe ek daarby uitkom:**
- Die Reserwebank se beleidskoers (repo) is volgens hul *Monetary Policy Review* van 6 Oktober 2026 **7,25%**. Die koers het oor April tot Oktober 2026 met 50 basispunte gestyg.
- Die SA-bankkonvensie is prima = repo + 3,50 persentasepunte. Dus: 7,25% + 3,50 = **10,75%**.

**Wat ek nie kon bevestig nie:**
- **Wanneer die prima laas verander het en met hoeveel.** Die 50 basispunte oor ses maande beteken waarskynlik meer as een verhoging, maar ek weet nie die datum van die laaste een nie. Moenie daarop staatmaak sonder om dit te kyk nie.
- **Die bankkoerse self.** Die webtuistes van Absa, Standard Bank en FNB het nie vir my oopgemaak nie. Nedbank en Capitec is nie nagegaan nie.

**Bronne:**
- Reserwebank, *Monetary Policy Review – October 2026* (6 Okt 2026): https://www.resbank.co.za/en/home/publications/publication-detail-pages/reviews/monetary-policy-review/2026/october
- Reserwebank, *Current Market Rates* (hier staan die prima-koers, maar die bladsy laai dinamies en ek kon die syfer nie lees nie): https://www.resbank.co.za/en/home/what-we-do/statistics/key-statistics/current-market-rates

**Die vinnigste manier om seker te wees:** maak die *Current Market Rates*-bladsy oop, of kyk by jou bank se koersbladsy. Dit sal die amptelike prima-syfer en die datum van die laaste verandering wys.

## files-2: Watter instellings in config/.env.example gaan oor die LLM? Lys hulle met 'n kort verduideliking.

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files"], "personality": "puls-reader"}] · *17.7 s*

In `config/.env.example` is daar eintlik net **één** LLM-instelling wat aktief is:

- **`LLM_PROFILE=anthropic`** (reël 7): kies watter profiel-lêer met die LLM-instellings gebruik word. `anthropic` is Claude en vereis `ANTHROPIC_API_KEY` in `.env.secrets`. `ollama` is plaaslik en gratis.

Daar is ook 'n paar verwante kommentaarreëls, nie aktiewe instellings nie:

- **Reël 5** wys hoe jy die profiel eenmalig kan oorskryf: `LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli`.
- **Reël 6** waarsku dat enige `LLM_*`-reël wat jy in hierdie lêer byvoeg, die profiel se instelling oorskryf. Los dit dus uit tensy jy 'n instelling wil vashaak.
- **`ANTHROPIC_API_KEY`** word net in die kommentaar genoem. Die sleutel self hoort in `.env.secrets`.

Model-naam, basis-URL, temperatuur, maksimum tokens en timeout staan nie in hierdie lêer nie. Dit sal waarskynlik in `config/.env.anthropic` of `config/.env.ollama` wees. Ek het daardie twee lêers nie gelees nie. Sê net as jy wil hê ek moet hulle nagaan.

## git-2: Wie het die meeste aan louis-agent.core/Agent/AgentEngine.cs verander, en wanneer was die laaste verandering?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["git"], "personality": "puls-historian"}] · *25.9 s*

Dit was **Louis Marais** wat die meeste aan `AgentEngine.cs` gedoen het, met 27 van die 33 commits.

- **Laaste verandering:** 14 Oktober 2026, commit `873e94d`, met die boodskap "FX14: a sub-session's prompt has a budget".
- **Ander outeurs:** "louis marais" (5 commits) en "Louis" (1 commit). Dit lyk na dieselfde persoon met verskillende skryfwyses van die naam in git, maar dit is my aanname.

Die telling is die specialis se eie berekening uit die git-log, nie 'n direkte uitvoer nie, en die volledige hash het ek nie gesien nie. As jy presies wil weet wat in daardie laaste commit verander het, kan ek dit ook nagaan.

## numbers-2: Hoeveel toetsmetodes ([Test] of [TestCase]) is daar onder tests/louis-agent.core.tests?

*Tools:* StartSubSession · *Delegations:* [{"toolsets": ["files", "python"], "personality": "puls-analyst"}] · *52.7 s*

Daar is **724 toetsmetodes** onder `tests/louis-agent.core.tests`.

- **667** metodes met `[Test]` of `[Test(...)]`
- **57** metodes met `[TestCase]` (een of meer per metode)
- **0** metodes met albei, dus geen dubbeltelling

Die 57 `[TestCase]`-metodes het saam 247 `[TestCase]`-reëls, maar dit is die aantal datarye, nie metodes nie. Ek het 75 `.cs`-lêers getel, met `bin/` en `obj/` uitgesluit. Die telling het kommentaar en stringe oorgeslaan, so `[Test]` in teks tel nie.

## chat-2: Skryf 'n kort verjaardagboodskap vir my ma in Afrikaans.

*Tools:* — · *Delegations:* — · *2.6 s*

Hier's een wat jy sommer kan gebruik:

---

Ma, alles van die beste vir jou verjaarsdag! 🎂

Dankie dat jy altyd daar is, met 'n warm kos, 'n goeie raad en 'n luisterende oor. Ek hoop jou dag is so lekker soos jy vir ons almal is. Geniet elke oomblik, en mag die jaar voor jou vol liefde, gesondheid en lekker kuiertjies wees.

Baie liefde,
[jou naam]

---

Wil jy dit warmer, lekkerder of meer formeel hê? Of sal ek dit met 'n spesifieke herinnering van julle saam aanpas?

