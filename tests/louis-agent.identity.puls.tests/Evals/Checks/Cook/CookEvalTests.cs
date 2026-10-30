using System.Security.Cryptography;
using LouisAgent.Choices;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook;

/// <summary>
/// Puls's cook eval as a folder of files beside the identity: its name and version as its stored runs carry them, its
/// two modes, its scenarios, its checks in order, its labels and rubric, and its references and known-bads, all as
/// they were when it was C#.
/// </summary>
public class CookEvalTests
{
    /// <summary>It is the identity's eval, under its dataset's name, at the version its trend carries.</summary>
    [Test]
    public void ItIsPulsCooksEval_UnderTheNameItsDatasetCarries()
    {
        var eval = CookFile.Eval;

        Assert.That((eval.Name, eval.Dataset.Name, eval.Identity, eval.Version),
            Is.EqualTo((PulsEvalName.Cook.Wire(), PulsEvalName.Cook.Wire(), PulsIdentityName.Puls.Wire(), 10)));
        Assert.That(eval.Proves, Does.StartWith("Puls routes a meal to its cook"));
        Assert.That(eval.Manual, Is.EqualTo($"{CookFile.Source}/manual.md"));
    }

    /// <summary>One mode gives Puls the cook's tools itself, the other lets it hand off to the cook.</summary>
    [Test]
    public async Task ItsModesArePulsWithTheCooksToolsAndPulsWithItsCook_BothOnPulsVoice()
    {
        var modes = await CookFile.Eval.ModesAsync(null!, CancellationToken.None);

        Assert.That(modes.Select(m => (m.Name, m.Role, string.Join(",", m.Toolsets))),
            Is.EqualTo(new[]
            {
                (CookMode.Tools.Wire(), Role, "web,shops"), (CookMode.Cook.Wire(), Role, "specialists,web")
            }));
    }

    /// <summary>Its scenarios are the cook dataset's, dev and held out, with their places.</summary>
    [Test]
    public void ItsScenariosAreTheCookDatasets()
    {
        var scenarios = CookFile.Eval.Scenarios;

        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.Dev).Select(s => s.Id), Is.EquivalentTo(
            new[]
            {
                PulsCookScenarioIds.CookAf, PulsCookScenarioIds.CookEn, "cook-shops", "cook-malmesbury",
                "cook-no-branch", "cook-recipe-only", "cook-asks-first", "control-web"
            }));
        Assert.That(scenarios.Where(s => s.Split == DatasetSplit.HeldOut).Select(s => s.Id), Is.EquivalentTo(
            new[] { "cook-paarl", "cook-worcester-budget", "cook-follow-up" }));
        Assert.That(scenarios.Single(s => s.Id == "cook-malmesbury").Input.Place,
            Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
    }

    /// <summary>
    /// The fixed rules that must hold come first; the serving rule, the meal judge and its human review are measured
    /// but never fail a run. Each keeps the name and version its stored grades carry.
    /// </summary>
    [Test]
    public void ItsChecks_AreTheRequiredRulesFirst_ThenTheOnesThatAreNot() =>
        Assert.That(CookFile.Eval.Checks.Select(g => (g.Name, g.Version, g.Required, g.Kind)),
            Is.EqualTo(new[]
            {
                ("cook-no-error", 1, true, CheckKind.FixedRule), ("routed-to", 1, true, CheckKind.FixedRule),
                ("within-budget", 2, true, CheckKind.FixedRule),
                ("estimates-at-shop", 2, true, CheckKind.FixedRule),
                ("untooled-shops", 1, true, CheckKind.FixedRule), ("recipe-image", 2, true, CheckKind.FixedRule),
                ("serves-as-asked", 1, false, CheckKind.FixedRule), ("meal-for-people", 2, false, CheckKind.Model),
                ("meal-for-people.human", 2, false, CheckKind.Human)
            }));

    /// <summary>
    /// Its rubric is the judge's calibrated prompt, byte for byte: the same hash its grades carry, so the 36 labels
    /// still describe it, and they travel with its dataset.
    /// </summary>
    [Test]
    public void ItsRubricAndLabels_AreTheCalibratedOnes()
    {
        var hash = Convert.ToHexStringLower(
            SHA256.HashData(File.ReadAllBytes(Path.Combine(CookFile.Folder, "Rubrics", "meal-for-people.md"))))[..12];

        Assert.That(hash, Is.EqualTo("161e5cebcc15"));
        Assert.That(CookFile.Eval.Labels, Has.Count.EqualTo(36));
    }

    /// <summary>Its reference transcripts pass its required checks, and its known-bad ones fail them.</summary>
    [Test]
    public async Task ItsReferencesAndKnownBads_Conform()
    {
        var eval = CookFile.Eval;

        Assert.That(
            await EvalConformance.CheckAsync(eval, ConformanceCases.Load(eval.Transcripts, eval.Scenarios),
                CancellationToken.None), Is.Empty);
    }
}
