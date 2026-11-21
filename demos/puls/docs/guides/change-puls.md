# Change Puls

> **How-to.** Change `demos/puls/identity/LouisAgent.Identities.Puls`, who Puls is: its voice and routing, a new language, a new
> specialist or minion, the packs it names, and how to test and try a change. **Elsewhere:** how the identity works in
> [LouisAgent.Identities.Puls](../projects/LouisAgent.Identities.Puls.md); a new identity from scratch in
> [Build an identity](../../../../src/docs/guides/build-an-identity.md); the web app in [Change the Puls web app](change-the-puls-app.md).

![Change Puls: its prompts, a language, a specialist, a minion, its packs, and the tests that pin what the model reads](../diagrams/change-puls.svg)

Puls is one small class and a folder of markdown prompts. `PulsIdentity` holds its name, its prompts, the packs it
names and the addresses of the specialists and minions it may ask. Two enums sit beside it: `PulsRole` lists the
role files by name, and `PulsCall` lists the same roles by address (`puls.cook`). Nearly every change to how Puls
behaves is a prompt change, not code.

The prompts are embedded in the assembly, so a change means rebuilding the Puls image. A running chat keeps the prompt
it opened with, so start a new chat to see the change.

| Prompt | Level | What it holds |
|---|---|---|
| `prompts/agent/puls/voice-af.md`, `voice-en.md` | 0, a mode per language | How Puls talks: its voice only |
| `prompts/agent/puls/voice.md` | 0, shared | The routing: which requests go to which toolset and specialist, loaded after the voice |
| `prompts/specialists/puls-{name}.md` | 1, a role | A specialist's function and how it reports back; a header may add toolsets |
| `prompts/minions/puls-minion.md`, `puls-minion-{function}.md` | 2 | What every minion is, then one minion's function |

`prompts/shared/principles.md` in core opens every one of them
([ADR 0020](../../../../src/docs/adr/0020-principles-for-every-agent.md)).

## Change what Puls says or does

How Puls talks lives in the voice file of each language. When you change a section, change it in every language,
because the languages must keep the same sections, and `PulsPromptsTests` checks that they do.

Where a request goes is the routing in `voice.md`. Its first table, *Where the answer is*, says which toolset and
which specialist a kind of request needs. Puls routes only what Puls does
([ADR 0019](../../../../src/docs/adr/0019-modes-one-file-per-product.md)): for anything else it says, in one line, that
the part isn't available yet, and answers what it can.

What a specialist does is its own file in `specialists/`.

## Add a language

1. Copy `voice-af.md` to `voice-{xx}.md` and translate it. Keep every section, and keep the heading's form
   (`# Puls - Voice` in English). The routing stays in `voice.md`, which every language shares.
2. Add `puls/voice-{xx}` to `ROLES_ENABLED` everywhere it's set: `demos/puls/docker/docker-compose.puls.yml`, the
   `puls` launch profile of `LouisAgent.PulsApi`, and `config/.env` if you set it there.
3. Start the API. `GET /v1/roles` now lists the new voice. The web app's language menu offers every role that starts
   with `puls/voice-`, so it shows the new one with no change to the app.

## Add a specialist

![Add a specialist: write its prompt, name it in the identity, measure it, and only then route to it and turn it on](../diagrams/change-puls-flow.svg)

A specialist is measured before Puls hands it any work, so the order of these steps matters.

1. Write `specialists/puls-{name}.md`: its function, what it may and may not do, and how it reports to Puls. It opens
   with the header every prompt has ([ADR 0063](../../../../src/docs/adr/0063-a-prompt-says-what-it-is.md)): its name,
   a description and its level. The loader strips the header before the model sees it.

   ```markdown
   ---
   name: puls-{name}
   description: Puls's {name}. What it does, and when Puls hands it work.
   metadata:
     level: specialist
   ---
   ```

   If the role always needs a toolset, list it under `metadata` as `toolsets: web`. A toolset named bare is required,
   so a missing pack refuses the role; with a `?` (`web?`) it loads only where the instance has it
   ([ADR 0025](../../../../src/docs/adr/0025-a-role-declares-its-toolsets.md)). Today only the cook declares toolsets,
   and `PulsIdentityTests.TheCookDeclaresItsToolsets_AndNoOtherRoleDoes` pins that, so a new role with toolsets
   changes that test too.
2. Name it in the identity. Add a member to `PulsRole` with its file name (`puls-{name}`) and one to `PulsCall` with
   its address (`puls.{name}`). `PulsRoleTests` fails while a role file has no member, and
   `Puls_CallsItsFiveSpecialists_AndRelaysTheirAnswersWhole` lists the specialists Puls may ask, so add yours there.
3. Measure it before users see it. Give it an eval folder in
   `demos/puls/evals/LouisAgent.Identities.Puls.Evals/Evals/` ([Add an eval](../../../../src/docs/guides/add-an-eval.md)),
   with a mode where Puls answers alone to compare against. The cook's eval does this with its `puls-tools` mode, in
   which Puls has the shop tools itself, beside its `puls-cook` mode, in which it asks the cook.
4. Once it has passed, route to it. Add its rows to `voice.md`: one in *Where the answer is*, and one in *Choosing the
   specialist*, the table of addresses. The routing also says the cook is the only specialist that has passed its
   check, so Puls hands nothing else on, and *Waiting for validation* lists the roles that are off. Change those lines
   too, and `OnlyTheCook_IsARoleToDelegate_UntilTheOthersPassTheirCheck`, which pins them.
5. Turn it on. Add it to `ROLES_ENABLED` everywhere it's set. Where that setting is set (the Puls stack sets it), a
   role that isn't listed is refused, and start-up refuses a listed name the instance doesn't have.

If the specialist needs a capability Puls doesn't have, that's a pack
([Build a pack](../../../../src/docs/guides/build-a-pack.md)). Name it in `PulsIdentity.Packs`, and never reference
it as a project.

## Add a minion's function

A minion (F30) does one small task for a specialist, with the one tool the specialist hands it. Its prompt is
`minions/puls-minion-{function}.md`, and it loads after the shared base, `minions/puls-minion.md`. Its header's level
is `minion`.

Name it in the identity as you would a specialist: a `PulsRole` member with its file name, and a `PulsCall` member
with its address, `puls.{function}`. `EveryMinion_IsListedByItsAddress_AndReachesItsFile` fails while a minion's file
has no address. Then add the role to `ROLES_ENABLED`.

## Test it

```bash
dotnet test demos/puls/tests/LouisAgent.Identities.Puls.Tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/puls/tests/LouisAgent.Identities.Puls.Tests    # after a deliberate prompt change: then read the diff
```

`PulsIdentityTests` checks Puls's name and packs, one prompt in each folder, the catalogue refusing Puls without its
packs, every minion's address, and that only the cook declares toolsets. `PulsRoleTests` holds the roles and the role
files to each other. `PulsPromptsTests` checks the modes and roles listed, a language loading its voice and then the
routing, the languages' sections, the routing's rows, and a minion's prompt order. The files under `Prompts/` and
`PulsEstimatesPromptsTests` pin what the cook and its minion are told.

`PulsSessionSnapshotTests` pins exactly what the model is sent in Puls's chat and in its cook, in a golden file,
`PulsSessionSnapshot.json`. A golden file is the expected output saved beside the test. Any word you change in a
prompt changes it on purpose, so regenerate it and read the diff: the diff is the review of the prompt change.

Then try it in the Puls app ([Change the Puls web app](change-the-puls-app.md#run-it)) with a new chat. A change that
should make Puls better is measured by its evals before it's merged. Running them against the real model costs money,
so ask first.

## Before you ask for a review

- [ ] Every language changed the same way, and the session golden's diff is the change you meant.
- [ ] A new role is in `PulsRole` and `PulsCall`, has passed its eval, is in the routing, and is in `ROLES_ENABLED`
      everywhere it's set.
- [ ] A new capability is a pack named in `PulsIdentity.Packs`, not code in the identity.

---
[Guides](../../../../src/docs/guides/README.md) · [Docs index](../../../../src/docs/README.md)
