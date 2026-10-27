using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Choices;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Protocol;

// F18 step 7.5.3: the protocol's enums travel as snake_case text. SettingChoice parses and writes the wire name from [JsonStringEnumMemberName]; the JSON writer reads the same attribute.
// (A grader kind and a model level travel as their member name, `FixedRule`, as they always have; SettingChoice reads that too.)
// For every member of each enum that goes on the wire the two must write the same text, so a two-word member without the attribute (`apierror` against `api_error`) fails here and not in a stored run.
public class WireNameAgreementTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) } };

    private static string Written<T>(T member) where T : struct, Enum => JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(member, Options))!;

    [Test]
    public void WhatSettingChoiceWrites_IsWhatTheJsonWriterWrites_ForEveryProtocolEnum()
    {
        foreach (var m in Enum.GetValues<TrialFailure>()) Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"TrialFailure.{m}");
        foreach (var m in Enum.GetValues<SplitSelection>()) Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"SplitSelection.{m}");
        foreach (var m in Enum.GetValues<BenchmarkTurnFailure>()) Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"BenchmarkTurnFailure.{m}");
        foreach (var m in Enum.GetValues<BenchmarkRunSplit>()) Assert.That(m.Wire(), Is.EqualTo(Written(m)), $"BenchmarkRunSplit.{m}");
    }

    // The harness writes a turn's failure and a run's split, the API reads and stores them (F18 steps 5.2 and 5.4 reviews): the two sides name the same set in the same words,
    // so a member added to one side only fails here and not in a stored run.
    [Test]
    public void TheHarnessAndTheApiNameTheSameFailures_InTheSameWords() =>
        Assert.That(Enum.GetValues<BenchmarkTurnFailure>().Select(m => m.Wire()).Order(), Is.EqualTo(Enum.GetValues<TrialFailure>().Select(m => m.Wire()).Order()));

    [Test]
    public void TheHarnessAndTheApiNameTheSameSplits_InTheSameWords() =>
        Assert.That(Enum.GetValues<BenchmarkRunSplit>().Select(m => m.Wire()).Order(), Is.EqualTo(Enum.GetValues<SplitSelection>().Select(m => m.Wire()).Order()));
}
