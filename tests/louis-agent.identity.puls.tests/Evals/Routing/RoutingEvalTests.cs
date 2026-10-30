using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Composition;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

namespace LouisAgent.Identities.Puls.Tests.Evals.Routing;

/// <summary>
/// Puls's routing eval as a folder of files beside the identity: eighteen questions, each in a session of its own,
/// asked of one agent with every toolset Puls has but the ones that hand a question on, and of Puls as built. A meal
/// must go to the cook; every other question Puls answers itself.
/// </summary>
public class RoutingEvalTests
{
    /// <summary>Where the folder sits in the repository.</summary>
    private const string Source = "identities/louis-agent.identity.puls/Evals/Routing";

    /// <summary>The toolsets that hand a question on to another agent.</summary>
    private static readonly string[] Delegating = [CoreToolset.Specialists.Wire(), CoreToolset.Minions.Wire()];

    /// <summary>It is the identity's eval, under its dataset's name, at the version its trend carries.</summary>
    [Test]
    public void ItIsPulssRoutingEval_UnderItsDatasetsName()
    {
        var eval = Load();

        Assert.That((eval.Name, eval.Identity, eval.Version),
            Is.EqualTo((eval.Dataset.Name, PulsIdentityName.Puls.Wire(), 2)));
        Assert.That(eval.Proves, Does.StartWith("Puls sends each kind of question to the right place"));
        Assert.That(eval.Manual, Is.EqualTo($"{Source}/manual.md"));
    }

    /// <summary>
    /// The baseline has every toolset Puls has but the delegating ones, the list written in the file, so a run says
    /// what it ran; the delegated mode is Puls as built, its specialists and the web.
    /// </summary>
    [Test]
    public async Task TheBaseline_IsEveryToolsetPulsHas_ButTheDelegatingOnes()
    {
        var puls = Plugins.FromSettings(PulsIdentityName.Puls.Wire(), null).Toolsets.Select(toolset => toolset.Name);

        var modes = await Load().ModesAsync(null!, CancellationToken.None);

        Assert.That(modes.Select(mode => mode.Role).Distinct(), Is.EqualTo(new[] { "puls/voice-af" }));
        Assert.That(modes[0].Toolsets, Is.EquivalentTo(puls.Except(Delegating)));
        Assert.That(modes[1].Toolsets, Is.EqualTo(new[] { CoreToolset.Specialists.Wire(), CoreToolset.Web.Wire() }));
    }

    /// <summary>Its one check is the route, required, where only the specialists hand a question on.</summary>
    [Test]
    public void ItsOneCheck_IsTheRequiredRoute()
    {
        var check = Load().Checks.Single();

        Assert.That((check.Name, check.Required, check.Kind), Is.EqualTo(("routed-to", true, CheckKind.FixedRule)));
        Assert.That(check.ExpectedKeys, Is.EqualTo(new[] { "routes_to" }));
    }

    /// <summary>
    /// Six kinds of question, each with two to work on and one new one held out, each in its kind's category; a meal
    /// expects the cook, every other question nobody.
    /// </summary>
    [Test]
    public void EachKind_HasTwoDevQuestionsAndOneHeldOut_AndOnlyAMealGoesToTheCook()
    {
        var scenarios = Load().Scenarios;
        var kinds = scenarios.GroupBy(scenario => scenario.Id.Split('-')[0]).ToList();

        Assert.That(kinds, Has.Count.EqualTo(6));
        foreach (var kind in kinds)
        {
            Assert.That(kind.Select(scenario => scenario.Split),
                Is.EquivalentTo(new[] { DatasetSplit.Dev, DatasetSplit.Dev, DatasetSplit.HeldOut }), kind.Key);
            Assert.That(kind.SelectMany(scenario => scenario.Categories), Has.All.EqualTo($"kind:{kind.Key}"));
        }

        Assert.That(scenarios.Where(scenario => RouteOf(scenario) is not null).Select(RouteOf).Distinct(),
            Is.EqualTo(new[] { PulsRole.Cook.Wire() }));
        Assert.That(scenarios.Where(scenario => RouteOf(scenario) is not null).Select(scenario => scenario.Id),
            Has.All.StartWith(kinds.Single(kind => kind.Any(scenario => RouteOf(scenario) is not null)).Key));
    }

    /// <summary>
    /// Puls asks first about a meal that doesn't say for how many or where they'll shop, and never hands it on; so a
    /// question that should reach the cook names its people (read as the cook's checks read them, in words too) and its
    /// shop.
    /// </summary>
    [Test]
    public void EveryQuestionForTheCook_NamesItsPeopleAndItsShop_ElsePulsWouldAskFirst()
    {
        foreach (var scenario in Load().Scenarios.Where(scenario => RouteOf(scenario) is not null))
        {
            var first = scenario.Input.Messages[0];
            Assert.That(Servings.Of(first), Is.Not.Empty, $"{scenario.Id}: the people");
            Assert.That(first, Does.Contain("PnP").Or.Contain("maak nie saak").Or.Contain("doesn't matter"),
                $"{scenario.Id}: the shop");
        }
    }

    /// <summary>
    /// Its manual, beside its file, opens with a title and names the eval and its check, in backticks.
    /// </summary>
    [Test]
    public void ItsManual_NamesTheEvalAndItsCheck()
    {
        var eval = Load();
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "EvalFiles", eval.Manual!));

        Assert.That(text, Does.StartWith("# "));
        Assert.That(text, Does.Contain($"`{eval.Name}`").And.Contain($"`{eval.Checks.Single().Name}`"));
    }

    /// <summary>The held-out questions are for a release check: none is also a question to work on.</summary>
    [Test]
    public void NoHeldOutQuestion_IsAlsoADevQuestion()
    {
        var scenarios = Load().Scenarios;
        var dev = scenarios.Where(s => s.Split == DatasetSplit.Dev).SelectMany(s => s.Input.Messages).ToHashSet();

        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.HeldOut).SelectMany(s => s.Input.Messages)
            .Where(dev.Contains), Is.Empty);
    }

    /// <summary>
    /// Its reference transcripts pass the route and its known-bad ones fail it, as they did when it was C#.
    /// </summary>
    [Test]
    public async Task ItsReferencesAndKnownBads_Conform()
    {
        var eval = Load();
        var cases = ConformanceCases.Load(eval.Transcripts, eval.Scenarios);

        Assert.That(await EvalConformance.CheckAsync(eval, cases, CancellationToken.None), Is.Empty);
    }

    /// <summary>The scenario's route: the role under <c>routes_to</c>, or null when Puls answers it itself.</summary>
    private static string? RouteOf(EvalScenario scenario) =>
        scenario.Expected.GetProperty("routes_to") is { ValueKind: JsonValueKind.String } role
            ? role.GetString()
            : null;

    /// <summary>The eval, from the folder its project copied beside these tests.</summary>
    private static FileEval Load() =>
        FileEval.Load(Path.Combine(AppContext.BaseDirectory, "EvalFiles", Source), new CheckCatalog(GeneralChecks.All),
            Source);
}
