# louis-agent.identity.puls — design

> **Kind:** class library (`net10.0`) · **Path:** `identities/louis-agent.identity.puls/` · **References:**
> louis-agent.core, Microsoft.Extensions.FileProviders.Embedded · **Loaded by:** `IDENTITIES=puls` (the Puls compose
> sets it) · **Names:** the `shops` pack

## Purpose

Who Puls is: the South African general assistant's voice in Afrikaans and English, its routing, its specialists and its
minion, and the pack it uses. It holds no tools and no settings: what Puls can do comes from core's toolsets and from
the packs it names ([ADR 0027](../adr/0027-engine-identities-and-packs.md)). Puls's web app, what the user sees, is a
separate project ([louis-agent.puls](louis-agent.puls.md)).

## How it works

- **One class, the rest is content.** `PulsIdentity` answers three questions: its name (`puls`), its prompts (a
  `ManifestEmbeddedFileProvider` over `Prompts/`, so the files keep their folder paths) and its packs (`["shops"]`).
- **Start-up.** With `IDENTITIES=puls`, the composition root builds it and the `shops` pack it names; the catalogue
  checks that the pack is registered and that no prompt path is also core's or a pack's. From then on Puls's prompts
  are read through the same reader as core's (`SkillFileSource`).
- **A chat.** The web app opens a session with `role: "puls/voice-af"` (or `puls/voice-en`) and the toolsets
  `specialists` and `web`. The session reads core's principles, then the language's voice (`voice-af.md`), then Puls's
  shared routing (`voice.md`). Puls answers the world itself with `web`, and hands every cooking question to the cook
  with `StartSubSession`, on the same instance (`AGENT_PEERS: puls=http://puls:8080`).
- **The cook.** `specialists/puls-cook.md` opens with a header, `toolsets: shops?`, so a cook session also gets the
  `shops` toolset where the instance has its settings, and does without it elsewhere
  ([ADR 0025](../adr/0025-a-role-declares-its-toolsets.md)).
- **Only checked roles are on.** `ROLES_ENABLED` lists them; the Puls compose defaults to `puls/voice-af`,
  `puls/voice-en`, `puls-cook` and `puls-minion-web-price-lookup`, the ones that passed their check
  (F29). The other specialists are written but off.

## Design notes

What building Puls's specialists settled (F21, F22, F25, F26, F29, F30); the measured story is
[write-up 10](../learning/10-multi-agent-orchestration.md):

- **A specialist is a role, one per kind of work,** and a sub-session gets no router: only its role and its toolsets'
  guides, about 1,900 tokens of prefix against Puls's 2,200. A role Puls names is refused before an engine is built
  when it is unknown or off (`ROLES_ENABLED`), with the same answer either way, so a role that's off comes back to
  Puls as a tool error it must report. Turning a role on is one line in `config/.env` and `up -d`, once its eval passes.
- **The prompts and the gate say the same:** the routing keeps the cook, makes the web Puls's own work, and marks the
  rest *not yet*; the specialists guide lists those under *Waiting for validation*, with what each needs (a report
  shape, a relay rule, an eval).
- **Cooking is never answered from memory** (Louis, 2026-10-15, after Puls answered "ek wil bobotie maak" itself):
  every cooking question goes to the cook, which takes the dish through the whole report and adds *Where to buy*, the
  shops' branches in the user's town. Branches come from OpenStreetMap (Nominatim's search, as JSON), since the shops'
  own store finders draw their content with JavaScript.
- **The user's place travels with the session,** not through the model: Ligging in the web app (typed, or the
  browser's position sent to Nominatim at two decimals, about a town), the time zone with every new chat, and the
  specialists get it in their task. Attachments go by name: the model names the file, the session hands it over and
  remembers it across turns.
- **A follow-up goes to the same specialist** (F26): re-delegating cold would refetch every page, could drift to
  another dish and rewrite the cached prefix. The chat's session keeps its open sub-sessions (core's
  `SubSessionRegistry`, short handles per chat, `sub-1`); at most 3 stay open (the oldest closes); a continue that
  fails or times out closes the sub-session; starts and continues count against the same per-turn limit.
- **The local model reads English, not Afrikaans:** a role written for a small local model is in English, Puls briefs it
  in English and writes the Afrikaans answer itself.
- **Minions are built and not used** (F30): a specialist could hand one task and one of its own tools to a minion on
  the model `MINION_LLMS` names (the minion role `puls-minion-web-price-lookup`), and the web app draws a minion's card
  inside its specialist's. Four hand checks found a small local model unreliable at pricing (wrong products, invented
  dates), and Haiku minions no better than the cook's own searches, so the prompts went back to their text before
  minions; the code stays until the cook is measured with and without them (TODO).

## Structure

| Path | What's in it |
|---|---|
| `PulsIdentity.cs` | The identity: name, prompts, packs |
| `PulsRole.cs` | The roles Puls offers a session as an enum (the cook, researcher, analyst, historian, reader and the price-lookup minion), each a file of the identity; written with `.Wire()` (F18 step 7.5.4) |
| `PulsPack.cs` | The packs Puls names as an enum of its own (the shops pack), never the pack's enum; the composition root's `PackKind` is checked against the packs it builds (F18 step 7.5.4e) |
| `Prompts/agent/puls/voice-af.md`, `voice-en.md` | The voice in each language (*Puls - Stem*, *Puls - Voice*): who Puls is and how it talks; the two have the same sections |
| `Prompts/agent/puls/voice.md` | The shared routing (*Puls - Routing*): which work goes to which toolset or specialist, how to brief one, what isn't available yet; no mode of its own |
| `Evals/Routing/` | The routing eval as a folder of files (F49.2): `eval.json` (its modes, the baseline's toolsets listed, and `routed-to` with only `specialists` delegating), `manual.md`, `Dataset/` and `Transcripts/`; copied to `EvalFiles/identities/louis-agent.identity.puls/…`, where the runner finds it through the composition. Files only: the identity references no harness |
| `Prompts/specialists/puls-cook.md` | The local cook: a recipe from a real page, priced at the shops near the user, with sources (on) |
| `Prompts/specialists/puls-researcher.md`, `puls-reader.md`, `puls-analyst.md`, `puls-historian.md` | The web researcher, file reader, numbers and history specialists (written, off until each passes a check) |
| `Prompts/minions/puls-minion.md` | What every Puls minion is: one task, one tool, a short answer in its role's fields |
| `Prompts/minions/puls-minion-web-price-lookup.md` | One ingredient's price at each named shop, with `WebSearch` (F30); runs only where the instance sets `MINION_LLMS` |
| `louis-agent.identity.puls.csproj` | References core only; the two embedding lines (`GenerateEmbeddedFilesManifest`, the `Prompts\**\*.md` glob) |

## Configuration

None of its own. A Puls stack (`docker/docker-compose.puls.yml`) sets `IDENTITIES=puls`, `ROLES_ENABLED`,
`AGENT_PEERS` (itself), `HOST_TIME_ZONE=Africa/Johannesburg` and `BROWSER_USER_AGENT` (who reads and why,
[ADR 0038](../adr/0038-the-browser-says-what-it-is.md)), plus the shops pack's settings
([louis-agent.pack.shops](louis-agent.pack.shops.md)). Every setting is in [Setup](../SETUP.md).

## Extending it

The steps are in [Change Puls](../guides/change-puls.md); a new identity from scratch, with Puls as the worked
example, is [Build an identity](../guides/build-an-identity.md). In short: a
language is a `voice-{xx}.md` with the same sections as `voice-af.md`; a specialist is `specialists/puls-{name}.md`, a
row in the routing, and a place in `ROLES_ENABLED` once its eval passes. A changed word in any prompt changes the
session golden: regenerate it and read the diff.

## Tests

`tests/louis-agent.identity.puls.tests`: `PulsIdentityTests` (name, packs, one prompt per folder found, the catalogue
refusing Puls without its pack), `PulsPromptsTests` (the modes and roles listed, a language loading its voice then the
routing, the languages with the same sections, the cook's header) and `PulsSessionSnapshotTests` with
`PulsSessionSnapshot.json`, the golden of exactly what the model is sent in Puls's chat and in its cook. `PulsPrompts`
registers the identity's and the pack's prompts first, as a host does. Puls's behaviour is measured by its evals:
the routing eval beside the identity (`Evals/Routing/RoutingEvalTests`: its modes, with the baseline equal to Puls's
toolsets from the composition minus the delegating ones; its dataset's shape; its references and known-bads conform;
its manual), and the cook in [evals](louis-agent.evals.puls.md) until it moves (F49.2 step 8). The test project
references the harness and the composition for these; the identity doesn't.

## Limits and plans

- Only the cook has passed its check; each other specialist is turned on after its own eval.
- Recipes come from the web today → F35 adds the `recipes` pack; CL16 moves the cook into a `cooking` pack; the area
  guide moves here with the Places pack ([TODO](../../TODO.md), *Puls*).
- Louis's corrections to the Afrikaans, and the eleven official languages, measured → [TODO](../../TODO.md).

## Related docs

[Build an identity](../guides/build-an-identity.md) · [louis-agent.puls](louis-agent.puls.md) (the web app) ·
[louis-agent.pack.shops](louis-agent.pack.shops.md) · [ADR 0019 modes](../adr/0019-modes-one-file-per-product.md)

---
[Projects](README.md) · Previous: [louis-agent.core](louis-agent.core.md) · Next: [louis-agent.pack.shops](louis-agent.pack.shops.md)
