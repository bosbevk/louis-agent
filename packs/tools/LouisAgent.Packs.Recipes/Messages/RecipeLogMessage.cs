using System.ComponentModel;

namespace LouisAgent.Packs.Recipes.Messages;

/// <summary>The recipe pack's log lines (ADR 0009).</summary>
public enum RecipeLogMessage
{
    [Description("RECIPE_SOURCES names '{Slug}', which is not a recipe site; it is ignored.")]
    SourceUnknown,

    [Description("Recipe site '{Slug}' refused a search; the other sites' hits are used.")]
    SiteSkipped,

    [Description("A photo on '{Host}' is gone; the next place to look is tried.")]
    PhotoGone,

    [Description("The recipe library didn't answer ({Reason}); carrying on without it.")]
    LibraryUnavailable,

    [Description("A search offered {Count} recipes from our library.")]
    LibraryOffered,

    [Description("Our library served {Id}, a recipe from {Site}.")]
    LibraryServed,

    [Description("Our library couldn't serve {Id}.")]
    LibraryCantServe,
}
