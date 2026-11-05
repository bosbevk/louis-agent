namespace LouisAgent.Packs.Recipes.Reading;

/// <summary>
/// Ends a text page's print where the recipe ends. Real pages run on into readers' comments with their names, related
/// recipes and newsletter boxes: The Kate Tin's bobotie's neighbour had 92 comments. None of that helps the cook, and an
/// open feed must not publish it.
/// </summary>
/// <remarks>
/// A plain line reader, not a regex (ADR 0057), and one filter in the print (Pipes and Filters). The endings are what real
/// prints showed. A heading only ends the recipe after a real part of the page, so one in a menu above it can't take the
/// recipe with it, and a sentence that mentions comments isn't a heading, so it stays. Better to keep a little too much
/// than to cut a method.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
internal static class PrintedRecipeText
{
    /// <summary>What a recipe page goes on to, after its recipe, in the words real pages use.</summary>
    private static readonly string[] Endings =
    [
        "comments", "leave a reply", "leave a comment", "share this recipe", "related recipes", "related posts",
        "you may also like", "you have successfully subscribed", "previous post", "next post", "verwante artikels",
        "lewer kommentaar", "kommentaar"
    ];

    /// <summary>A heading is short, so a longer line is a sentence.</summary>
    private const int LongestHeading = 45;

    /// <summary>Less than this before a heading means we haven't reached the recipe yet.</summary>
    private const int ShortestRecipe = 300;

    /// <summary>The text up to the first heading that ends the recipe, or all of it when none does.</summary>
    public static string Cut(string text)
    {
        int at = 0;
        foreach (string line in text.Split('\n'))
        {
            if (at >= ShortestRecipe && IsAnEnding(line)) return text[..at].TrimEnd();
            at += line.Length + 1;
        }

        return text;
    }

    /// <summary>Whether a line is one of the endings, with its heading marks and a count taken off.</summary>
    private static bool IsAnEnding(string line)
    {
        string words = line.Trim().TrimStart('#', '*', ' ').TrimEnd('*', ':', '!', ' ').ToLowerInvariant();
        if (words.Length == 0 || words.Length > LongestHeading) return false;

        string withoutCount = words.TrimStart("0123456789 ".ToCharArray());
        return Endings.Contains(withoutCount);
    }
}
