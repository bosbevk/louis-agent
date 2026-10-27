namespace LouisAgent.Puls.Admin;

/// <summary>
/// The admin portal's model filter (F18 step 2a): the models a run names, and whether a run matches the model picked. A run
/// that named no model ("unknown") isn't under any model, so it's shown only when no model is picked.
/// </summary>
public static class RunModelFilter
{
    /// <summary>The distinct models the runs name, in the order they first appear, without "unknown".</summary>
    public static IReadOnlyList<string> ModelsOf(IEnumerable<IReadOnlyList<string>?> runs)
    {
        var models = new List<string>();
        foreach (var run in runs)
            foreach (string model in run ?? [])
                if (!models.Contains(model))
                    models.Add(model);
        return models;
    }

    /// <summary>Whether a run that named <paramref name="runModels"/> is shown for the picked model (null: every run is shown).</summary>
    public static bool Matches(IReadOnlyList<string>? runModels, string? picked) =>
        picked is null || (runModels is not null && runModels.Contains(picked));
}
