namespace LouisAgent.Web.Shell;

/// <summary>A model's id as a name for the header: <c>claude-opus-5-5</c> reads Claude Opus 5.5.</summary>
public static class ModelNames
{
    /// <summary>The words capitalised, the version's parts joined with dots; nothing for no id.</summary>
    public static string? Pretty(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var words = id.Split('-', StringSplitOptions.RemoveEmptyEntries);
        var named = words.TakeWhile(w => !char.IsDigit(w[0])).Select(w => char.ToUpperInvariant(w[0]) + w[1..]);
        string version = string.Join(".", words.SkipWhile(w => !char.IsDigit(w[0])));
        return string.Join(" ", named) + (version.Length > 0 ? " " + version : "");
    }
}
