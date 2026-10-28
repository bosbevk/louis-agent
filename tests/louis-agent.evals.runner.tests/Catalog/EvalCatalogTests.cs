using System.Text.Json;
using LouisAgent.Testing;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// The catalogue holds every eval the runner can run, each by name once, and none that no run can trust; its listing
/// is what the API reads.
/// </summary>
public class EvalCatalogTests
{
    private static readonly IEval Stub = new StubEval();

    /// <summary>The stub is always there, even when no eval set is chosen.</summary>
    [Test]
    public void TheStubIsAlwaysListed_EvenWithNoSets()
    {
        var catalog = new EvalCatalog([], Stub);

        Assert.That(catalog.Listing().Select(l => l.Name), Is.EqualTo(new[] { "stub" }));
    }

    /// <summary>Each eval shows up once, the sets' evals first and the stub last.</summary>
    [Test]
    public void EachEval_IsListedOnce()
    {
        var catalog = new EvalCatalog([new FakeSet(new FakeEval("cook")), new FakeSet(new FakeEval("routing"))], Stub);

        Assert.That(catalog.Listing().Select(l => l.Name), Is.EqualTo(new[] { "cook", "routing", "stub" }));
    }

    /// <summary>Two evals can't share a name: the runner wouldn't know which one to run.</summary>
    [Test]
    public void RefusesTwoEvalsWithOneName()
    {
        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook")), new FakeSet(new FakeEval("cook"))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook'"));
    }

    /// <summary>An eval with nothing to grade it is refused.</summary>
    [Test]
    public void RefusesAnEvalWithNoGraders()
    {
        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", []))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook' has no graders"));
    }

    /// <summary>A model can't be an eval's only judge: it needs a check that asks no model.</summary>
    [Test]
    public void RefusesAnEvalWithOnlyModelGraders()
    {
        var model = new FakeGrader(GraderKind.Model, required: true);

        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", [model]))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook' has no required fixed-rule grader"));
    }

    /// <summary>A fixed rule that doesn't decide the trial doesn't count.</summary>
    [Test]
    public void RefusesAnEvalWhoseFixedRuleIsNotRequired()
    {
        // A fixed rule that doesn't decide the trial is no check: the eval needs one it must pass.
        var optional = new FakeGrader(GraderKind.FixedRule, required: false);

        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", [optional]))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'cook' has no required fixed-rule grader"));
    }

    /// <summary>One required fixed rule is enough to be let in.</summary>
    [Test]
    public void AcceptsAnEvalWithARequiredFixedRule()
    {
        var catalog =
            new EvalCatalog([new FakeSet(new FakeEval("cook", [new FakeGrader(GraderKind.FixedRule, required: true)]))],
                Stub);

        Assert.That(catalog.Find("cook"), Is.Not.Null);
    }

    /// <summary>Find gives you the eval by name, or nothing.</summary>
    [Test]
    public void FindReturnsTheEvalByNameOrNull()
    {
        var catalog = new EvalCatalog([], Stub);

        Assert.That(catalog.Find("stub"), Is.SameAs(Stub));
        Assert.That(catalog.Find("nope"), Is.Null);
    }

    /// <summary>Asking for a name no eval has is refused, and the refusal names the evals there are.</summary>
    [Test]
    public void GetRefusesAnUnknownNameNamingTheEvals()
    {
        // The catalogue refuses the name, being the one place that knows the evals, not the host.
        var catalog = new EvalCatalog([], Stub);

        Assert.That(catalog.Get("stub"), Is.SameAs(Stub));
        Assert.That(() => catalog.Get("nope"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'nope'").And.Message.Contains("stub"));
    }

    /// <summary>
    /// Each listed eval is one JSON object, with its fields in the order the API reads them and the stub's own values.
    /// </summary>
    [Test]
    public void TheListingIsOneJsonObjectPerEval_AsTheAPIReadsIt()
    {
        // Read back as JSON, not compared as text: the encoder may escape an apostrophe, and the API parses either.
        var listing = new EvalCatalog([], Stub).Listing();
        var line = JsonDocument.Parse(JsonSerializer.Serialize(listing.Single(), EvalJson.SnakeCase)).RootElement;

        Assert.That(line.EnumerateObject().Select(p => p.Name),
            Is.EqualTo(new[]
            {
                "name", "identity", "version", "proves", "manual", "dataset", "categories", "graders"
            }));
        Assert.That(line.GetProperty("graders").GetRawText(),
            Is.EqualTo("""[{"name":"no-error","version":1,"kind":"FixedRule"},"""
                       + """{"name":"within-time","version":1,"kind":"FixedRule"}]"""),
            "each grader by name, version and kind, the kind as the protocol writes it");
        Assert.That(line.GetProperty("dataset").ValueKind, Is.EqualTo(JsonValueKind.Null), "the stub has no dataset");
        Assert.That(line.GetProperty("categories").GetRawText(), Is.EqualTo("[]"));
        Assert.That(line.GetProperty("manual").GetString(),
            Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"),
            "the manual's path from the repo root, forward slashes (7.1)");
        Assert.That(line.GetProperty("name").GetString(), Is.EqualTo("stub"));
        Assert.That(line.GetProperty("identity").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(line.GetProperty("version").GetInt32(), Is.EqualTo(1));
        Assert.That(line.GetProperty("proves").GetString(), Is.EqualTo(MessageText.Format(RunnerMessage.StubProves)));
    }

    /// <summary>A scenario with nothing to send is refused when the catalogue is built, not skipped later.</summary>
    [Test]
    public void RefusesAScenarioWithNoMessages()
    {
        var empty = new EvalScenario("empty", new EvalInput(null, "Africa/Johannesburg", []), default, [],
            DatasetSplit.Dev);

        Assert.That(() => new EvalCatalog([new FakeSet(new FakeEval("cook", scenarios: [empty]))], Stub),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'empty'").And.Message.Contains("no messages"));
    }

    /// <summary>An eval is listed under its set's identity, and the stub, in no set, under none.</summary>
    [Test]
    public void AnEvalIsListedUnderItsSetsIdentity_AndTheStubUnderNone()
    {
        var catalog = new EvalCatalog([new FakeSet(new FakeEval("cook"), SampleIdentity.Alpha)], Stub);

        Assert.That(catalog.Listing().Select(l => (l.Name, l.Identity)),
            Is.EqualTo(new (string, string?)[] { ("cook", SampleIdentity.Alpha), ("stub", null) }));
    }

    /// <summary>
    /// The portal lists what an eval is before it ever ran: its dataset, the categories its scenarios fall in (each
    /// once, in the order they first come) and its graders.
    /// </summary>
    [Test]
    public void AnEvalsDatasetCategoriesAndGraders_AreListed()
    {
        var dataset = new DatasetManifest("cook-set", 3, "Meals", "hash", []);
        var model = new FakeGrader(GraderKind.Model, required: false);
        var catalog = new EvalCatalog(
        [
            new FakeSet(new FakeEval("cook", [new FakeGrader(GraderKind.FixedRule, required: true), model],
                [Scenario("a", "town", "city"), Scenario("b", "town")], dataset))
        ], Stub);

        var cook = catalog.Listing().First();

        Assert.That(cook.Dataset, Is.EqualTo(new ListedDataset("cook-set", 3)));
        Assert.That(cook.Categories, Is.EqualTo(new[] { "town", "city" }));
        Assert.That(cook.Graders,
            Is.EqualTo(new[]
            {
                new ListedGrader("fake", 1, GraderKind.FixedRule), new ListedGrader("fake", 1, GraderKind.Model)
            }));
    }

    /// <summary>A scenario with one message, in the categories given.</summary>
    private static EvalScenario Scenario(string id, params string[] categories) =>
        new(id, new EvalInput(null, "Africa/Johannesburg", ["hi"]), default, categories, DatasetSplit.Dev);

    /// <summary>A set of one eval, under an identity.</summary>
    private sealed class FakeSet(IEval eval, string identity = SampleIdentity.Alpha) : IEvalSet
    {
        public string Identity { get; } = identity;
        public IReadOnlyList<IEval> Evals { get; } = [eval];
    }

    /// <summary>
    /// An eval that only describes itself: the catalogue never runs it, so its modes and scores aren't there.
    /// </summary>
    private sealed class FakeEval(
        string name,
        IReadOnlyList<IGrader>? graders = null,
        IReadOnlyList<EvalScenario>? scenarios = null,
        DatasetManifest? dataset = null) : IEval
    {
        public string Name { get; } = name;
        public DatasetManifest? Dataset { get; } = dataset;
        public int Version => 1;
        public string Proves => "A fake eval, to test the catalogue.";

        /// <summary>Never asked: the catalogue doesn't run the eval.</summary>
        public ValueTask<IReadOnlyList<EvalMode>> ModesAsync(ISessionClient sessions,
            CancellationToken cancellationToken) => throw new NotImplementedException();

        public IReadOnlyList<EvalScenario> Scenarios { get; } = scenarios ?? [];

        public IReadOnlyList<IGrader> Graders { get; } =
            graders ?? [new FakeGrader(GraderKind.FixedRule, required: true)];

        /// <summary>Never asked: the catalogue doesn't score.</summary>
        public EvalScores ScoresOf(Trial trial, IReadOnlyList<Grade> grades) => throw new NotImplementedException();

        /// <summary>Never asked: the catalogue doesn't record turns.</summary>
        public TurnFacts TurnFactsOf(TurnTranscript turn, EvalMode mode) => throw new NotImplementedException();
    }

    /// <summary>A grader of the kind given, which the catalogue only looks at and never asks to grade.</summary>
    private sealed class FakeGrader(GraderKind kind, bool required) : IGrader
    {
        public string Name => "fake";
        public int Version => 1;
        public GraderKind Kind { get; } = kind;
        public bool Required { get; } = required;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

        /// <summary>Never asked: the catalogue doesn't grade.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
