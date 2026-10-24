namespace LouisAgent.Evals.Puls.Cook;

/// <summary>
/// The cook check's setup, as the tests need it: the two modes Puls runs in, and where two of the conversations are asked. The
/// rest of the conversations come with the cook eval (step 5), so this is not the whole dataset yet.
/// </summary>
public static class CookSetup
{
    /// <summary>The modes: Puls with its own shop tools, and Puls with its cook (F34: the same toolsets the cook has).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Modes = new Dictionary<string, IReadOnlyList<string>>
    {
        ["puls-tools"] = ["web", "shops"],
        ["puls-cook"] = ["specialists", "web"],
    };

    /// <summary>The place each of the first two conversations is asked from.</summary>
    public static readonly IReadOnlyDictionary<string, string> Places = new Dictionary<string, string>
    {
        ["cook-malmesbury"] = "Malmesbury, Wes-Kaap, Suid-Afrika",
        ["cook-no-shop"] = "Prince Albert, Wes-Kaap, Suid-Afrika",
    };
}
