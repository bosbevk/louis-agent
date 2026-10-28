namespace LouisAgent.Evals.Datasets.Scenarios;

/// <summary>
/// Which folder a scenario is in: <c>Dev</c> is tuned against, <c>HeldOut</c> runs only before a release, so a prompt is never
/// fitted to it (plan F18 step 3a). Read from the folder's name, never from a field of the scenario.
/// </summary>
public enum DatasetSplit
{
    Dev,
    HeldOut,
}
