# Build an identity

> **How-to.** Make a new agent: a project in `demos/{name}/identity/` with its voice, its routing, its own specialists and the
> packs it uses, loaded by a host with `IDENTITIES`. **Elsewhere:** why identities exist in
> [ADR 0027](../adr/0027-engine-identities-and-packs.md), modes in [ADR 0019](../adr/0019-modes-one-file-per-product.md),
> the role levels in [ADR 0017](../adr/0017-skills-by-level.md); giving an agent tools in [Build a pack](build-a-pack.md).

![An identity at a glance: who an agent is, with its mode (voice and routing), its specialists and minions, the packs it names and the agents it may call, registered in the composition root and loaded by IDENTITIES](diagrams/build-an-identity.svg)

An **identity** is who an agent is: its name, how it talks (its voice), which work goes where (its routing), the
specialists it can hand work to, and the packs it uses, by name. It has no tools of its own: everything it can do comes
from core's toolsets and from packs. Like a pack, it never references a pack's code, and core never references it.

The steps below build a small example identity, `acme`: a hardware shop's assistant that talks plain English, answers
questions itself, and hands "how much do I need?" jobs to one specialist, `acme-estimator`, which uses the `units` pack
from [Build a pack](build-a-pack.md). The real identity in the repo, `demos/puls/identity/louis-agent.identity.puls`, follows the
same steps and adds two languages, five specialists and a minion. Read it beside this guide.

![Building an identity: create the project, write the class, write its mode, add a specialist when the work needs one, register it in the composition root, test it, then run it](diagrams/build-an-identity-flow.svg)

## 1. Create the project

```bash
dotnet new classlib -o demos/acme/identity/louis-agent.identity.acme
dotnet sln louis-agent-solution.sln add demos/acme/identity/louis-agent.identity.acme/louis-agent.identity.acme.csproj
```

Delete `Class1.cs`, and give the csproj the same embedding lines a pack has:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>LouisAgent.Identities.Acme</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Keeps the prompts' folder paths: without it, agent/acme/voice.md is never found. -->
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\src\engine\louis-agent.composition\louis-agent.composition.csproj" />
    <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="10.0.13" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="prompts\**\*.md" />
  </ItemGroup>
  <ItemGroup>
    <None Include="..\..\docs\projects\louis-agent.identity.acme.md" Link="docs\louis-agent.identity.acme.md" />
  </ItemGroup>
</Project>
```

The only project reference is `louis-agent.composition`, the contracts everything agrees on, even though `acme` uses
the `units` pack. An identity names its packs, and the composition root brings them. Reference a pack's project only
when the identity holds workflow code that calls the pack's public types
([ADR 0027](../adr/0027-engine-identities-and-packs.md)). A pack may never reference an identity.

The last group shows the identity's design doc under `docs/` in the IDE's solution explorer, as every project does.
Nothing is built or copied. Write the doc (`demos/acme/docs/projects/louis-agent.identity.acme.md`) once the identity
works.

## 2. Write the identity class

```csharp
using LouisAgent.Composition.Packs;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Identities.Acme;

/// <summary>Acme, a hardware shop's assistant: its mode (agent/acme/), its estimator (specialists/acme-*), and the units pack.</summary>
public sealed class AcmeIdentity : IAgentIdentity
{
    public string Name => "acme";

    public string DefaultVoice => "acme/voice";

    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(AcmeIdentity).Assembly);

    public IReadOnlyList<string> Packs => ["units"];

    public IReadOnlyList<string> Calls => ["acme.estimator"];
}
```

`Name` is what `IDENTITIES` loads it by, and the folder of its modes (`agent/acme/`). Keep it lowercase. A name with a
dash needs a `[WireName]`, as a pack's does (step 5).

`DefaultVoice` is the mode a session runs when it names none. `Prompts` reads the prompts the project embeds under
`prompts/`. `Packs` holds names, not types: whatever the catalogue holds under that name serves it, our compiled pack
today or an MCP server one day. An identity that needs no pack returns `[]`. `Calls` lists the agents it may ask, by
their addresses (step 4). An identity calls only whom it names, so one with no specialist leaves it out.

These hard-coded names are only to keep the example short: in a real identity they are enums, as *Your names are an
enum* below shows.

## 3. Write its mode

A mode is what the agent the user talks to (level 0) reads: one markdown file under `prompts/agent/acme/`. Its first
`# ` section is the **voice**: who the agent is and how it talks. Everything from the second `# ` heading on is the
**routing**: which work goes to which toolset or specialist.

`prompts/agent/acme/voice.md`:

```markdown
---
name: voice
description: Acme's voice and routing, the mode a session opens with. Who Acme is, and which toolset or specialist a request needs.
metadata:
  level: agent
---
# Acme - Voice

You are **Acme**, the assistant of a hardware shop. People ask you about tools, materials and jobs around the house.

- **Plain and practical.** Short answers, metric measures, one tip when it saves the user a trip.
- **Honest.** If you're not sure, say so; never guess an amount of material.

# Acme - Routing

| The request is about | Toolset | Specialist role |
|---|---|---|
| Converting a measurement | `units` | you, with `units` |
| How much of a material a job needs (paint, tiles, timber) | `units` | `acme-estimator` |
| Handing a task to a specialist agent | `agents` | — |

Anything else (orders, stock, prices) isn't available here yet: say so in one line, and answer what you can.
```

A few things are worth knowing when you write one.

Open with the header. Every prompt starts with the same front matter, between two `---` lines. `name` is the file's
name without `.md`. `description` says in a sentence or two what the file is and when it's loaded; it can be at most
1024 characters, and mustn't hold a `: `, which YAML would read as a new key. Under `metadata` goes the `level`
(`agent` for a mode, `specialist` or `minion`) and, for a role, the `toolsets` it always loads. The loader reads the
header once and strips it, so the model never sees it, and the admin portal's *Prompts* page lists it. A key the
loader doesn't know, a name that isn't the file's, or a wrong level stops the host at its start, naming the file.
`EveryPromptHasAHeaderTests` holds every prompt to this.

Don't repeat the principles. Core's `shared/principles.md` opens every prompt at every level, before your mode.

Route only what this identity does. Each pack's guide opens with its own routing line and explains its own tools, so
your mode only says when to use a toolset or a specialist, not how the tools work. And when a request is something the
agent can't do yet, it says so in one honest line. It never invents an answer.

If the agent speaks more than one language, split the file. `voice-en.md` and `voice-af.md` hold only the voice, each
in its own language, and `voice.md` holds only the routing they share. A session then names `acme/voice-en`, which
loads that voice and then `voice.md`, and `acme/voice` on its own is no longer a mode. Puls is laid out this way.

If the file grows too long, split off skills. Whatever the agent doesn't need on every turn goes in a skill beside the
voice, `prompts/agent/acme/skills/{name}.md`, with the same header plus `metadata.load`. A skill marked `always` goes
in the prompt right after the voice. One marked `on-demand` is only listed, by its name (`acme/{name}`) and its
description, and the agent reads it with `ReadSkill`, a tool a session gets only when its identity has such a skill.
The description is all the model sees until it reads the skill, so make it say when to read it. If the model has to
know a thing exists before it would think of reading about it (a page it could open, say), keep a short index of
those always, and the detail on demand. The guide is laid out this way: its voice, an index of the site's pages it
always has, and the docs map and what each page says on demand.

*Decided in:* [ADR 0020](../adr/0020-principles-for-every-agent.md), [ADR 0063](../adr/0063-a-prompt-says-what-it-is.md),
[ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md).

## 4. Add a specialist (when the work needs one)

A specialist (level 1) is a sub-session the mode hands a piece of work to, with only the toolsets that work needs.
Its role file is `prompts/specialists/acme-estimator.md`: what it does, and how it reports back to the agent that
called it.

```markdown
---
name: acme-estimator
description: Acme's estimator. Turns a job into the amount of material to buy, with the sums shown.
metadata:
  level: specialist
  toolsets: units
---
# Acme specialist: Estimator

You are one of **Acme's specialists**: the estimator (toolset: `units`).

## Your function

You turn a job ("paint a 4 by 3 metre room") into the amount of material to buy, with the sums shown.

## How you report

One line with the amount and the unit to buy, then the sums, then anything you assumed under *Unsure*.
```

The header's `metadata.toolsets` names the toolsets this role always loads, on top of what the parent passed, and
the loader strips the header before the model sees the file. Write `units?` for a toolset that needs settings an
instance might not have: it then loads only where it can. A role that needs none leaves the key out.

Prefix every role with the identity's name, as in `acme-estimator`. The catalogue refuses a prompt path that two
identities or packs share, so a plain `specialists/estimator.md` would clash with the next identity's.

The mode also needs a way to hand the work over. Its session needs the `agents` toolset, the identity's `Calls` must
list the role's address (`acme.estimator`, as in step 2), and `AGENT_PEERS` must name an instance that has the role. Usually that's
the instance itself, as Puls's compose file does (`AGENT_PEERS: puls=http://puls:8080`).

*Decided in:* [ADR 0025](../adr/0025-a-role-declares-its-toolsets.md), [ADR 0063](../adr/0063-a-prompt-says-what-it-is.md),
[ADR 0117](../adr/0117-one-address-for-every-agent-call.md).

A **minion** (level 2) is smaller still: one task with one tool, which a specialist hands out. It needs a base file
`prompts/minions/acme-minion.md` (what every Acme minion is) and one file per function,
`prompts/minions/acme-minion-{function}.md`, plus the `MINION_LLMS` setting. Start without one. Puls's recipe search
(`minions/puls-minion-find-recipe.md`, which a specialist would start with `FindRecipes` as its tool; the cook no longer does, ADR 0081) and its price lookup
(`minions/puls-minion-web-price-lookup.md`) are the worked examples when you need one; a role that lists `minions` in
its header is refused where `MINION_LLMS` isn't set.

### Your names are an enum

The roles the identity offers (`acme-estimator`, `acme-minion-lookup`) are written in its tests, in a request and in the stream, so they are an enum
([ADR 0043](../adr/0043-repeated-names-are-enums.md)), in the identity's project and no one else's:

```csharp
public enum AcmeRole
{
    [WireName("acme-estimator")] Estimator,        // the role's file name: specialists/acme-estimator.md
    [WireName("acme-minion-lookup")] MinionLookup, // minions/acme-minion-lookup.md
}
```

Write a role out with `.Wire()`, read one in with `SettingChoice.TryParse`, and have a test check that each member is a file of the identity
(`PulsRoleTests` is the model). The packs the identity names are its own enum too (`public enum AcmePack { [WireName("units")] Units }`, `Packs => [AcmePack.Units.Wire()]`), never the pack's
own enum, which the identity cannot reference; the composition root's `PackKind` is the host's copy, and its tests check that every `PackKind` is the name of the pack built for it
and (`CrossBoundaryNamesTests`) that each pack an identity names exists. A mode (`acme/voice`) is still its file's name. Tests that only store or show a role use a sample
(`SampleRole`); a test that opens a session on a real role uses your enum member. Add your enum's file to the list in `RepeatedNameTests`.

## 5. Register it in the composition root

Three edits in `demos/puls/hosts/louis-agent.host-packs`, as for a pack:

1. A member in `IdentityKind.cs`: `Acme`.
2. Its arm in `Plugins.Create(IdentityKind)`: `IdentityKind.Acme => new AcmeIdentity(),`.
3. A project reference to the identity in `louis-agent.host-packs.csproj`.

Every pack it names must be registered too (`PackKind.Units`, from [Build a pack](build-a-pack.md)). If one isn't, the
catalogue stops start-up with the identity's name, the missing pack's and the ones it has. The setting is matched to
the enum member with the case ignored, so a name with a dash needs the member to say so: `[WireName("acme-shop")] AcmeShop`.

## 6. Test it

```bash
dotnet new nunit -o demos/acme/tests/louis-agent.identity.acme.tests
dotnet sln louis-agent-solution.sln add demos/acme/tests/louis-agent.identity.acme.tests/louis-agent.identity.acme.tests.csproj
```

The test project references the identity, every pack it names (so its sessions build as the host builds them), and
`src/tests/shared/louis-agent.testing` for the shared fakes; `demos/puls/tests/louis-agent.identity.puls.tests` is
the model to copy. You write four kinds of test.

The first is a prompts fixture. It registers the identity's prompts and its packs' before any test reads one, the way
a host does at start-up (`PulsPrompts.cs`):
`[SetUpFixture] … SkillFileSource.Use([new AcmeIdentity().Prompts, new UnitsPack().Prompts]);`

The second is an identity test (`PulsIdentityTests`). It checks the identity's name and packs, finds one prompt per
folder through `Prompts` (which proves the csproj's embedding lines work), and checks that the catalogue refuses the
identity without its pack and takes it with it.

The third is the prompt tests (`PulsPromptsTests`). They check that `RoleCatalog.Available(RoleLevel.Agent)` lists
`acme/voice`, that `SkillLoader.Load(new AgentOptions(), [], "acme/voice")` holds the voice, and that
`SkillLoader.ToolsetsOf("acme-estimator", RoleLevel.Specialist)` is `["units"]`.

The last is a session golden (`PulsSessionSnapshotTests` and its JSON). It builds the sessions the identity runs, the
chat and each specialist, with a fake model, and pins exactly what the model would be sent. A changed word in a prompt
then shows up as a diff you review: regenerate with `LOUIS_UPDATE_GOLDEN=1`, read the diff, and commit it.

Two checks come for free once it is in `IdentityKind`: `PackConvertibilityTests` checks that no pack's tool
description names your identity, and `ArchitectureTests` checks that no pack references it. Run everything:

```bash
dotnet build louis-agent-solution.sln
dotnet test louis-agent-solution.sln
```

## 7. Run it

You don't need a new host: the API serves whichever identities it is given. A stack for `acme` sets, in its compose
file or `config/.env`:

| Setting | For `acme` | Why |
|---|---|---|
| `IDENTITIES` | `acme` | Loads the identity and the packs it names |
| `ROLES_ENABLED` | `acme/voice,acme-estimator` | Only roles that have passed their check are on; empty turns every role on (Puls's list holds its two minion roles too) |
| `AGENT_PEERS` | `acme=http://acme:8080` | Where the mode hands work to its specialists (itself) |
| `HOST_TIME_ZONE` | the shop's zone | The time the agent tells when the user shares none; core names no country |
| `BROWSER_USER_AGENT` | `Acme/1.0 (+link; reads a page on a user's behalf)` | Only if it reads the web: says who reads and why, never a person ([ADR 0038](../adr/0038-the-browser-says-what-it-is.md)) |

Then check it is there, and open a chat in its mode. `$API` is your stack's address (Puls's is
`http://127.0.0.1:5070`):

```bash
curl -s -H "x-api-key: $AGENT_API_KEY" $API/v1/roles
curl -s -H "x-api-key: $AGENT_API_KEY" -H "Content-Type: application/json" \
     -d '{"role": "acme/voice", "toolsets": ["agents", "units"]}' $API/v1/sessions
```

`GET /v1/roles` lists `acme/voice` under `agent` and `acme-estimator` under `specialists`. A session that names no mode
runs the default, which is the first loaded identity's `DefaultVoice` (`acme/voice` here). A web app of its own (as
Puls has in `demos/puls/web/louis-agent.puls`) is a separate project and optional; any client that opens sessions this way works.

## Modes, roles and minions: the rules

A session names its role when it opens, with `"role": "{name}"` on `POST /v1/sessions`, and keeps it, just as it
keeps its toolsets. A sub-session gets its role from the address `AskAgent` was given: `acme.estimator` for
`acme-estimator`. In the code a role is `Skills/Roles/RoleCatalog`, and its level is `RoleLevel`. A session that names
none gets the first loaded identity's `DefaultVoice`. Prompts are embedded and read only from the assembly, so changing a
word means rebuilding the images that use the identity.

*Decided in:* F21, CL13, CL14.

A mode (level 0) is one file in a product's folder. It holds the voice (the first `# ` section) and then the routing
(from the second `# ` heading on), and it's named `{product}/{mode}`, as in `louis-agent/voice`. A language is
`{product}/voice-{xx}.md` with only the voice in it. It loads the product's shared `voice.md`, the routing, after
itself, and Puls's *Taal* menu offers it as soon as `GET /v1/roles` does. Each product routes only what it does.

*Decided in:* [ADR 0019](../adr/0019-modes-one-file-per-product.md).

A role (level 1) is `specialists/{name}.md`: a specialist's function, and how it reports to the agent that called it.
A parent asks it by its address (`AskAgent("acme.estimator", …)`), which the identity's `Calls` must list, and the
agents guide (`toolsets/agents.md`) says how. A role that always needs a toolset says so under `metadata` in the header
every prompt opens with, and the loader strips that header before the model sees it:

```markdown
---
name: acme-cook
description: Acme's cook. A meal for the people asked for, priced at the shops.
metadata:
  level: specialist
  toolsets: recipes?
---
```

Those toolsets are added to what the parent passed. The `?` loads one only where this instance has it (`recipes`
needs `RECIPE_SOURCES` and `BROWSER_URL`), and skips it elsewhere with a line in the log.

*Decided in:* F33, CL27, [ADR 0025](../adr/0025-a-role-declares-its-toolsets.md),
[ADR 0063](../adr/0063-a-prompt-says-what-it-is.md).

A minion's function (level 2) is `minions/{product}-minion-{function}.md`. It's loaded after its product's base,
`minions/{product}-minion.md`, which says what every minion is and isn't a role itself. The specialist hands the minion
one tool of its own, so no file lists tools per role.

*Decided in:* F30.

Names are lowercase letters, digits and dashes (a mode adds one `/`), and each is used at one level only: a name from
another level is refused as unknown. `ROLES_ENABLED` turns on only the roles it lists, so a test can run Puls with just
the one role it's testing. `GET /v1/roles` lists them all by level, with the default, and an unknown or invalid name
is a 400 before any engine is built.

*Decided in:* [ADR 0017](../adr/0017-skills-by-level.md).

## Before you ask for a review

- [ ] References `louis-agent.composition` only (a pack only for workflow code that uses its types); its packs are named in `Packs`.
- [ ] A one-word lowercase name, the same as its modes' folder.
- [ ] The mode opens with its voice; the routing starts at the second `# ` heading and routes only this identity's work.
- [ ] Every role is prefixed with the identity's name; a role's header names the toolsets it always needs.
- [ ] `IdentityKind`, `Plugins` and the `louis-agent.host-packs.csproj` reference in the composition root; every named pack registered.
- [ ] A prompts fixture, an identity test, prompt tests and a session golden, reviewed.
- [ ] `ROLES_ENABLED` lists only the roles that have passed their check.

---
[Guides](README.md) · [Docs index](../README.md) · Previous: [Build a pack](build-a-pack.md)
