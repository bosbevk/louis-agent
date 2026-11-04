namespace LouisAgent.Packs.Recipes;

/// <summary>
/// The recipes pack's own settings, read once by the composition root. Read it from the environment and you get
/// the sites a deployment turned on.
/// </summary>
/// <remarks>
/// A plain options object with a reader that takes the variable lookup, so a
/// test needs no real environment. Nothing is enabled until a person names it, because a site is read only after
/// its terms were checked (ADR 0023).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0023-copyleft-service-beside-us.md">ADR 0023: a copyleft service may run beside us</seealso>
public sealed class RecipesOptions
{
    /// <summary>
    /// RECIPE_SOURCES: the sites whose recipes may be searched, comma-separated slugs, or <c>*</c> for every site in
    /// the table; unset, none.
    /// </summary>
    public string? RecipeSources { get; init; }

    /// <summary>Reads the settings; the variable lookup is replaceable for tests.</summary>
    public static RecipesOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();
        return new RecipesOptions { RecipeSources = Get("RECIPE_SOURCES") };
    }
}
