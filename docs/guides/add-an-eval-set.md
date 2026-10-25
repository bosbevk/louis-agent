# Add an eval set

> **How-to.** Give an identity its evals: a project in `evals/` with its dataset, its graders, its evals and the set the
> runner lists under `EVALS=`, the reference transcripts that prove its checks work, and the tests. **Elsewhere:** why evals
> are C# and what they prove in [ADR 0029](../adr/0029-simulations-in-csharp.md); the feature in
> [F18](../features/F18-evaluation.md); the portal's view of them in
> [Change the API](change-the-api.md#add-an-eval-to-the-portal); running and writing tests in
> [Write and run tests](write-tests.md); the identity an eval proves in [Build an identity](build-an-identity.md).

An **eval** is a real task an identity has to do well, run against the real agent, and scored by graders that read the
transcript. An **eval set** is one identity's evals, in one evals project, with one `IEvalSet` class that lists them (one
set per project, ADR 0027). The harness, `evals/louis-agent.evals`, runs any set and knows none of them. The runner,
`evals/louis-agent.evals.runner`, is the one project that knows every set: it picks them with `EVALS=` and lists them for
the portal. Puls's set is in `evals/louis-agent.evals.puls`; it has two evals, the cook's (`CookEval`) and the routing's (`RoutingEval`), which are the
working examples to open beside this guide.

The example is `acme`, the identity [Build an identity](build-an-identity.md) makes: a hardware shop's assistant, whose
evals here are a quote for the tiles a room needs. `acme` is not in the repo, and the steps don't create it. Each step
names the file in the repo that does the same thing today (`StubEval`, `PulsEvalSet`, the Puls dataset, `NoErrorGrader`,
the stub's transcripts), so you can open working code beside this guide.

## Start from the template

`evals/louis-agent.evals.template` is the project to copy: the layout below with every member present and empty (F18 step 5.4c).
Its one eval, `Example`, has every `IEval` member with a comment saying what goes there and which section of this guide explains
it; what needs real work throws `NotImplementedException`, and the dataset has one working scenario, so the loader's rules (the hash,
the split from the folder, the expected key) are shown working. Copy the folder to `evals/louis-agent.evals.{name}`, rename
`Template` and `Example`, and follow sections 2 to 9 to fill it in. The template is never registered with the runner (`EvalSetKind`
doesn't list it), so no run and no portal shows it; `EvalLayoutTests` holds it to the layout, so a copy starts right.

A renamed copy can also be made with `dotnet new` (the template carries its `.template.config`). From `evals/`:

```bash
dotnet new install louis-agent.evals.template
dotnet new louis-eval-set -n Acme -o louis-agent.evals.acme    # Template becomes Acme, template becomes acme; rename Example by hand
dotnet new uninstall louis-agent.evals.template                 # when you are done: the install is on your machine, not in the repo
```

Then add the project to the solution and give it a tests project as in section 1 below (the commands there create an empty project
by hand; the copy replaces them, but not the solution entries and the tests project). Everything the copy has is described in the
sections that follow, so read on with it open.

## 1. Create the projects

```bash
dotnet new classlib -o evals/louis-agent.evals.acme
rm evals/louis-agent.evals.acme/Class1.cs
mkdir tests/louis-agent.evals.acme.tests
cp tests/louis-agent.evals.puls.tests/louis-agent.evals.puls.tests.csproj tests/louis-agent.evals.acme.tests/louis-agent.evals.acme.tests.csproj
dotnet sln louis-agent-solution.sln add --solution-folder evals evals/louis-agent.evals.acme/louis-agent.evals.acme.csproj
dotnet sln louis-agent-solution.sln add --solution-folder tests tests/louis-agent.evals.acme.tests/louis-agent.evals.acme.tests.csproj
```

**An eval project is cut per eval** (F18 step 5.4b, ADR 0010): one folder holds everything only that eval uses, with the same
names every time, and what two evals or more use is in `Shared/`, and only then. Adding, changing or deleting an eval touches
one folder. The namespace is the folder path, as everywhere (`LouisAgent.Evals.Puls.Evals.Cook`):

```
louis-agent.evals.acme/
  AcmeEvalSet.cs               the only file at the root
  Evals/
    Quotes/                    one folder per eval, named for the eval type (QuotesEval)
      QuotesEval.cs            the IEval
      QuotesSetup.cs           its modes, if it has any setup (optional)
      Grading/                 the graders only this eval uses
      Scoring/                 its scorer and its scores record
      Dataset/                 dataset.json, scenarios/dev/, scenarios/held-out/
      Transcripts/             {scenario}.reference.txt, {scenario}.bad.{label}.txt
  Shared/                      used by two evals or more: Grading/, Scoring/, Turns/, and the set's constants
  Messages/                    the set's message enums (ADR 0009)
```

`EvalLayoutTests` (the runner's tests) holds it for every set and for the stub: an eval's file, namespace, dataset and transcripts;
where each grader is; that every file of `Shared/` is used by two evals or more (directly, or through another `Shared/` file),
and names no eval; and what the project's root holds. A misplaced file fails there and names the rule. `louis-agent.evals.puls` is
the worked example to open beside this guide; the cook (`Evals/Cook/`) is the bigger eval, the routing the smaller.

The evals project's csproj has one data item, the same in every eval project: it copies each eval's `Dataset/` and `Transcripts/`
beside the assembly under `EvalFiles/{assembly}/...`, which reaches the runner and the tests through the project reference, and the
harness's `EvalFiles` finds it for any eval (no eval builds a path). `TargetPath`, not `Link`, because `Link` shows every file a
second time in the IDE, under a folder named like the project. The project references the harness, which names no identity, so
nothing in it changes:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>LouisAgent.Evals.Acme</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- An eval's data is beside it, and copied beside the assembly so the runner and the tests find it with EvalFiles. -->
    <None Update="Evals\**\Dataset\**\*;Evals\**\Transcripts\**\*" TargetPath="EvalFiles\$(AssemblyName)\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\louis-agent.evals\louis-agent.evals.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\docs\guides\add-an-eval-set.md" Link="docs\add-an-eval-set.md" />
  </ItemGroup>
</Project>
```

The tests project is the Puls tests' csproj with the names changed, and it also references the acme project. Its tests mirror the
project's folders (`Evals/Quotes/...`, `Shared/...`), and the data reaches them through that reference, so there is nothing to copy:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\evals\louis-agent.evals.acme\louis-agent.evals.acme.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\docs\guides\add-an-eval-set.md" Link="docs\add-an-eval-set.md" />
  </ItemGroup>
```

The identity, pack and evals projects link their guides this way, so a reader of the code finds the guide.

## 2. Write the dataset

A dataset is the eval's `Dataset/` folder, with a manifest and its scenarios. The loader reads it strictly:

```
evals/louis-agent.evals.acme/Evals/Quotes/Dataset/
  dataset.json
  scenarios/dev/quote-tiles.json
  scenarios/held-out/
```

`dataset.json` names the dataset, says what it covers, and records its hash and its history. Every field is required:

```json
{
  "name": "acme-quotes",
  "version": 1,
  "covers": "Tile quantities for a room: the area from its measurements.",
  "content_hash": "0000000000000000000000000000000000000000000000000000000000000000",
  "changes": [
    "2026-10-25: first version, the example scenarios of this guide"
  ]
}
```

Each scenario is one JSON file: its `id`, its `input` (`place`, `time_zone`, `messages`), its `expected` criteria and its
`categories`. This is a complete one (the input is in English; datasets keep the language they were recorded in):

```json
{
  "id": "quote-tiles",
  "input": {
    "place": "Paarl, Western Cape, South Africa",
    "time_zone": "Africa/Johannesburg",
    "messages": [
      "How many square metres of tile does a bathroom 2.4 by 1.8 metres need?"
    ]
  },
  "expected": {
    "square_metres": 4.32
  },
  "categories": [
    "measurements"
  ]
}
```

The loader refuses what it doesn't know, so a misspelled field is an error, not a silent default:

- a field it doesn't know, or a missing required one, or a null where the type isn't nullable;
- a scenario file that isn't `dev/{name}.json` or `held-out/{name}.json`, with the name in lowercase;
- an `id` used twice;
- an `expected` key that no grader reads, or a key a grader reads that no scenario sets (step 3 names the keys);
- a scenario whose hash doesn't match `content_hash`, or a `changes` list that isn't one line per version.

**The hash.** Any change to a scenario changes the hash, so the loader refuses the folder until you bump `version`, add a
dated line to `changes`, and set `content_hash`. You don't compute the hash by hand: the refusal names it. Write any
placeholder, run the dataset's test (step 8), and paste the hash it reports.

`dev` scenarios are the ones you tune against. `held-out` scenarios are for a release check, which must not tune against
them. A run asks for the dev ones by default (and the portal always does); `--split held-out` runs the held-out ones and
`--split all` both (step 9). Write the held-out scenarios fresh, and read them to nobody who is tuning.

**A scenario states what it asks for.** Everything a grader checks is a key of `expected`, and the question carries the same
facts, so nothing depends on a rule about the scenario's name: a cook question names its town, its people and its shop (the
`routes_to`, `town`, `serves`, `budget` and `shop` of Puls's cook), because Puls asks first when they are missing. A test that
reads the dataset and checks each question against its `expected` keys keeps the two from drifting apart
(`PulsDatasetsTests.EveryQuestionForTheCook_NamesItsTownItsPeopleAndItsShop_ElsePulsWouldAskFirst`).

## 3. Choose the graders

Start from the common graders in `evals/louis-agent.evals/Grading/Common/`:

- `NoErrorGrader()`: no turn's stream reported an error;
- `WithinTimeGrader(TimeSpan limit)`: every turn was answered within the limit;
- `CostUnderGrader(decimal limitUsd)`: the trial's cost, from the stream's own usage, is under the limit;
- `RoutedToGrader(Func<Trial, string?> expectedRole, bool required = true, IReadOnlySet<string>? expectedKeys = null)`: the first
  turn was routed to the role `expectedRole` gives for the trial (null means it must answer itself). A later turn's hand-over
  is no reason for a miss. An eval whose route is a scenario's key names it in `expectedKeys` (Puls: `routes_to`), so the loader
  can check it is read and fed.

Each is `required` by default, and the stub uses two of them (`NoErrorGrader` and `WithinTimeGrader`, in
`StubEval.cs`). An infrastructure failure (a timeout, a dropped connection) is `Unknown` in each of them, never a fail.
Your grader builds its grades with `Verdict`, which the common graders use too: `Verdict.Pass`, `Verdict.Fail`
and `Verdict.Unknown`. For your identity's rules, write your own `IGrader`. Its members are what the conformance check reads:

```csharp
public sealed class SquareMetresRight : IGrader
{
    public string Name => "square-metres-right";                  // MCP-safe: letters, digits, '-' and '_'
    public int Version => 1;                                       // bump it when the rule changes
    public GraderKind Kind => GraderKind.FixedRule;               // no model: free and repeatable
    public bool Required => true;                                  // a trial passes only if every required grader does
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>(["square_metres"]); // every key it reads from expected

    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // A turn that failed in the infrastructure (a timeout, a dropped connection) is not the agent's: Unknown, never a Fail.
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown) return ValueTask.FromResult(unknown);

        double want = trial.Scenario.Expected.GetProperty("square_metres").GetDouble();
        string answer = EngineEvents.AnswerOf(trial.Transcript.Turns[^1]);
        bool right = answer.Contains(want.ToString("0.00", CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return ValueTask.FromResult(right
            ? Verdict.Pass(Name, Version, MessageText.Format(AcmeGradeMessage.AreaRight, new { Want = want }))
            : Verdict.Fail(Name, Version, MessageText.Format(AcmeGradeMessage.AreaWrong, new { Want = want })));
    }
}
```

The reason is a message enum member, not a string (ADR 0009):

```csharp
public enum AcmeGradeMessage
{
    [Description("The area is {Want} square metres, and the answer gives it.")]
    AreaRight,

    [Description("The area should be {Want} square metres, and the answer doesn't give it.")]
    AreaWrong,
}
```

The rules every set must keep, which the catalogue and the conformance check hold:

- at least one required grader is a fixed rule (`Kind == FixedRule`), so no eval runs without a check that needs no model;
- every key in a scenario's `expected` is in some grader's `ExpectedKeys`;
- `Unknown` (a cost with no price, no usage) never counts as a pass; a trial whose turn failed for the infrastructure (a timeout, a dropped connection, an API error, a replay miss) is `Infrastructure` for the run: neither a pass nor a fail, left out of pass@k and pass^k and counted apart (a transport failure is run once more first);
- the cost comes from the stream's usage (`EngineEvents.CostOf`), never from a grader's own sum (ADR 0029);
- names are MCP-safe and unique, and versions are at least 1. The check requires the number; bumping it when a rule
  changes is your job, and nothing checks it.

## 4. Write the eval

An eval implements `IEval`. The stub's (`evals/louis-agent.evals.runner/Evals/Stub/StubEval.cs`) is the one complete example
today:

```csharp
public sealed class QuotesEval : IEval
{
    public string Name => "quotes";                       // MCP-safe; also the portal's check id (a Python check with the same id is hidden)
    public string? Identity => "acme";
    public int Version => 1;                              // bumped by any change to a scenario, a mode or a grader
    public string Proves => MessageText.Format(AcmeEvalMessage.QuotesProves);   // the portal's title: a message enum member (ADR 0009)

    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("acme-only", "acme/voice", ["web"])]);

    // Static, so the scenarios can load with them: the dataset checks its expected keys against these graders when it loads.
    private static readonly IReadOnlyList<IGrader> GraderList =
        [new NoErrorGrader(), new WithinTimeGrader(TimeSpan.FromMinutes(2)), new SquareMetresRight()];

    public IReadOnlyList<IGrader> Graders => GraderList;

    public IReadOnlyList<EvalScenario> Scenarios { get; } =
        DatasetLoader.Load(EvalFiles.DatasetOf(typeof(QuotesEval)), GraderList).Scenarios;

    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) =>
        new QuotesScores(AreaRight: grades.All(grade => grade.Outcome == GradeOutcome.Pass));

    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) =>
        new(Role: EngineEvents.RoutedRole(turn), Continued: null, AgentCalls: 0, SpecialistCalls: 0, Hosts: [],
            Error: EngineEvents.ErrorMessageOf(turn));
}

public sealed record QuotesScores(bool AreaRight) : EvalScores;

public enum AcmeEvalMessage
{
    [Description("Acme's assistant works out the tiles a room needs from its measurements.")]
    QuotesProves,
}
```

Two choices here, to check against the code:

- **The scenarios come from the dataset folder.** `StubEval` keeps its scenarios in code, because the stub proves the
  protocol and isn't a dataset. `CookEval` and `RoutingEval` load theirs with `DatasetLoader.Load`, and so do the Puls tests
  (`tests/louis-agent.evals.puls.tests/PulsDatasetsTests.cs`). The path comes from `EvalFiles.DatasetOf`, never built by the eval:
  it is where the csproj's one data item copied the folder.
- **The load happens when the eval is built.** A dataset that fails to load stops the runner before a run. The runner
  refuses with the loader's reason on stderr and exit code 1, as it does its own, and the portal's listing
  warning shows the same reason.

`TurnFactsOf` is what the protocol records about a turn: who answered, the calls it made, the hosts it read and its
error. The stub returns canned facts. A real eval reads them from the turn's events, with the harness's helpers
(`EngineEvents`).

## 5. Write the set

The set is the class the runner asks for, as `PulsEvalSet` is:

```csharp
public sealed class AcmeEvalSet : IEvalSet
{
    public string Identity => "acme";
    public IReadOnlyList<IEval> Evals { get; } = [new QuotesEval()];
}
```

## 6. Register it in the runner

The runner is the one project that knows every evals project, as the composition root knows every pack (ADR 0027). Three
lines:

1. `EvalSetKind.Acme` in `evals/louis-agent.evals.runner/Catalog/EvalSetKind.cs`;
2. its arm in `EvalSets.Create` (`evals/louis-agent.evals.runner/Catalog/EvalSets.cs`): `EvalSetKind.Acme => new AcmeEvalSet(),`;
3. a `ProjectReference` to `..\louis-agent.evals.acme\louis-agent.evals.acme.csproj` in `louis-agent.evals.runner.csproj`.

`EVALS=acme` picks this set, and unset `EVALS` lists every set. The conformance test and `EvalLayoutTests` already build every
set the runner knows, so the new set's evals are checked with no edit to either, and its data reaches the runner's and the runner
tests' output through that one reference (nothing to copy).

## 7. Add the reference transcripts

An eval proves its checks work with transcripts in its own `Transcripts/` folder (`Evals/Quotes/Transcripts/`), beside the eval:

- `{scenario}.reference.txt`: one per scenario, a conversation that passes every required grader;
- `{scenario}.bad.{label}.txt`: a conversation wrong in one way, one per way (`error`, `wrong-area`).

Each file is the protocol's own lines, each starting with `@`: the `mode` line first, then that conversation's `sse` and
`turn` lines. The reader keeps those three kinds and ignores the rest. The reference of `quote-tiles` looks like this,
shortened:

```
@{"event": "mode", "t": 1792877067.683, "mode": "acme-only", "repeat": 1, "toolsets": ["web"], "role": "acme/voice", "llm": null, "conversations": 1}
@{"event": "sse", "t": 1792877067.7, "mode": "acme-only", "repeat": 1, "task": "quote-tiles", "turn": 1, "type": "content_block_delta", "data": {"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "You need 4.32 square metres: 2.4 by 1.8."}}}
@{"event": "turn", "t": 1792877071.9, "mode": "acme-only", "repeat": 1, "task": "quote-tiles", "turn": 1, "session": "sess_acme-only_1_quote-tiles", "question": "How many square metres of tile does a bathroom 2.4 by 1.8 metres need?", "seconds": 4.2}
```

The rules the conformance check holds:

- every required grader fails at least one known-bad of the eval (only a `Fail` counts, not an `Unknown`);
- no known-bad passes every required grader;
- a file that is neither a scenario's reference nor one of its known-bads is stray, and refused: a misspelling, or a
  deleted scenario's files.

Where the files come from: cut from a stored run where one passed (`GET /v1/admin/benchmarks/runs/{id}/protocol` returns a stored
run as these lines, with the admin key in `x-api-key`; take one conversation's lines), or built by hand from a reference, as the stub's are
(`evals/louis-agent.evals.runner/Evals/Stub/Transcripts/`) and as Puls's first ones are: no run was recorded yet, so each is a short conversation
written to pass the required graders. A script that writes them is fine; the files are what is committed.

Two things the reference decides without saying so. Its `mode` line names the mode the graders judge it in: a mode without the delegating
toolsets expects the agent to answer itself, so a reference that hands a question on needs a mode that can. And the lines the reader keeps are
`sse` (type and data) and `turn` (`session`, `question`, `seconds`; `failure` when the turn failed for the infrastructure); the rest are ignored. A known-bad `error` is a turn
with an `error` event; a known-bad `wrong-area` is a reference whose answer gives a different area.

## 8. Test it

Three kinds of test, each in the acme tests project:

- **Each grader on made-up transcripts**, a pass and a fail, as `tests/louis-agent.evals.puls.tests/Evals/Cook/Grading/`
  does: build a `Trial` with the transcript it needs, grade it, and assert the outcome.
- **The dataset loads strictly and its hash holds**, as `PulsDatasetsTests` does: load the folder with the eval's graders.
  When the hash is wrong, this test is where `HashChanged` names the new one.
- **The conformance test** covers every set, so the new evals are in it with no edit. The check is in the runner tests.

```bash
dotnet test tests/louis-agent.evals.acme.tests
dotnet test tests/louis-agent.evals.runner.tests --filter "FullyQualifiedName~EvalConformance"
dotnet test tests/louis-agent.evals.runner.tests
```

## 9. List it and run it

The flags, record and replay, the dev and held-out scenarios and the portal are in [Run the evals](run-the-evals.md); this section is the short version.

Listing is free and calls no model. With `EVALS=acme`, the list has the acme eval, and the stub, which is always there:

```bash
EVALS=acme dotnet run --project evals/louis-agent.evals.runner -- --list
```

PowerShell: `$env:EVALS = 'acme'`, then the same command. Each line is one eval's JSON: its name, identity, version and
`proves`.

Running it is not free. `run {name} --repeat N [--split dev|held-out|all]` gives every eval but the stub the HTTP session client (F18 step 5.1): the API
at `PULS_API` (default `http://127.0.0.1:5070/`), with `AGENT_API_KEY` as `x-api-key` when it is set. A run against the real
API spends money, so ask before starting one. A turn that fails for the infrastructure (a timeout, a refused connection, a
broken stream, a non-2xx answer) is recorded in its `turn` line as `failure` and the run goes on.

## 10. See it in the portal

The portal lists the runner's evals with no change in the API. [Change the API](change-the-api.md#add-an-eval-to-the-portal)
says what the portal shows and how to rebuild the image that serves it. Its grades show in the run with nothing to add: the
run page reads only the standard records (your eval's name, version and graders on the run; each conversation's grades and
outcome), so every grader you wrote is a chip on its conversation, in your `Graders` order, with its reason on hover
([Change the Puls app](change-the-puls-app.md#change-the-admin-portal)).

## 11. Record a run, and replay it

A run that reads the live web measures the website as much as the agent. `--web record` saves every page, search, fetch and the time
the run reads; `--web replay --of {runId}` answers from those saved inputs later, without the browser, the search engine, the network or the clock. Both are
flags of the same command, and the runner has no web of its own: the API writes and reads the recordings.

A recording also keeps the model's answers (step 4e), so a run can be replayed three ways. Pick by what you changed:

| Command | The web | The model | What it tests | Cost |
|---|---|---|---|---|
| `--web record` | live, saved | live, saved | the baseline: the recording itself | the run's own |
| `--web replay --of {runId}` | the recording's | **live** | **a prompt change**: the same pages, a new model answer, so two runs differ only in the prompt (`prompt_hash` in the ledger says which) | the run's own |
| `--replay {runId}` | the recording's | the recording's | **everything around the model**: the graders, the harness, the engine's code | $0, in seconds |

`--replay {runId}` is `--web replay --of {runId}` with the model's answers too, and it can't be given with `--web` or `--of`. It can't test a
prompt change: a changed prompt is a different request, so it misses, and the miss says where the request changed (the model, the system
prompt, a tool, the settings, or message *n*). Test a prompt change with the second row.

```bash
EVALS=acme BENCHMARK_RUN=acme-before dotnet run --project evals/louis-agent.evals.runner -- run acme --web record
EVALS=acme BENCHMARK_RUN=acme-after  dotnet run --project evals/louis-agent.evals.runner -- run acme --web replay --of acme-before
```

- **The run's id** is `BENCHMARK_RUN` when you set it, else `local-` and the time the run started. Set it yourself, so you
  know what to replay: `--of` takes the recorded run's id, and only its letters, digits, `-` and `_`.
- **Where the pages live.** The API writes them, into its `REPLAY_DIRECTORY` (the Puls compose sets `/data/replay`, which is
  `data/puls/replay` on the host). The runner never touches that folder. Without `REPLAY_DIRECTORY` the API refuses a record or
  a replay with a 400 that says this instance keeps no recordings. The recording holds one answer per distinct request in a trial, so an
  identical request twice in one trial is answered by its first recording.
- **They stay on that machine.** Recordings are private: nothing serves them, and `/data/` is git-ignored, so they never go in
  the repo.
- **A replay that misses.** What the recorded run didn't read comes back to the model as a short text, and the model treats it as
  unknown: a page by its address ("was not read in the recorded run"), a search by its query ("was not run in the recorded run"),
  and the clock ("never read the clock"). The time stands still within a trial, so a replay shows the time the recording did.
- **What is still live.** The Tavily renderer (`PAGE_RENDERER=tavily`) reads the page itself, and `ReadRenderedPage` still resolves DNS in
  a replay. A failed fetch replays as a miss.
- **Only a real eval records.** The stub reads no web, so a recording of it saves nothing. A recording of `puls-cook` or `puls-routing`
  spends money and waits for Louis's go. A replay with `--replay` is free.

## Before you ask for a review

- [ ] The eval's and every grader's names are MCP-safe and unique; versions are at least 1, and bumped with each change.
- [ ] The dataset's version is bumped, with a dated line in `changes`, for any change to a scenario; its hash is the one the test names.
- [ ] Every `expected` key is read by a grader; every grader's `ExpectedKeys` is what it reads.
- [ ] A required fixed-rule grader exists; `Unknown` never counts as a pass; cost comes from the stream's usage.
- [ ] Every reason is a message enum member.
- [ ] Each scenario has a reference transcript; each required grader fails at least one known-bad; no stray files.
- [ ] The graders' tests pass and fail; the dataset test and the conformance test are green.
- [ ] The projects are in the solution and link this guide; the runner lists the set with `EVALS=`.

---
[Guides](README.md) · [Docs index](../README.md)
