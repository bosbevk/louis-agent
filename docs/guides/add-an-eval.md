# Add an eval

> **How-to.** Give an identity (or a pack) an eval: copy the template folder beside it, write its `eval.json`, its
> dataset, its manual and its transcripts, and pick its checks from the catalogue. **Elsewhere:** why an eval is a
> folder of files in [ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md), why a check is a catalogue entry in
> [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md); running, recording, replaying and reading the results in
> [Run the evals](run-the-evals.md); the identity an eval proves in [Build an identity](build-an-identity.md).

An **eval** is a real task an identity has to do well, run against the real agent and graded by checks that read the
transcript. It is a folder of files: `eval.json` says what it is and which checks grade it, and its dataset, manual and
transcripts sit beside it. An eval that uses the harness's checks needs no C# at all.

The example is `acme`, the hardware shop's assistant [Build an identity](build-an-identity.md) makes, with an eval that
asks how many square metres of tile a room needs. `acme` is not in the repo. Puls's two evals are the working
examples to open beside this guide: `identities/louis-agent.identity.puls/Evals/Routing/` (one general check) and
`Evals/Cook/` (Puls's own checks, a judge and a person's review).

## 1. Copy the template

`evals/template/Example/` is a working eval to copy: one mode, two general checks, one scenario, a manual and a
reference with its known-bad. It loads and conforms (`TemplateFolderTests`), and no project copies it, so it never
shows in a run. Copy it under your identity's `Evals/`, named for the task:

```bash
cp -r evals/template/Example identities/louis-agent.identity.acme/Evals/Quotes
```

The identity's project copies every file under `Evals/` beside the build, which is how the runner finds it; a new
identity needs this item once in its csproj, as Puls's has:

```xml
<None Update="Evals\**\*" TargetPath="EvalFiles\identities\$(MSBuildProjectName)\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
```

The runner reaches the identity's files through the composition, so a new identity must be registered there too
([Build an identity](build-an-identity.md#5-register-it-in-the-composition-root)). There's nothing else to register:
every `eval.json` copied beside the runner is an eval.

## 2. Write `eval.json`

```json
{
  "schema_version": 1,
  "name": "acme-quotes",
  "identity": "acme",
  "version": 1,
  "proves": "Acme works out the tiles a room needs from its measurements.",
  "manual": "manual.md",
  "modes": [
    { "name": "acme-only", "role": "acme/voice", "toolsets": ["web"] }
  ],
  "checks": [
    { "type": "no-error" },
    { "type": "contains", "parameters": { "texts": ["4.32"] } },
    { "type": "within-time", "parameters": { "seconds": 120, "required": false } }
  ]
}
```

- **`name`** is how the runner, the store and the portal know it: lowercase, letters, digits and `-`, unique.
- **`identity`** is who it proves; `EVALS=acme` runs only acme's evals.
- **`version`** is a whole number you bump when a scenario, a mode or a check changes, so the trend never compares two
  different evals. Nothing bumps it for you.
- **`modes`** are the ways the same conversations run: a role and its toolsets each. Two modes compare, for example,
  the agent alone with the agent handing work to a specialist.
- **`checks`** grade each conversation, in this order on the portal. `parameters` holds the check's settings, in snake
  case; leave it out when the defaults do. `name` keeps an old name a stored run already carries (Puls's `cook-no-error`);
  a new eval leaves it out.

The file is read strictly: a field it doesn't know, one missing, a null, a mode or a check twice, or a check type the
catalogue doesn't have is refused, naming the file and the field.

## 3. Write the dataset

`Dataset/` holds `dataset.json` (the name, version, what it covers, the scenarios' hash and a dated line per version)
and the scenarios, one file each, in `scenarios/dev/` (the ones you tune against) or `scenarios/held-out/` (for a
release check only; read them to nobody who is tuning). A scenario is Inspect's Sample shape:

```json
{
  "id": "quote-tiles",
  "input": [{ "role": "user", "content": "How many square metres of tile does a bathroom 2.4 by 1.8 metres need?" }],
  "metadata": {
    "place": "Paarl, Western Cape, South Africa",
    "time_zone": "Africa/Johannesburg",
    "follow_ups": [],
    "expected": {},
    "categories": ["measurements"]
  }
}
```

`expected` holds the facts a check reads from the scenario (`routes_to` for `routed-to`, `serves` for Puls's cook);
the loader refuses a key no check reads, and a check's key no scenario sets. **The hash:** any change to a scenario
changes it, and the loader refuses the folder until you bump `version`, add a dated line to `changes` and set
`content_hash`. You never compute it: write any placeholder, run the test, and paste the hash the refusal names.

## 4. Choose the checks

The harness's general checks, which any eval can name:

| Type | Settings | Passes when |
|---|---|---|
| `no-error` | `required` | no turn's stream reported an error |
| `within-time` | `seconds`, `required` | every turn was answered within the limit |
| `cost-under` | `limit_usd`, `required` | the trial's cost, from the stream's usage, is under the limit |
| `contains` | `texts`, `required` | the first answer holds one of the texts, case and accents ignored |
| `tool-used` | `tool`, `required` | a tool with this name was called, by the agent or a specialist |
| `routed-to` | `expected_key`, `delegating_toolsets`, `required` | the first turn went to the role under the scenario's key; a mode without a delegating toolset must answer itself |
| `judge` | `rubric`, `ask`, `version` | a model, given the rubric file as its prompt and the scenario's `ask` keys, says yes |
| `human` | `reviews` | a person's review of the named judge (shown, never required) |

Each is required unless you say `"required": false`, a `judge` never is, and a timeout or a dropped connection is
`Unknown`, never a fail. Every eval needs at least one required check that asks no model (a fixed rule), so a run
always has a check that is free and repeatable.

**A check of your own** is C#, because a rule is code: a class that implements `ICheck` (from the check contract,
`evals/louis-agent.evals.checks`), with a nested `Entry` that builds it from a typed `Settings` record, listed in your
identity's list (as Puls's six are in `PulsChecks.All`, in `Evals/Checks/Cook/Grading/`). The composition hands every
list to the runner (`EvalChecks.All`), so add yours there. The worked examples are the cook's (`WithinBudget`,
`RecipeImage`) and their tests beside the identity's tests. Its reasons are message enum members (ADR 0009), and a turn
that failed for the infrastructure is `Unknown` (`Verdict.InfrastructureFailure`). A model judge of your own needs the
harness, which an identity never references, so it goes in a project of its own beside the identity that only the
runner references, as `identities/louis-agent.identity.puls.judges` does; the general `judge` with a rubric file
usually does instead.

## 5. Write the manual

`manual.md` is what the portal opens from the eval's card: what it asks, each mode, each check and what its failure
means, and what to do about it. In English, opening with a title, naming the eval and every check in backticks; the
cook's (`Evals/Cook/manual.md`) is the full example.

## 6. Add the reference transcripts

`Transcripts/` proves the checks work. Each file is the protocol's own lines, one JSON event per line, as the runner
prints them: the `mode` line, then the conversation's `question`, `sse` and `turn` lines (the template's are the
shortest example).

- `{scenario}.reference.txt`: one per scenario, a conversation that passes every required check;
- `{scenario}.bad.{label}.txt`: a conversation wrong in one way, one per way (`error`, `wrong-area`).

Every required check must fail at least one known-bad (a `Fail`; an `Unknown` doesn't count), no known-bad may pass
every required check, and any other file is stray and refused. Cut them from a stored run where one passed
(`GET /v1/admin/evals/runs/{id}/protocol` with the admin key in `x-api-key`), or write them by hand, as the template's are.

## 7. Test it

`EvalConformanceTests` (in the runner's tests) runs every eval folder it finds, so your references and known-bads are
checked with no edit. Beside the identity's tests, add one test file for the eval, as Puls's
`Evals/Routing/RoutingEvalTests.cs` does: it loads the folder with the runner's catalogue and holds its name, version,
modes, checks and the shape of its dataset. A check of your own gets a pass and a fail on made-up transcripts.

```bash
dotnet test tests/louis-agent.evals.runner.tests --filter "FullyQualifiedName~EvalConformance"
dotnet test tests/louis-agent.identity.acme.tests
```

## 8. List it, and run it

Listing is free and calls no model; the stub is always listed too:

```bash
EVALS=acme dotnet run --project evals/louis-agent.evals.runner -- --list
```

Running one talks to the API and spends money, so ask first. The flags, recording and replaying, the dev and held-out
scenarios, and the portal are in [Run the evals](run-the-evals.md); the portal shows a new eval with no change in the
API ([Change the API](change-the-api.md#add-an-eval-to-the-portal)).

## Before you ask for a review

- [ ] The eval's name is unique and lowercase; its version is bumped with each change to a scenario, a mode or a check.
- [ ] The dataset's version is bumped, with a dated line in `changes`, for any change to a scenario; its hash is the one the refusal named.
- [ ] Every `expected` key is read by a check, and at least one required check is a fixed rule.
- [ ] The manual names the eval and every check.
- [ ] Each scenario has a reference; each required check fails at least one known-bad; no stray files.
- [ ] `--list` shows it under `EVALS={identity}`; the conformance test and the eval's own test are green.

---
[Guides](README.md) · [Docs index](../README.md)
