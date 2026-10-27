using System.Text.Json;
using LouisAgent.Testing;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

// The catalogue (F18 step 4.1): every eval the runner can run, each by name once, and none that no run can trust. Written first:
// the catalogue threw NotImplementedException on the red run.
public class EvalCatalogTests
{
    private static readonly IEval Stub = new StubEval();

    [Test]
    public void TheStubIsAlwaysListed_EvenWithNoSets()
    {
        var catalog = new EvalCatalog([], Stub);

        Assert.That(catalog.Listing().Select(l => l.Name), Is.EqualTo(new[] { "stub" }));
    }

    [Test]
    public void EachEval_IsListedOnce()
    {
        var catalog = new EvalCatalog([new FakeSet(new FakeEval("cook")), new FakeSet(new FakeEval("routing"))], Stub);

        Assert.That(catalog.Listing().Select(l => l.Name), Is.EqualTo(new[] { "cook", "routing", "stub" }));
    }

    [Test]
    public void RefusesTwoEvalsWithOneName()
    {
        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook")), new FakeSet(new FakeEval("cook"))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook'"));
    }

    [Test]
    public void RefusesAnEvalWithNoGraders()
    {
        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", []))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook' has no graders"));
    }

    [Test]
    public void RefusesAnEvalWithOnlyModelGraders()
    {
        var model = new FakeGrader(GraderKind.Model, required: true);

        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", [model]))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook' has no required fixed-rule grader"));
    }

    [Test]
    public void RefusesAnEvalWhoseFixedRuleIsNotRequired()
    {
        // A fixed rule that doesn't decide the trial is no check: the eval needs one it must pass.
        var optional = new FakeGrader(GraderKind.FixedRule, required: false);

        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", [optional]))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook' has no required fixed-rule grader"));
    }

    [Test]
    public void AcceptsAnEvalWithARequiredFixedRule()
    {
        var catalog = new EvalCatalog([new FakeSet(new FakeEval("cook", [new FakeGrader(GraderKind.FixedRule, required: true)]))], Stub);

        Assert.That(catalog.Find("cook"), Is.Not.Null);
    }

    [Test]
    public void FindReturnsTheEvalByNameOrNull()
    {
        var catalog = new EvalCatalog([], Stub);

        Assert.That(catalog.Find("stub"), Is.SameAs(Stub));
        Assert.That(catalog.Find("nope"), Is.Null);
    }

    [Test]
    public void GetRefusesAnUnknownNameNamingTheEvals()
    {
        // The name is refused by the catalogue, the one place that knows the evals, not by the host (F18 step 4.1 review).
        var catalog = new EvalCatalog([], Stub);

        Assert.That(catalog.Get("stub"), Is.SameAs(Stub));
        Assert.That(() => catalog.Get("nope"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'nope'").And.Message.Contains("stub"));
    }

    [Test]
    public void TheListingIsOneJsonObjectPerEval_AsTheAPIReadsIt()
    {
        // The line is read back as JSON, not compared as text: the encoder may escape an apostrophe, and the API parses it either way.
        var listing = new EvalCatalog([], Stub).Listing();
        var line = JsonDocument.Parse(JsonSerializer.Serialize(listing.Single(), EvalJson.SnakeCase)).RootElement;

        Assert.That(line.EnumerateObject().Select(p => p.Name), Is.EqualTo(new[] { "name", "identity", "version", "proves", "manual" }));
        Assert.That(line.GetProperty("manual").GetString(), Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"), "the manual's path from the repo root, forward slashes (7.1)");
        Assert.That(line.GetProperty("name").GetString(), Is.EqualTo("stub"));
        Assert.That(line.GetProperty("identity").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(line.GetProperty("version").GetInt32(), Is.EqualTo(1));
        Assert.That(line.GetProperty("proves").GetString(), Is.EqualTo(MessageText.Format(RunnerMessage.StubProves)));
    }

    [Test]
    public void RefusesAScenarioWithNoMessages()
    {
        // Nothing to send is nothing to run: refused when the catalogue is built, not skipped by the runner (F18 step 4.1 review).
        var empty = new EvalScenario("empty", new EvalInput(null, "Africa/Johannesburg", []), default, [], DatasetSplit.Dev);

        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", scenarios: [empty]))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'empty'").And.Message.Contains("no messages"));
    }

    [Test]
    public void AnEvalIsListedUnderItsSetsIdentity_AndTheStubUnderNone()
    {
        // The eval belongs to exactly one set, so the set says whose it is (F18 step 7.5.4d); an eval no longer says it itself.
        var catalog = new EvalCatalog([new FakeSet(new FakeEval("cook"), SampleIdentity.Alpha)], Stub);

        Assert.That(catalog.Listing().Select(l => (l.Name, l.Identity)), Is.EqualTo(new (string, string?)[] { ("cook", SampleIdentity.Alpha), ("stub", null) }));
    }

    private sealed class FakeSet(IEval eval, string identity = SampleIdentity.Alpha) : IEvalSet
    {
        public string Identity { get; } = identity;
        public IReadOnlyList<IEval> Evals { get; } = [eval];
    }

    private sealed class FakeEval(string name, IReadOnlyList<IGrader>? graders = null, IReadOnlyList<EvalScenario>? scenarios = null) : IEval
    {
        public string Name { get; } = name;
        public int Version => 1;
        public string Proves => "A fake eval, to test the catalogue.";
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) => throw new NotImplementedException();
        public IReadOnlyList<EvalScenario> Scenarios { get; } = scenarios ?? [];
        public IReadOnlyList<IGrader> Graders { get; } = graders ?? [new FakeGrader(GraderKind.FixedRule, required: true)];
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotImplementedException();
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }

    private sealed class FakeGrader(GraderKind kind, bool required) : IGrader
    {
        public string Name => "fake";
        public int Version => 1;
        public GraderKind Kind { get; } = kind;
        public bool Required { get; } = required;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
