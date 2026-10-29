# Build an identity

> **How-to.** Make a new agent: a project in `identities/` with its voice, its routing, its own specialists and the
> packs it uses, loaded by a host with `IDENTITIES`. **Elsewhere:** why identities exist in
> [ADR 0027](../adr/0027-engine-identities-and-packs.md), modes in [ADR 0019](../adr/0019-modes-one-file-per-product.md),
> the role levels in [ADR 0017](../adr/0017-skills-by-level.md); giving an agent tools in [Build a pack](build-a-pack.md).

An **identity** is who an agent is: its name, how it talks (its voice), which work goes where (its routing), the
specialists it can hand work to, and the packs it uses, by name. It has no tools of its own: everything it can do comes
from core's toolsets and from packs. Like a pack, it never references a pack's code, and core never references it.

The steps below build a small example identity, `acme`: a hardware shop's assistant that talks plain English, answers
questions itself, and hands "how much do I need?" jobs to one specialist, `acme-estimator`, which uses the `units` pack
from [Build a pack](build-a-pack.md). The real identity in the repo, `identities/louis-agent.identity.puls`, follows the
same steps and adds two languages, five specialists and a minion. Read it beside this guide.

## 1. Create the project

```bash
dotnet new classlib -o identities/louis-agent.identity.acme
dotnet sln louis-agent-solution.sln add identities/louis-agent.identity.acme/louis-agent.identity.acme.csproj
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
    <ProjectReference Include="..\..\louis-agent.core\louis-agent.core.csproj" />
    <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="10.0.13" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="Prompts\**\*.md" />
  </ItemGroup>
  <ItemGroup>
    <None Include="..\..\docs\projects\louis-agent.identity.acme.md" Link="docs\louis-agent.identity.acme.md" />
  </ItemGroup>
</Project>
```

The only project reference is core, even though `acme` uses the `units` pack: an identity names its packs, and the
composition root brings them. Reference a pack's project only when the identity holds workflow code that calls the
pack's public types (ADR 0027); a pack may never reference an identity.

The last group shows the identity's design doc under `docs/` in the IDE's solution explorer, as every project does;
nothing is built or copied. Write the doc (`docs/projects/louis-agent.identity.acme.md`) once the identity works.

## 2. Write the identity class

```csharp
namespace LouisAgent.Identities.Acme;

using LouisAgent.Core.Agent.Catalog;
using Microsoft.Extensions.FileProviders;

/// <summary>Acme, a hardware shop's assistant: its mode (agent/acme/), its estimator (specialists/acme-*), and the units pack.</summary>
public sealed class AcmeIdentity : IAgentIdentity
{
    public string Name => "acme";

    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(AcmeIdentity).Assembly, "Prompts");

    public IReadOnlyList<string> Packs => ["units"];
}
```

`Name` is what `IDENTITIES` loads it by and the folder of its modes (`agent/acme/`). Keep it one lowercase word, for
the same reason a pack's name is one word (step 5). `Packs` holds names, not types: whatever the catalogue holds under
that name serves it, our compiled pack today or an MCP server one day. An identity that needs no pack returns `[]`.

## 3. Write its mode

A mode is what the agent the user talks to (level 0) reads: one markdown file under `Prompts/agent/acme/`. Its first
`# ` section is the **voice**: who the agent is and how it talks. Everything from the second `# ` heading on is the
**routing**: which work goes to which toolset or specialist.

`Prompts/agent/acme/voice.md`:

```markdown
# Acme - Voice

You are **Acme**, the assistant of a hardware shop. People ask you about tools, materials and jobs around the house.

- **Plain and practical.** Short answers, metric measures, one tip when it saves the user a trip.
- **Honest.** If you're not sure, say so; never guess an amount of material.

# Acme - Routing

| The request is about | Toolset | Specialist role |
|---|---|---|
| Converting a measurement | `units` | you, with `units` |
| How much of a material a job needs (paint, tiles, timber) | `units` | `acme-estimator` |
| Handing a task to a specialist agent | `specialists` | — |

Anything else (orders, stock, prices) isn't available here yet: say so in one line, and answer what you can.
```

What to know when you write one:

- **Don't repeat the principles.** Core's `shared/principles.md` opens every prompt at every level before your mode
  ([ADR 0020](../adr/0020-principles-for-every-agent.md)).
- **Route only what this identity does.** Each pack's guide opens with its own routing line and explains its tools, so
  the mode says when to use a toolset or a specialist, not how the tools work.
- **Say what is missing.** A request the agent can't serve yet gets one honest line, never an invented answer.
- **More than one language?** Split the file: `voice-en.md` and `voice-af.md` hold only the voice, each in its language,
  and `voice.md` holds only the shared routing. A session then names `acme/voice-en`, which loads its voice and then
  `voice.md`; `acme/voice` on its own is no longer a mode. Puls is laid out this way.

## 4. Add a specialist (when the work needs one)

A specialist (level 1) is a sub-session the mode hands a piece of work to, with only the toolsets that work needs.
Its role file is `Prompts/specialists/acme-estimator.md`: what it does, and how it reports back to the agent that
called it.

```markdown
---
toolsets: units
---
# Acme specialist: Estimator

You are one of **Acme's specialists**: the estimator (toolset: `units`).

## Your function

You turn a job ("paint a 4 by 3 metre room") into the amount of material to buy, with the sums shown.

## How you report

One line with the amount and the unit to buy, then the sums, then anything you assumed under *Unsure*.
```

- **The header** (between the `---` lines) names the toolsets this role always loads, added to what the parent passed;
  the loader strips it before the model sees the file ([ADR 0025](../adr/0025-a-role-declares-its-toolsets.md)).
  Write `units?` for a toolset that needs settings an instance may lack: it then loads only where it can, as the
  Puls cook's `shops?` does.
- **Prefix every role with the identity's name** (`acme-estimator`). The catalogue refuses a prompt path two
  identities or packs share, so a plain `specialists/estimator.md` would clash with the next identity's.
- **The mode needs a way to hand work over:** the `specialists` toolset in its session, and `AGENT_PEERS` naming an
  instance that has the role. Usually that is the instance itself, as Puls's compose does (`AGENT_PEERS: puls=http://puls:8080`).

A **minion** (level 2) is smaller still: one task with one tool, which a specialist hands out. It needs a base file
`Prompts/minions/acme-minion.md` (what every Acme minion is) and one file per function,
`Prompts/minions/acme-minion-{function}.md`, plus the `MINION_LLMS` setting. Start without one. Puls's price lookup
(`minions/puls-minion-web-price-lookup.md`) is the worked example when you need one.

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

Three edits in `src/louis-agent.composition`, as for a pack:

1. A member in `IdentityKind.cs`: `Acme`.
2. Its arm in `Plugins.Create(IdentityKind)`: `IdentityKind.Acme => new AcmeIdentity(),`.
3. A project reference to the identity in `louis-agent.composition.csproj`.

Every pack it names must be registered too (`PackKind.Units`, from [Build a pack](build-a-pack.md)). If one isn't, the
catalogue stops start-up with the identity's name, the missing pack's and the ones it has. The setting is matched to
the enum member with the case ignored, so a dashed name (`acme-shop`) wouldn't match `AcmeShop`: keep it one word.

## 6. Test it

```bash
dotnet new nunit -o tests/louis-agent.identity.acme.tests
dotnet sln louis-agent-solution.sln add tests/louis-agent.identity.acme.tests/louis-agent.identity.acme.tests.csproj
```

The test project references the identity, every pack it names (so its sessions build as the host builds them), and
`tests/louis-agent.testing` for the shared fakes; `tests/louis-agent.identity.puls.tests` is the model. Write:

- **A prompts fixture** that registers the identity's prompts and its packs' before any test reads one, as a host does
  at start-up (`PulsPrompts.cs`):
  `[SetUpFixture] … SkillFileSource.Use([new AcmeIdentity().Prompts, new UnitsPack().Prompts]);`
- **An identity test** (`PulsIdentityTests`): its name and packs, one prompt per folder found through `Prompts` (that
  proves the csproj's embedding lines work), and the catalogue refusing it without its pack and taking it with.
- **Prompt tests** (`PulsPromptsTests`): `RoleCatalog.Available(RoleLevel.Agent)` lists `acme/voice`,
  `SkillLoader.Load(new AgentOptions(), [], "acme/voice")` holds the voice, and
  `RoleCatalog.ToolsetsOf("acme-estimator", RoleLevel.Specialist)` is `["units"]`.
- **A session golden** (`PulsSessionSnapshotTests` and its JSON): it builds the sessions the identity runs (the chat
  and each specialist) with a fake model and pins exactly what the model would be sent. A changed word in a prompt is
  then a diff you review: regenerate with `LOUIS_UPDATE_GOLDEN=1`, read the diff, commit it.

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
| `ROLES_ENABLED` | `acme/voice,acme-estimator` | Only roles that have passed their check are on; empty turns every role on |
| `AGENT_PEERS` | `acme=http://acme:8080` | Where the mode hands work to its specialists (itself) |
| `HOST_TIME_ZONE` | the shop's zone | The time the agent tells when the user shares none; core names no country |
| `BROWSER_USER_AGENT` | `Acme/1.0 (+link; reads a page on a user's behalf)` | Only if it reads the web: says who reads and why, never a person ([ADR 0038](../adr/0038-the-browser-says-what-it-is.md)) |

Then check it is there, and open a chat in its mode:

```bash
curl -s -H "x-api-key: $AGENT_API_KEY" http://127.0.0.1:5080/v1/roles
curl -s -H "x-api-key: $AGENT_API_KEY" -H "Content-Type: application/json" \
     -d '{"role": "acme/voice", "toolsets": ["specialists", "units"]}' http://127.0.0.1:5080/v1/sessions
```

`GET /v1/roles` lists `acme/voice` under `agent` and `acme-estimator` under `specialists`. A session must name
its mode: one that names none gets core's default, the coding agent's (`louis-agent/voice`). A web app of its own (as
Puls has in `src/louis-agent.puls`) is a separate project and optional; any client that opens sessions this way works.

## Modes, roles and minions: the rules

A session names its role when it opens (`POST /v1/sessions` with `"role": "{name}"`; a sub-session through
`StartSubSession(…, role)`), and keeps it, as it keeps its toolsets (F21). In the code a role is
`Skills/Roles/RoleCatalog` and its level `RoleLevel` (CL13); CL14 made "role" the public word. Without one a session
gets the default mode, `louis-agent/voice`. Prompts are embedded and read only from the assembly, so a wording change
is a rebuild of the images that use the identity.

1. **A mode** (level 0) is one file in a product's folder: its voice (the first `# ` section), then its routing (from
   the second `# ` heading on), named `{product}/{mode}` (`louis-agent/voice`). A **language** is
   `{product}/voice-{xx}.md` with only the voice; it loads the product's shared `voice.md` (the routing) after it, and
   Puls's *Taal* menu offers it as soon as `GET /v1/roles` does. Each product routes only what it does
   ([ADR 0019](../adr/0019-modes-one-file-per-product.md)).
2. **A role** (level 1) is `specialists/{name}.md`: a specialist's function and how it reports to the agent that called
   it. A parent names it in `StartSubSession`; the specialists guide (`toolsets/specialists.md`) says which goes with
   which toolsets. A role that always needs a toolset declares it in a header at the top of its file, which the
   loader strips before the model sees it (F33, [ADR 0025](../adr/0025-a-role-declares-its-toolsets.md)):

   ```markdown
   ---
   toolsets: shops?
   ---
   ```

   Those toolsets are added to what the parent passed; `?` loads one only where this instance has it (`shops` needs
   `SHOP_CATALOGUES` and `BROWSER_URL`), and skips it with a log line elsewhere.

   **A minion's function** (level 2, F30) is `minions/{product}-minion-{function}.md`, loaded after its product's base
   (`minions/{product}-minion.md`: what every minion is, which is no role itself); the specialist hands the minion one
   tool of its own, so no file lists tools per role.
3. **Names** are lowercase letters, digits and dashes (a mode adds one `/`), used at one level only; a name from
   another level is refused as unknown ([ADR 0017](../adr/0017-skills-by-level.md)). `ROLES_ENABLED` turns on only
   the listed ones, so a test can run Puls and the one role under test.
4. `GET /v1/roles` lists them by level, with the default; an unknown or invalid name is a 400 before any engine is
   built.

## Before you ask for a review

- [ ] References core only (a pack only for workflow code that uses its types); its packs are named in `Packs`.
- [ ] A one-word lowercase name, the same as its modes' folder.
- [ ] The mode opens with its voice; the routing starts at the second `# ` heading and routes only this identity's work.
- [ ] Every role is prefixed with the identity's name; a role's header names the toolsets it always needs.
- [ ] `IdentityKind`, `Plugins` and the csproj reference in the composition root; every named pack registered.
- [ ] A prompts fixture, an identity test, prompt tests and a session golden, reviewed.
- [ ] `ROLES_ENABLED` lists only the roles that have passed their check.

---
[Guides](README.md) · [Docs index](../README.md) · Previous: [Build a pack](build-a-pack.md)
