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
  case; leave it out when the defaults do. `name` keeps an old name a stored run already carries when a check's type
  changes under it (none of Puls's today); a new eval leaves it out.

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

The harness's general checks, which any eval can name, by family. "The answer" is the first turn's; every setting
list also takes `required`:

| Type | Settings | Passes when |
|---|---|---|
| **Run facts** | | |
| `no-error` | | no turn's stream reported an error |
| `within-time` | `seconds` | every turn was answered within the limit |
| `cost-under` | `limit_usd` | the trial's cost, from the stream's usage, is under the limit |
| `turns-under` | `count` | the trial took fewer turns than the count |
| `tokens-under` | `count` | the trial's tokens (input, cache and output) stay under the count |
| **Text rules** (case and accents ignored) | | |
| `contains` · `not-contains` | `texts` | the answer holds one of the texts · none of them |
| `starts-with` | `text` | the answer, trimmed, opens with the text |
| `length-under` | `characters` | the answer is shorter than that |
| `mentions-expected` | `key` | the answer names the scenario's value under the key (its town, its shop) |
| **Structure** | | |
| `is-json` | | the answer, or its fenced block, parses as JSON |
| **Tool calls** (the agent's and its specialists') | | |
| `tool-used` · `tool-not-used` | `tool` | the tool was called · never called |
| `tool-used-with` | `tool`, `field`, `value` | the tool was called with that input field set to the value |
| `tool-calls-under` | `count` | fewer tool calls than the count, hand-overs included |
| `routed-to` | `expected_key`, `delegating_toolsets` | the first turn went to the role under the scenario's key; a mode without a delegating toolset must answer itself |
| **Extract, then compare** (through a reader) | | |
| `amount-under` | `key`, `reader` | the answer's total, as the reader reads it, is at most the scenario's amount |
| `number-named` | `key`, `reader` | each turn names the scenario's number (one, or one per turn) |
| `image-shown` | `reader`, `key` (optional) | the answer shows an image a page the agent read gave; with a key, only where the scenario sets it to true |
| `links-to-host` | `hosts` | the answer links to one of the hosts, or a subdomain of one |
| **Similarity and language** | | |
| `near` | `text`, `distance` | the whole answer is at most that many edits from the text (Levenshtein, case ignored) |
| `answers-in-language` | `language` (`af`, `en`) | the answer has more of that language's common small words than of any other's |
| **Model and person** | | |
| `judge` | `rubric`, `ask`, `version` | a model, given the rubric file as its prompt and the scenario's `ask` keys, says yes |
| `human` | `reviews` | a person's review of the named judge (shown, never required) |

Each is required unless you say `"required": false`, a `judge` never is, and a timeout or a dropped connection is
`Unknown`, never a fail. A compare passes a scenario that sets no key ("not asked"). Every eval needs at least one
required check that asks no model (a fixed rule), so a run always has a check that is free and repeatable.

**A reader** is what a compare reads the value with, named beside its key: `{"type": "amount-under", "name":
"within-budget", "parameters": {"key": "budget", "reader": "rand-total"}}`. The harness ships `currency-amount`,
`digits` and `markdown-images`; an identity ships its own when the value is written its way (Puls's `rand-total`,
`rand-prices`, `servings` and `recipe-photos`, in `Evals/Readers/`). A reader is a small class that implements
`IMoneyReader`, `INumberReader`, `IImageReader`, `IPriceReader` or `ILinkReader` (from the check contract,
`evals/louis-agent.evals.checks`), listed in your
identity's list (`PulsReaders.All`), which the composition hands to the runner (`EvalReaders.All`). `name` keeps the
name your stored grades carry when the type is a general one.

**A check of your own** is for a rule no general check and reader can say. It is C#, because a rule is code: a class
that implements `ICheck`, with a nested `Entry` that builds it from a typed `Settings` record, listed in its owner's
list (as the shops pack's rules are in `ShopsChecks.All`, in `packs/louis-agent.pack.shops/Checks/`), which the
composition hands to the runner (`EvalChecks.All`). A rule that proves a pack's tools is the pack's
([Build a pack](build-a-pack.md#9-its-checks-when-it-proves-its-tools), ADR 0060). The worked example is
`EstimatesAtShop` and its tests. Its reasons
are message enum members (ADR 0009), and a turn that failed for the infrastructure is `Unknown`
(`Verdict.InfrastructureFailure`). A model judge of your own needs the harness, which an identity never references,
so it goes in a project of its own beside the identity that only the runner references, as
`identities/louis-agent.identity.puls.judges` does; the general `judge` with a rubric file usually does instead.

**A rubric file** (`Rubrics/{name}.md`) opens with the header every prompt has
([ADR 0063](../adr/0063-a-prompt-says-what-it-is.md)): its `name` (the file's) and a `description` of what it judges.
The judge is sent only the body after it, and the rubric's hash, which a calibration records, is the body's, so the
header can change without moving a calibration:

```markdown
---
name: meal-for-people
description: The cook eval's judge rubric. Is the answer a main dish that feeds the number of people asked for.
---
# Judge: is this a meal for the people asked for
```

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

**From the portal, instead of by hand** (F49.4): on a stored run's conversation, *Sample* makes a new sample from
it (a variant of the scenario it ran: change the id, the expected values, the categories, add an ideal answer), and
after you grade a conversation, *Keep as reference* (a pass) or *Keep as known-bad* (a fail, with a reason in
kebab-case) keeps its transcript. Both wait as drafts on the Evals page, under the eval's card
([ADR 0061](../adr/0061-evals-grow-from-use.md)), and the studio's publish writes them into the eval's folder with
it ([section 9](#9-or-compose-it-in-the-studio)).

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

## 9. Or compose it in the studio

The steps above work in any editor. The admin portal's **Studio** page (`/admin/studio`, *Studio* in the admin
menu) does the same from the browser, and ends in the same folder of files
([ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)). The page numbers its steps (Compose, Check, Try,
Publish), and each one saves the draft first:

1. **Compose.** *New eval* opens a form over `eval.json`: the name, the identity, the version, what a pass means, the
   manual's file name, the modes; *Add a check…* lists the runner's catalogue, and each check's fields come from its
   settings (a reader is picked from the readers the runner has); the rubrics are text boxes. *View JSON* shows the
   file the form makes.
2. **Save.** The eval is kept as a draft under your reviewer's name, with the sample and transcript drafts made for
   it from runs (section 6). Nothing in the repository changes yet.
3. **Check.** The runner loads the draft's folder as strictly as a run would and holds it to the conformance gate the
   tests run (section 7); every problem shows in its own words. Fix them and check again.
4. **Try.** Pick a run to replay from the list (only runs that recorded their answers, this eval's own first) and, if
   you like, a few samples (commas between). A try is always a replay, so it costs nothing; the link opens its run
   page. A fresh run stays the Run button's, and spends money.
5. **Publish.** Only a draft the runner found no problem with is published. On a machine whose workspace is a git
   checkout, the folder is committed to `identities/louis-agent.identity.{identity}/Evals/{Name}/` on the branch
   `eval/{identity}-{name}`, without touching your checkout; push the branch and open the pull request yourself.
   Where the API has no git workspace (Azure), the folder comes as a zip to add in a pull request.

**promptfoo in and out.** Paste a `promptfooconfig.yaml` in the studio's *promptfoo* box and *Import*: its tests
become the draft's scenarios, its asserts our checks by a table (`contains`, `not-contains`, `starts-with`,
`is-json`, `levenshtein`, `latency`, `cost`), each `llm-rubric` a rubric the `judge` reads, and everything else is
listed as left out, so you know what to add by hand. *Export the open draft* saves it, then downloads it as a
promptfoo file. The command line has the same two: `--import-promptfoo {file} {folder}` and
`--export-promptfoo {folder}`.

## Before you ask for a review

- [ ] The eval's name is unique and lowercase; its version is bumped with each change to a scenario, a mode or a check.
- [ ] The dataset's version is bumped, with a dated line in `changes`, for any change to a scenario; its hash is the one the refusal named.
- [ ] Every `expected` key is read by a check, and at least one required check is a fixed rule.
- [ ] The manual names the eval and every check.
- [ ] Each scenario has a reference; each required check fails at least one known-bad; no stray files.
- [ ] `--list` shows it under `EVALS={identity}`; the conformance test and the eval's own test are green.

---
[Guides](README.md) · [Docs index](../README.md)
