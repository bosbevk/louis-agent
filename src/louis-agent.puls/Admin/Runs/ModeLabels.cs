using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Runs;

/// <summary>
/// Who each mode of a run is, for the trend's legend: Puls with the roles its turns handed work to, or Puls alone.
/// </summary>
public static class ModeLabels
{
    /// <summary>A name per mode, from the roles its turns used, each once, in the order they first came.</summary>
    public static Dictionary<string, string> Of(IEnumerable<TurnRow> turns) =>
        turns.GroupBy(t => t.Mode).ToDictionary(g => g.Key, g => Label(g));

    /// <summary>One mode's name: the roles it used joined with "and", or Puls alone when it used none.</summary>
    private static string Label(IEnumerable<TurnRow> turns)
    {
        var roles = turns.Select(t => t.Role).Where(r => !string.IsNullOrEmpty(r)).Distinct().ToList();
        if (roles.Count == 0) return "Puls alone";
        return $"Puls with {string.Join(" and ", roles)}";
    }
}
