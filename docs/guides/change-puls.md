# Change Puls

> **How-to.** Change `identities/louis-agent.identity.puls`, who Puls is: its voice and routing, a new language, a new
> specialist or minion, the packs it names, and how to test and try a change. **Elsewhere:** how the identity works in
> [louis-agent.identity.puls](../projects/louis-agent.identity.puls.md); a new identity from scratch in
> [Build an identity](build-an-identity.md); the web app in [Change the Puls web app](change-the-puls-app.md).

The Puls identity is one class (`PulsIdentity`: its name, its prompts and the packs it names) and a folder of markdown
prompts. Nearly every change to how Puls behaves is a prompt change, not code. The prompts are embedded in the
assembly, so a change is a rebuild of the Puls image, and a running chat keeps the prompt it opened with: start a new
chat to see the change.

| Prompt | Level | What it holds |
|---|---|---|
| `Prompts/agent/puls/voice-af.md`, `voice-en.md` | 0, a mode per language | How Puls talks: its voice only |
| `Prompts/agent/puls/voice.md` | 0, shared | The routing: which requests go to which toolset and specialist, loaded after the voice |
| `Prompts/specialists/puls-{name}.md` | 1, a role | A specialist's function and how it reports back; a header may add toolsets |
| `Prompts/minions/puls-minion.md`, `puls-minion-{function}.md` | 2 | What every minion is, then one minion's function |

`Prompts/shared/principles.md` in core opens every one of them
([ADR 0020](../adr/0020-principles-for-every-agent.md)).

## Change what Puls says or does

- **How it talks** is the voice file of each language. Change the same section in every language: the languages must
  keep the same sections (`PulsPromptsTests` checks).
- **Where a request goes** is `voice.md`'s routing table. Puls routes only what Puls does
  ([ADR 0019](../adr/0019-modes-one-file-per-product.md)): anything else gets "not available yet" in one line.
- **What a specialist does** is its file in `specialists/`.

## Add a language

1. Copy `voice-af.md` to `voice-{xx}.md` and translate it, keeping every section and the heading's form
   (`# Puls - Voice` in English). The routing stays in `voice.md`, shared by every language.
2. Add `puls/voice-{xx}` to `ROLES_ENABLED` where it is set: `docker/docker-compose.puls.yml`, the API's `puls`
   launch profile, and `config/.env` if you set it there.
3. `GET /roles` then lists it, and the web app's language menu offers it with no change to the app.

## Add a specialist

1. Write `specialists/puls-{name}.md`: its function, what it may and may not do, and how it reports to Puls. If it
   always needs a toolset, declare it in a header the loader strips before the model sees it; a `?` loads it only
   where the instance has it ([ADR 0025](../adr/0025-a-role-declares-its-toolsets.md)):

   ```markdown
   ---
   toolsets: shops?
   ---
   ```

2. Add its row to `voice.md`'s routing table. The routing also says the cook is the only specialist that has passed
   its check, so Puls hands nothing else on: change that line too, once the new one has passed.
3. Measure it before users see it: an eval in `evals/louis-agent.evals.puls` (F18), compared with Puls answering alone.
   Only then add it to `ROLES_ENABLED`: where that setting is set (the Puls stack sets it), a role not listed is
   refused, and start-up refuses a listed name the instance doesn't have.
4. If it needs a capability Puls doesn't have, that is a pack ([Build a pack](build-a-pack.md)), named in
   `PulsIdentity.Packs`, never referenced as a project.

## Add a minion's function

A minion (F30) does one small task for a specialist, with one tool the specialist hands it. Its prompt is
`minions/puls-minion-{function}.md`, loaded after the shared base `minions/puls-minion.md`. Add the role to
`ROLES_ENABLED` as you would a specialist.

## Test it

```bash
dotnet test tests/louis-agent.identity.puls.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.identity.puls.tests    # after a deliberate prompt change: then read the diff
```

- `PulsIdentityTests`: its name and packs, one prompt found per folder, the catalogue refusing Puls without its pack.
- `PulsPromptsTests`: the modes and roles listed, a language loading its voice then the routing, the languages'
  sections, the cook's header, a minion's prompt order.
- `PulsSessionSnapshotTests` pins exactly what the model is sent in Puls's chat and in its cook
  (`PulsSessionSnapshot.json`). Any word you change in a prompt changes it on purpose, so regenerate it and read the
  diff: it is the review of the prompt change.

Then try it in the Puls app ([Change the Puls web app](change-the-puls-app.md#run-it)) with a new chat. A change that
should make Puls better is measured by its evals before it is merged; running them against the real model costs money,
so ask first.

## Before you ask for a review

- [ ] Every language changed the same way; the session golden's diff is the change you meant.
- [ ] A new role is in the routing, has passed its eval, and is in `ROLES_ENABLED` everywhere it is set.
- [ ] A new capability is a pack named in `PulsIdentity.Packs`, not code in the identity.

---
[Guides](README.md) · [Docs index](../README.md)
