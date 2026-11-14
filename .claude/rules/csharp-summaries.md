---
paths:
  - "**/*.cs"
---

# C# summaries: how one is written

Loaded when a `.cs` file is read or edited, beside `csharp.md`, which holds the other rules of the code.

## What a class and a method carry

Every class and method has a `<summary>`, private and test ones included. A class (record, interface) has up to three
parts; a method its `<summary>`, and a `<remarks>` when it carries a pattern of its own:

- `<summary>`: what it does, written like a person (below).
- `<remarks>`: when it carries a pattern, which one and **why it was chosen here**: the problem it solves for this
  class, and what we'd lose without it.
- `<seealso>`: one per ADR that shaped this class (not every ADR that applies), linked on GitHub, as
  `ADR 0024: choices are enums`.

## Like a person, not a robot

**Write them like a person, not a robot:** say what it does for you, the way you'd tell a colleague at your desk, in
one or two short sentences; leave out what the code already shows (the types, null, "returns", "true when", every
edge case); step numbers belong in the commit and the feature doc.

| Robot | Person |
|---|---|
| *True when the browser address is set and is an absolute URL.* | *We can only use Chrome if we know where it runs.* |
| *Strategy: the setup of the renderer named, else the first ready one in Setups; null when the name is unknown or none is ready.* | *Uses the renderer you named. If you named none, it takes the first one that's set up.* |
| *Chrome.* | *This is the Chrome one.* |
| *Null Object: whichever settings are given, a setup's renderer is the unavailable one exactly when the setup is not ready.* (a test) | *If a renderer isn't set up you get the "not set up" one, and only then.* |

A whole class, `ChromeSetup`:

```csharp
/// <summary>
/// Reads pages through our own headless Chrome. If we don't know where Chrome runs, you get a renderer that tells
/// the model to set BROWSER_URL.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. Each renderer knows for itself whether it's set up and how to build itself, so
/// "is it ready?" and "build it" can't drift apart, and a new renderer is one new class instead of another case in
/// two switches. When Chrome isn't set up it hands back a renderer that explains what's missing, so nothing has to
/// check for null and the model still gets a useful answer.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0038-the-browser-says-what-it-is.md">ADR 0038: the browser says what it is</seealso>
internal sealed class ChromeSetup : IPageRendererSetup
```

## Inside a method

**Inside a method, explain the steps:** a step a reader can't follow at a glance (building a request, a `try` that
hides a failure path, a trick such as `using var _`, a rule the code enforces) gets one plain `//` line above it: what
it does and why, like a person. Obvious lines (`_busy = true;`) stay bare. **Steps that belong together sit together,
with one blank line between groups**, like paragraphs: each group does one job and carries its comment line on top
when it needs one; a test's arrange, act and assert are three groups. From `NotesPanel.AddAsync`:

```csharp
// Post it to this run's notes. The run id is escaped because it goes into the web address.
using var request = new HttpRequestMessage(HttpMethod.Post, $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/notes") { Content = JsonContent.Create(body) };
// We only need to know it worked. If the API says no, it throws, and the catch below shows why.
using var _ = await Api.SendAsync(request);
// Saved: empty the box, and tell the run page to fetch the notes again (we never add it to the list ourselves).
_text = "";
State.Written();
```

And grouped, from a test's `StartAsync`:

```csharp
// A store that already holds one finished run.
var store = new InMemoryBenchmarkStore();
await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5), 0));

// The API with the admin key, that store, and a clock that never moves.
var app = ApiTestHost.Build(adminKey: AdminKey, store: store, time: new FixedClock(Now));
await app.StartAsync();

return new Host(app, app.GetTestClient(), store);
```

*Decided in:* [ADR 0050](../../src/docs/adr/0050-summaries-like-a-person.md).
