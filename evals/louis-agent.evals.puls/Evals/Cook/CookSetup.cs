using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Evals.Cook;

/// <summary>
/// The cook check's setup: the two modes Puls runs in. The conversations, and the place each is asked from, are the <c>puls-cook</c> dataset.
/// </summary>
public static class CookSetup
{
    /// <summary>The modes: Puls with its own shop tools, and Puls with its cook (F34: the same toolsets the cook has).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Modes =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [CookMode.Tools.Wire()] = [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()],
            [CookMode.Cook.Wire()] = [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()],
        };
}
