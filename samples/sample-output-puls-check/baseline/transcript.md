## web: Wat is die nuutste weergawe van .NET en wanneer is dit vrygestel? Gee jou bronne.

*Tools:* WebSearch, DotNetSdkInfo, FetchUrl, GetToday, FetchUrl, WebSearch, FetchUrl · *Specialists:* — · *23.6 s*

Die nuutste **stabiele** (produksie-gereed) weergawe van .NET is **.NET 10**. Dit is 'n LTS-weergawe (Long Term Support), wat beteken dit kry drie jaar se ondersteuning.

**Vrystelling:** .NET 10 is op **11 November 2025** vrygestel. Die amptelike aankondiging is op die .NET-blog gepubliseer, en dit is ook die dag waarop Visual Studio 2026 en die C# Dev Kit vir VS Code beskikbaar gekom het.

**Ondersteuning:** Tot **10 November 2028** (LTS).

**Wat is daarna?**
- **.NET 11** is nog in 'n release candidate-fase. Die download-bladsy wys vandag **11.0.0-rc.2** (13 Oktober 2026). Dit is nie die finale weergawe nie, so moenie dit in produksie gebruik nie.
- **.NET 10.0.13** is die nuutste patch-opdatering op 10.0. Die download-bladsy wys dit as 13 Oktober 2026.

**Jou masjien:** Die SDK wat hier geïnstalleer is, is **10.0.401** met runtime **10.0.12**. Dit is dus een patch-weergawe agter die nuutste 10.0.13. As jy by die nuutste patch wil wees, is dit 'n gewone opdatering.

**Bronne:**
- Aankondiging van .NET 10 (11 November 2025): https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/
- Allerlaaste weergawes en datums: https://dotnet.microsoft.com/en-us/download/dotnet
- .NET 10-aflaaibladsy: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## files: Lees README.md in die werkruimte en som in drie punte op wat die projek doen.

*Tools:* ReadWorkspaceFile · *Specialists:* — · *5.2 s*

Dis 'n self-hosted KI-kodeagent vir .NET, gebou op Microsoft.Extensions.AI. Hy werk op jou repository met regte gereedskap (lêers, git, `dotnet`, Python, PowerShell, bash, websoektog, Azure DevOps en Paymo) en praat met jou via Rider, 'n webapp, 'n HTTP API of die terminal. Standaard gebruik hy Claude, maar Ollama en OpenAI-kompatibele eindpunte werk ook.

Dit het ook 'n orkestrator-demo (proof of concept). Die een agent kyk na foute in 'n mikrodiens, en louis-agent maak die regstelbare foute reg, met een tak en commit per fout, sonder om iets te push. Die orkestrator verifieer dan elke fix self. Die demo loop in Docker met tien bugs.

Die agent het ook 'n stel hosts: 'n Rider ACP-bediener, 'n Blazor-webapp met lêerblaaier, git-veranderinge en takke, 'n JSON-API met SSE-stroming, 'n CLI en 'n MCP-bediener. Almal deel die `louis-agent.core`-engine. Die lopie begin met Docker: stel `config/.env` en `config/.env.secrets` op, en start dan die API met `docker compose`. Daarna is die webapp op http://127.0.0.1:5080.

## git: Wat het die laaste drie commits in hierdie repository verander?

*Tools:* ShowCommit, ShowCommit, ShowCommit · *Specialists:* — · *15.0 s*

Die laaste drie commits, van nuutste na oudste:

**1. `8cdfa67`: PR #28 review** (Louis Marais, 13 Okt 2026, 23:46)
Hierdie commit maak 'n klomp reviewwerk klaar: 11 lêers, 81 invoegings en 19 skrappings.
- **PDF-lees (`WebTools.cs`):** Enige fout met 'n PDF word nou as `DocumentUnreadable` behandel, want PdfPig gooi meer as een soort uitsondering. Daar is 'n toets met 'n beskadigde PDF bygevoeg.
- **Ligging:** Die blaser se posisie gaan na Nominatim met twee desimale (ongeveer 1 km, dus 'n dorp), nie vier nie.
- **Tydsone:** Elke nuwe chat kry nou die tydsone. `GetToday` werk in die gebruiker se tyd sonder 'n ligging; die ligging word net gestuur as dit ingestel is.
- **Docs:** "Done when" is by F20, F21 en F22 ingevul, `GOAL.md` is opgedateer, en die F22-plan skryf die tydsone-besluit neer.
- **Kode:** Ook veranderinge in `Program.cs`, `ChatPage.razor`, `chat.js` en `AgentSessionsTests.cs`.

**2. `d28c274`: Samevoeging van PR #27 review-regstellings** (merge, 23:44)
Dit is 'n merge van die F5-tak. Daar was konflikte in `docs/ARCHITECTURE.md` en `AgentEngineFactory.cs`, wat in die merge opgelos is.
- **Agent-engine (`AgentEngineFactory.cs`):** Die meeste van die 30 nuwe reëls is hier.
- **Agent-peer tools (`AgentPeerTools.cs`):** 35 reëls verander, met toetse in `AgentPeerToolsTests.cs` en `AgentEngineFactoryTests.cs`.
- **Messages:** `ErrorMessage.cs` het 2 reëls verloor. `LogMessage.cs` en `ToolMessage.cs` het nuwe reëls gekry, en daar is 'n snapshot-lêer `ToolMessageSnapshot.json` bygevoeg.
- **Docs:** `ARCHITECTURE.md` en `GOAL.md` is bygewerk.
- In totaal: 10 lêers, 112 invoegings en 24 skrappings.

**3. `3a92bf7`: F20 stap 9, docs en die leerstuk** (23:28)
Hierdie commit is net dokumentasie, geen kode nie: 7 lêers, 148 invoegings en 3 skrappings.
- Nuwe projekdokument `docs/projects/louis-agent.puls.md` (75 reëls).
- 'n Eerste konsep van die leerstuk oor multi-agent-orkestrasie (`docs/learning/10-multi-agent-orchestration.md`, 58 reëls). Die prompt het volgens die commit van ongeveer 28,6k na 2,2k tokens afgeneem.
- F20 se bou-notas en status is opgedateer, plus `TODO.md`, en die README's in `docs/features`, `docs/learning` en `docs/projects`.

Wil jy een van hierdie commits in meer detail sien, of het jy 'n spesifieke lêer in gedagte?

## numbers: Hoeveel .cs-lêers is daar onder louis-agent.core, en hoeveel reëls het hulle saam?

*Tools:* FindFiles, BashRun · *Specialists:* — · *6.9 s*

Daar is **109 `.cs`-lêers** onder `louis-agent.core`, wat saam **10 971 reëls** is.

Die telling sluit die `bin`- en `obj`-gidse uit, want dit is build-uitvoer en nie jou eie kode nie. As jy dit insluit, kry jy 112 lêers en 11 009 reëls. Die drie ekstra lêers is outomaties gegenereerde `obj/Debug/net10.0/`-lêers.

## chat: Gee my drie name vir 'n Jack Russell, met 'n Afrikaanse knipoog.

*Tools:* — · *Specialists:* — · *7.8 s*

Hier is drie wat by 'n Jack Russell pas:

1. **Bokkie**: Dis 'n liefkosingsnaam en 'n klein bokkie, presies wat so 'n klein hond is. Hy gaan jou hart steel.
2. **Kwaai**: Jack Russells is klein, maar hulle dink hulle is die baas van die hele erf. "Kwaai" pas perfek by daardie terrier-houding.
3. **Wors**: As hy enigiets kan vang, gaan hy dit eet. Gee hom die volle naam Boerewors as hy stout is.

