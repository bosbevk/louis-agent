using System.Text.Json;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Reports;

/// <summary>
/// One graded trial, as the roll-up reads it (F18 step 5c): its scenario and that scenario's categories, its outcome, the grades behind it and the
/// eval's scores for it. A value: the roll-up is a pure function of these.
/// </summary>
public sealed record TrialResult(
    string Scenario,
    IReadOnlyList<string> Categories,
    TrialOutcome Outcome,
    IReadOnlyList<Grade> Grades,
    JsonElement Scores);
