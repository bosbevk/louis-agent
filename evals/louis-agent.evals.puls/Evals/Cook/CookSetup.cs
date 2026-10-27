using LouisAgent.Evals.Puls.Shared;
namespace LouisAgent.Evals.Puls.Evals.Cook;

/// <summary>
/// The cook check's setup: the two modes Puls runs in. The conversations, and the place each is asked from, are the <c>puls-cook</c> dataset.
/// </summary>
public static class CookSetup
{
    /// <summary>The modes: Puls with its own shop tools, and Puls with its cook (F34: the same toolsets the cook has).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Modes = new Dictionary<string, IReadOnlyList<string>>
    {
        ["puls-tools"] = [PulsToolset.Web.Name(), PulsToolset.Shops.Name()],
        ["puls-cook"] = [PulsToolset.Specialists.Name(), PulsToolset.Web.Name()],
    };
}
