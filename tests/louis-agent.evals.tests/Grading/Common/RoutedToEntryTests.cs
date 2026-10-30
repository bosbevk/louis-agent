using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common;

/// <summary>
/// The <c>routed-to</c> entry: the expected role is a scenario key, and a mode without a delegating toolset is
/// expected to answer itself, since that is all it can do.
/// </summary>
public class RoutedToEntryTests
{
    /// <summary>
    /// The settings a file gives: the key the role is read from, and the toolsets that hand a question on.
    /// </summary>
    private const string Settings =
        $$"""{"expected_key": "routes_to", "delegating_toolsets": ["{{SampleToolset.Alpha}}"]}""";

    /// <summary>A mode that can hand a question on, through the alpha toolset.</summary>
    private static readonly EvalMode CanDelegate = new("delegated", SampleRole.Alpha, [SampleToolset.Alpha]);

    /// <summary>A mode with no delegating toolset.</summary>
    private static readonly EvalMode CannotDelegate = new("baseline", SampleRole.Alpha, [SampleToolset.Gamma]);

    /// <summary>The entry builds today's check, and names the key it reads so the loader checks it is set.</summary>
    [Test]
    public void TheEntry_BuildsTheCheck_NamingItsKey()
    {
        var check = Build(Settings);

        Assert.That((check.Name, check.Version, check.Required), Is.EqualTo(("routed-to", 1, true)));
        Assert.That(check.ExpectedKeys, Is.EqualTo(new[] { "routes_to" }));
    }

    /// <summary>Where the mode can hand on, the question must go to the role the scenario's key names.</summary>
    [Test]
    public async Task AModeThatCanHandOn_IsExpectedToRouteToTheKeysRole()
    {
        const string expected = $$"""{"routes_to": "{{SampleRole.Alpha}}"}""";

        var routed = await GradeAsync(Trial(CanDelegate, expected, Turn(1, [Call(SampleRole.Alpha)])));
        var itself = await GradeAsync(Trial(CanDelegate, expected, Turn(1, [Text("Hallo")])));

        Assert.That((routed.Outcome, itself.Outcome), Is.EqualTo((GradeOutcome.Pass, GradeOutcome.Fail)));
    }

    /// <summary>Where the mode can't hand on, answering itself is right, whatever the key says.</summary>
    [Test]
    public async Task AModeThatCannotHandOn_IsExpectedToAnswerItself()
    {
        var grade = await GradeAsync(
            Trial(CannotDelegate, $$"""{"routes_to": "{{SampleRole.Alpha}}"}""", Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>A null under the key means the agent should answer itself, even where it could hand on.</summary>
    [Test]
    public async Task ANullRole_IsExpectedToAnswerItself()
    {
        var grade = await GradeAsync(Trial(CanDelegate, """{"routes_to": null}""", Turn(1, [Text("Hallo")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>
    /// A scenario without the key, or with something other than a role or null, is refused, naming both.
    /// </summary>
    [TestCase("{}")]
    [TestCase("""{"routes_to": 3}""")]
    public void AScenarioWithoutARole_IsRefused(string expected)
    {
        var refusal = Assert.ThrowsAsync<InvalidDataException>(() =>
            GradeAsync(Trial(CanDelegate, expected, Turn(1, [Text("Hallo")]))));

        Assert.That(refusal!.Message, Does.Contain("bobotie").And.Contain("routes_to"));
    }

    /// <summary>Grades the trial with the check the settings build.</summary>
    private static async Task<Grade> GradeAsync(Trial trial) =>
        await Build(Settings).GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file will: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("routed-to")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
