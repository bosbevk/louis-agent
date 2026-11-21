namespace LouisAgent.Packs.Recipes.Library.Usage;

/// <summary>The four things the library's counter adds up in a day.</summary>
public enum RecipeUsageKind
{
    /// <summary>A recipe read from a site that the library didn't hold yet.</summary>
    Kept,

    /// <summary>A hit from the library shown in a search.</summary>
    Offered,

    /// <summary>A recipe the cook read from the library.</summary>
    Served,

    /// <summary>A library read that failed, so the cook went without.</summary>
    Failed
}
