namespace LouisAgent.Evals.Template.Evals.Example.Scoring;

/// <summary>
/// One trial as numbers (docs/guides/add-an-eval-set.md, section 4): a record of the eval's own keys, written snake case into the run's conversation line.
/// Empty here. Add a property for each number the eval tracks; a renamed property renames the key, so a baseline keeps them put.
/// </summary>
public sealed record ExampleScores : EvalScores;
