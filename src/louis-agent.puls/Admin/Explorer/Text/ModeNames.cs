using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Admin.Explorer.Text;

/// <summary>What the explorer calls a mode and its specialists, from the roles its turns handed work to.</summary>
public static class ModeNames
{
    /// <summary>Who a mode is: Puls with the roles it handed work to, or Puls alone if it handed none.</summary>
    public static string Who(ModeNode m) =>
        RolesOf(m) is { Count: > 0 } roles ? $"Puls with {string.Join(" and ", roles)}" : "Puls alone";

    /// <summary>
    /// The specialists' name for a legend: the roles in one mode, or in every mode, or just "specialist" if there are
    /// none.
    /// </summary>
    public static string Roles(IEnumerable<ModeNode> modes)
    {
        var roles = modes.SelectMany(RolesOf).Distinct().ToList();
        return roles.Count == 0 ? "specialist" : string.Join(" / ", roles);
    }

    /// <summary>Who answered a turn: its specialist, the specialist's session it carried on, or Puls itself.</summary>
    /// <remarks>A switch on the turn's shape, one arm per case, instead of a ternary inside a <c>??</c>.</remarks>
    public static string Answerer(TurnNode turn) => turn switch
    {
        { Role: { } role } => role,
        { Continued: { } session } => $"continued · {session}",
        _ => "Puls itself"
    };

    /// <summary>The roles a mode handed work to, from its own turns.</summary>
    private static List<string> RolesOf(ModeNode m) =>
        m.Turns.Select(t => t.Role).Where(r => !string.IsNullOrEmpty(r)).Distinct().ToList()!;
}
