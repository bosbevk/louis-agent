using System.ComponentModel;

namespace LouisAgent.Packs.Recipes.Messages;

/// <summary>
/// The recipe tools' texts the model reads (ADR 0009); step 5 adds the rest and pins them with a snapshot.
/// </summary>
public enum RecipeToolMessage
{
    [Description("{Source} refused the search for '{Query}' ({Reason}).")]
    SourceRefused,

    [Description("{Source} answered the search for '{Query}', but the answer couldn't be read ({Reason}).")]
    SourceUnreadable,

    [Description("{Source} has no recipe for '{Query}'. Try fewer or other words.")]
    SourceNoResults,
}
