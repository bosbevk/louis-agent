using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common;

/// <summary>
/// The trial's first turn went where the eval expects for this trial: "went to role X", the way Anthropic's tool-call
/// check reads which tool was called. The expected role is the eval's, per trial; null means the agent should answer
/// itself. A turn that errored is not right, and a specialist's own hand-over is not the turn's route. An eval whose
/// route is a scenario's <c>expected</c> key names it in <paramref name="expectedKeys"/>, so the loader checks it is
/// read and fed.
/// </summary>
/// <remarks>
/// A file states the route as data (<see cref="ByScenarioKey"/>): the role is read from a key of the scenario's
/// <c>expected</c>, and a mode with none of the delegating toolsets is expected to answer itself, since that is all it
/// can do. Its <see cref="Entry"/> builds that from a file's settings (Factory Method), nested here as the other
/// general checks' are.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class RoutedToCheck(
    Func<Trial, string?> expectedRole,
    bool required = true,
    IReadOnlySet<string>? expectedKeys = null) : ICheck
{
    public string Name => "routed-to";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = expectedKeys ?? new HashSet<string>();

    /// <summary>
    /// The route as a file states it: the role under <paramref name="expectedKey"/> in the scenario's
    /// <c>expected</c> where the mode has one of <paramref name="delegatingToolsets"/>, else the agent answers itself.
    /// </summary>
    public static RoutedToCheck ByScenarioKey(string expectedKey, IReadOnlyList<string> delegatingToolsets,
        bool required = true) =>
        new(trial => CanHandOn(trial.Mode, delegatingToolsets) ? RoleIn(trial.Scenario, expectedKey) : null,
            required, new HashSet<string> { expectedKey });

    /// <summary>
    /// Compares the first turn's route with the one expected: a pass when they agree, else a fail saying where it went.
    /// </summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        // The route is decided by the first turn; a later turn's hand-over is no reason for its miss.
        var first = trial.Transcript.Turns.FirstOrDefault();
        if (first is null)
            return ValueTask.FromResult(Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn)));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return ValueTask.FromResult(unknown);
        if (EngineEvents.StreamedError(first))
            return ValueTask.FromResult(Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed)));

        var expected = expectedRole(trial);
        var actual = EngineEvents.RoutedRole(first);
        if (actual == expected)
            return ValueTask.FromResult(Verdict.Pass(Name, Version, MessageText.Format(
                expected is null ? CommonGradeMessage.AnsweredAsExpected : CommonGradeMessage.RoutedPass,
                new { Role = expected })));
        if (expected is null)
            return ValueTask.FromResult(Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.HandedAway, new { Actual = actual })));
        return ValueTask.FromResult(actual is null
            ? Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.AnsweredItself, new { Role = expected }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.RoutedElsewhere, new
                {
                    Actual = actual,
                    Role = expected
                })));
    }

    /// <summary>True when the mode has a toolset that hands a question on to a role.</summary>
    private static bool CanHandOn(EvalMode mode, IReadOnlyList<string> delegatingToolsets) =>
        mode.Toolsets.Any(toolset => delegatingToolsets.Contains(toolset, StringComparer.Ordinal));

    /// <summary>
    /// The role the scenario names under the key, or null when it says the agent answers itself; a scenario that
    /// names neither is refused, since the check can't be told what to expect.
    /// </summary>
    /// <remarks>A guard clause for the missing key, then a switch expression on the value's kind (ADR 0051).</remarks>
    private static string? RoleIn(EvalScenario scenario, string key)
    {
        if (scenario.Expected.ValueKind != JsonValueKind.Object || !scenario.Expected.TryGetProperty(key, out var role))
            throw Refuse(EvalErrorMessage.NoExpectedRole, scenario, key);
        return role.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String when !string.IsNullOrWhiteSpace(role.GetString()) => role.GetString(),
            _ => throw Refuse(EvalErrorMessage.BadExpectedRole, scenario, key)
        };
    }

    /// <summary>The refusal of a scenario's route, naming the scenario and the key.</summary>
    private static InvalidDataException Refuse(EvalErrorMessage message, EvalScenario scenario, string key) =>
        new(MessageText.Format(message, new
        {
            scenario.Id,
            Key = key
        }));

    /// <summary>What a file can set: the key the role is read from, and the toolsets that hand a question on.</summary>
    public sealed record Settings(string ExpectedKey, IReadOnlyList<string> DelegatingToolsets, bool Required = true);

    /// <summary>The check as a file names it, <c>routed-to</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("routed-to", CheckKind.FixedRule)
    {
        /// <summary>Builds the route from the scenario's key, as the settings say.</summary>
        protected override ICheck Create(Settings settings) =>
            ByScenarioKey(settings.ExpectedKey, settings.DelegatingToolsets, settings.Required);
    }
}
