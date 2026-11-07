using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Web.Admin.Runs;

/// <summary>
/// Who each mode of a run is, for the trend's legend: the agent with the roles its turns handed work to, or the agent
/// alone.
/// </summary>
public static class ModeLabels
{
    /// <summary>A name per mode, from the roles its turns used, each once, in the order they first came.</summary>
    public static Dictionary<string, string> Of(IEnumerable<TurnRow> turns) =>
        turns.GroupBy(t => t.Mode).ToDictionary(g => g.Key, g => Label(g));

    /// <summary>One mode's name: the roles it used joined with "and", or the agent alone when it used none.</summary>
    private static string Label(IEnumerable<TurnRow> turns)
    {
        var roles = turns.Select(t => t.Role).Where(r => !string.IsNullOrEmpty(r)).Distinct().ToList();
        if (roles.Count == 0) return "The agent alone";
        return $"The agent with {string.Join(" and ", roles)}";
    }
}
