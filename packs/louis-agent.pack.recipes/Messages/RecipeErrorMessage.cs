using System.ComponentModel;

namespace LouisAgent.Packs.Recipes.Messages;

/// <summary>The pack's errors for a developer, when its own content is wrong (ADR 0009).</summary>
public enum RecipeErrorMessage
{
    [Description("Row {Row} of the recipe sites has {Count} columns, not {Expected}.")]
    SiteRowColumns,

    [Description("Row {Row} of the recipe sites has '{Value}' for {Column}, which is not a valid {Column}.")]
    SiteRowValue,

    [Description("The recipe sites table is missing from the pack.")]
    SitesMissing,
}
