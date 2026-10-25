namespace LouisAgent.Evals.Puls.Routing;

/// <summary>
/// The routing check's setup (F5, F21, F29): the role a kind of question goes to, and the toolsets that decide the modes. The twelve
/// questions are the <c>puls-routing</c> dataset.
/// </summary>
public static class RoutingSetup
{
    /// <summary>The role each kind goes to when Puls delegates; null where Puls answers itself (only cooking is on, F29).</summary>
    public static readonly IReadOnlyDictionary<string, string?> Kinds = new Dictionary<string, string?>
    {
        ["web"] = null, ["files"] = null, ["git"] = null, ["numbers"] = null, ["chat"] = null, ["cook"] = "puls-cook",
    };

    /// <summary>Puls as built (F29): the delegated mode's toolsets.</summary>
    public static readonly IReadOnlyList<string> Delegated = ["specialists", "web"];

    /// <summary>The toolsets that let an agent hand a question on.</summary>
    public static readonly IReadOnlyList<string> DelegatingToolsets = ["specialists", "minions"];
}
