using System.Text.Json;

namespace LouisAgent.Evals.Reports;

/// <summary>
/// The mean of each numeric score over a slice's trials (F18 step 5c). The keys are read generically, whatever the eval named them: a number is averaged over the
/// trials that have it, with its own n; a string, a boolean, a null or a missing key is skipped, and a trial without the key is not counted in its n.
/// </summary>
internal static class ScoreMeans
{
    public static IReadOnlyList<ScoreMean> Of(IEnumerable<JsonElement> scores)
    {
        var values = new SortedDictionary<string, List<double>>(StringComparer.Ordinal);
        foreach (var score in scores.Where(score => score.ValueKind == JsonValueKind.Object))
            foreach (var property in score.EnumerateObject().Where(property => property.Value.ValueKind == JsonValueKind.Number))
            {
                if (!values.TryGetValue(property.Name, out var list)) values[property.Name] = list = [];
                list.Add(property.Value.GetDouble());
            }
        return [.. values.Select(pair => new ScoreMean(pair.Key, pair.Value.Average(), pair.Value.Count))];
    }
}
