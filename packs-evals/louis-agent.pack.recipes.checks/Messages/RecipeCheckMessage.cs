using System.ComponentModel;

namespace LouisAgent.Packs.Recipes.Messages;

/// <summary>
/// Why the recipe check passed or failed a conversation, in words a person reads in the portal: the pack's own checks'
/// reasons (F35.2), as its tools' texts are <see cref="RecipeToolMessage"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum RecipeCheckMessage
{
    [Description("turn {Turn} gave no answer to read the recipe link from")]
    SourceNothingToRead,

    [Description("the answer gives no link to the recipe's page")]
    SourceNoLink,

    [Description("the recipe link is on {Site}, a recipe site in the table")]
    SourceOnASite,

    [Description("the recipe link is on {Host}, which is no recipe site in the table")]
    SourceOnAnotherHost,
}
