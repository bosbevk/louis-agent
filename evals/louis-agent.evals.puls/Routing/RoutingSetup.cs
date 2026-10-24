namespace LouisAgent.Evals.Puls.Routing;

/// <summary>
/// The routing check's setup (F5, F21, F29): the kind of each question and the role a kind goes to, the two modes' toolsets,
/// and the twelve questions' tasks. The questions' texts come with the routing eval (step 5), so this is not the full dataset.
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

    /// <summary>Two of each kind, so one odd answer doesn't decide a kind.</summary>
    public static readonly IReadOnlyList<string> Tasks =
    [
        "web", "files", "git", "numbers", "chat", "cook",
        "web-2", "files-2", "git-2", "numbers-2", "chat-2", "cook-2",
    ];
}
