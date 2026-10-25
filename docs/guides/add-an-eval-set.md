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
the portal. Puls's set is in `evals/louis-agent.evals.puls`; it is empty until step 5 adds its cook and routing evals.

The example is `acme`, the identity [Build an identity](build-an-identity.md) makes: a hardware shop's assistant, whose
evals here are a quote for the tiles a room needs. `acme` is not in the repo, and the steps don't create it. Each step
names the file in the repo that does the same thing today (`StubEval`, `PulsEvalSet`, the Puls dataset, `NoErrorGrader`,
the stub's transcripts), so you can open working code beside this guide.

## 1. Create the projects

```bash
dotnet new classlib -o evals/louis-agent.evals.acme
rm evals/louis-agent.evals.acme/Class1.cs
mkdir tests/louis-agent.evals.acme.tests
cp tests/louis-agent.evals.puls.tests/louis-agent.evals.puls.tests.csproj tests/louis-agent.evals.acme.tests/louis-agent.evals.acme.tests.csproj
dotnet sln louis-agent-solution.sln add --solution-folder evals evals/louis-agent.evals.acme/louis-agent.evals.acme.csproj
dotnet sln louis-agent-solution.sln add --solution-folder tests tests/louis-agent.evals.acme.tests/louis-agent.evals.acme.tests.csproj
```

The evals project's csproj copies its datasets beside the assembly, as `louis-agent.evals.puls` does, and references the
harness. The harness names no identity, so nothing in it changes:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>LouisAgent.Evals.Acme</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- The dataset is data, copied beside the assembly so the runner can load it. -->
    <None Update="Datasets\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\louis-agent.evals\louis-agent.evals.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\docs\guides\add-an-eval-set.md" Link="docs\add-an-eval-set.md" />
  </ItemGroup>
</Project>
```

The tests project is the Puls tests' csproj with the names changed, and it also references the acme project and copies
its datasets beside the tests, as `louis-agent.evals.puls.tests` does:

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\evals\louis-agent.evals.acme\louis-agent.evals.acme.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\evals\louis-agent.evals.acme\Datasets\**\*" Link="Datasets\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
    <None Include="..\..\docs\guides\add-an-eval-set.md" Link="docs\add-an-eval-set.md" />
  </ItemGroup>
```

The identity, pack and evals projects link their guides this way, so a reader of the code finds the guide.

## 2. Write the dataset

A dataset is a folder, `Datasets/{name}/`, with a manifest and its scenarios. The loader reads it strictly:

```
evals/louis-agent.evals.acme/Datasets/acme-quotes/
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
- an `expected` key that no grader reads (step 3 names the keys);
- a scenario whose hash doesn't match `content_hash`, or a `changes` list that isn't one line per version.

**The hash.** Any change to a scenario changes the hash, so the loader refuses the folder until you bump `version`, add a
dated line to `changes`, and set `content_hash`. You don't compute the hash by hand: the refusal names it. Write any
placeholder, run the dataset's test (step 8), and paste the hash it reports.

`dev` scenarios are the ones you tune against. `held-out` scenarios are for a release check, which must not tune against
them. The runner doesn't select the held-out folder yet, so the split is recorded for when it does.

## 3. Choose the graders

Start from the common graders in `evals/louis-agent.evals/Grading/Common/`:

- `NoErrorGrader()`: no turn's stream reported an error;
- `WithinTimeGrader(TimeSpan limit)`: every turn was answered within the limit;
- `CostUnderGrader(decimal limitUsd)`: the trial's cost, from the stream's own usage, is under the limit;
- `RoutedToGrader(Func<Trial, string?> expectedRole)`: the first turn was routed to the role `expectedRole` gives for the
  trial (null means it must answer itself). A later turn's hand-over is no reason for a miss.

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
- `Unknown` (an infrastructure failure, a cost with no price, no usage) never counts as a pass;
- the cost comes from the stream's usage (`EngineEvents.CostOf`), never from a grader's own sum (ADR 0029);
- names are MCP-safe and unique, and versions are at least 1. The check requires the number; bumping it when a rule
  changes is your job, and nothing checks it.

## 4. Write the eval

An eval implements `IEval`. The stub's (`evals/louis-agent.evals.runner/Stub/StubEval.cs`) is the one complete example
today:

```csharp
public sealed class QuotesEval : IEval
{
    public string Name => "quotes";                       // MCP-safe; also the portal's check id (a Python check with the same id is hidden)
    public string? Identity => "acme";
    public int Version => 1;                              // bumped by any change to a scenario, a mode or a grader
    public string Proves => "Acme's assistant works out the tiles a room needs from its measurements.";   // the portal's title

    public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>([new EvalMode("acme-only", "acme/voice", ["web"])]);

    // Static, so the scenarios can load with them: the dataset checks its expected keys against these graders when it loads.
    private static readonly IReadOnlyList<IGrader> GraderList =
        [new NoErrorGrader(), new WithinTimeGrader(TimeSpan.FromMinutes(2)), new SquareMetresRight()];

    public IReadOnlyList<IGrader> Graders => GraderList;

    public IReadOnlyList<EvalScenario> Scenarios { get; } =
        DatasetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Datasets", "acme-quotes"), GraderList).Scenarios;

    public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) =>
        new QuotesScores(AreaRight: grades.All(grade => grade.Outcome == GradeOutcome.Pass));

    public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) =>
        new(Role: EngineEvents.RoutedRole(turn), Continued: null, AgentCalls: 0, SpecialistCalls: 0, Hosts: [],
            Error: EngineEvents.ErrorMessageOf(turn));
}

public sealed record QuotesScores(bool AreaRight) : EvalScores;
```

Two choices here, to check against the code:

- **The scenarios come from the dataset folder.** `StubEval` keeps its scenarios in code, because the stub proves the
  protocol and isn't a dataset. No eval reads its dataset yet; the Puls tests load theirs with `DatasetLoader.Load`
  (`tests/louis-agent.evals.puls.tests/Datasets/PulsDatasetsTests.cs`), and step 5's evals load theirs the same way. The
  path is `AppContext.BaseDirectory`, which is where the csproj copies the folder.
- **The load happens when the eval is built.** A dataset that fails to load stops the runner before a run. The runner
  refuses with the loader's reason on stderr and exit code 1, as it refuses its own refusals, and the portal's listing
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

The runner is the one project that knows every evals project, as the composition root knows every pack (ADR 0027). Four
lines:

1. `EvalSetKind.Acme` in `evals/louis-agent.evals.runner/Catalog/EvalSetKind.cs`;
2. its arm in `EvalSets.Create` (`evals/louis-agent.evals.runner/Catalog/EvalSets.cs`): `EvalSetKind.Acme => new AcmeEvalSet(),`;
3. a `ProjectReference` to `..\louis-agent.evals.acme\louis-agent.evals.acme.csproj` in `louis-agent.evals.runner.csproj`;
4. the line that copies its reference transcripts into the runner tests, in `louis-agent.evals.runner.tests.csproj`:
   `<None Include="..\..\evals\louis-agent.evals.acme\Transcripts\**\*" Link="Transcripts\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />`.

`EVALS=acme` picks this set, and unset `EVALS` lists every set. The conformance test already builds every set (F18 step
4.4 prep), so the new set's evals are checked with no edit to it.

## 7. Add the reference transcripts

An eval proves its checks work with transcripts in `Transcripts/{eval}/`, beside the eval:

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

Where the files come from: cut from a stored run where one passed (its conversation's protocol lines), or built by hand
from a reference, as the stub's are (`evals/louis-agent.evals.runner/Transcripts/stub/`). A known-bad `error` is a turn
with an `error` event; a known-bad `wrong-area` is a reference whose answer gives a different area.

## 8. Test it

Three kinds of test, each in the acme tests project:

- **Each grader on made-up transcripts**, a pass and a fail, as `tests/louis-agent.evals.puls.tests/Grading/FixedRules/`
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

Listing is free and calls no model. With `EVALS=acme`, the list has the acme eval, and the stub, which is always there:

```bash
EVALS=acme dotnet run --project evals/louis-agent.evals.runner -- --list
```

PowerShell: `$env:EVALS = 'acme'`, then the same command. Each line is one eval's JSON: its name, identity, version and
`proves`.

Running it is not free. `run {name} --repeat N` refuses every eval but the stub until step 5 brings the HTTP session
client: the runner has no client for a real eval yet, and says so with the eval's name (`RunnerMessage.NoSessionClient`).
Once a client is wired, a run against the real API spends money, so ask before starting one.

## 10. See it in the portal

The portal lists the runner's evals with no change in the API. [Change the API](change-the-api.md#add-an-eval-to-the-portal)
says what the portal shows and how to rebuild the image that serves it.

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
