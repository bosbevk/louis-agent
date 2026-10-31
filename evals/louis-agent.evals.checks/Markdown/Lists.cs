using System.Text.RegularExpressions;

namespace LouisAgent.Evals.Markdown;

/// <summary>
/// A shopping list in an answer: its header's cells, and its ingredient rows' cells (totals and separators left out).
/// </summary>
public sealed record ShoppingList(List<string> Header, List<List<string>> Rows);

/// <summary>
/// The answer's markdown tables, and which of them is the shopping list. The branches and what's unsure are tables
/// too: a table with a why column is the cook's *Onseker* table, never the list (cook check 42d837f13659).
/// </summary>
/// <remarks>
/// In the check contract (Louis's pick, F49.3 step 1a), so an identity's prices and a pack's shop rules read a table
/// and a shopping list the same way, with no copy and no reference to a sibling (Move Method, Fowler). It knows how
/// the agents head a shopping list, in Afrikaans and English: that's what tells the list from the other tables.
/// </remarks>
public static partial class Lists
{
    /// <summary>A header that starts a shopping list: an ingredient column first.</summary>
    [GeneratedRegex(@"^\|\s*(Item|Bestanddeel|Ingredient|Produk|Wat|What)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Header();

    /// <summary>A why column, which marks the cook's table of what it's unsure about.</summary>
    [GeneratedRegex(@"\|\s*(Hoekom|Waarom|Why)\s*\|", RegexOptions.IgnoreCase)]
    private static partial Regex WhyColumn();

    /// <summary>The line of dashes under a table's header.</summary>
    [GeneratedRegex(@"^\|\s*:?-")]
    private static partial Regex Separator();

    /// <summary>A bold row, which closes a list with its totals.</summary>
    [GeneratedRegex(@"^\|\s*\*\*")]
    private static partial Regex Total();

    /// <summary>The answer's tables, each as its lines in order: runs of lines that start with '|'.</summary>
    public static List<List<string>> Tables(string answer)
    {
        var tables = new List<List<string>>();
        var current = new List<string>();
        foreach (string line in answer.Split(["\r\n", "\n", "\r"], StringSplitOptions.None))
        {
            if (line.TrimStart().StartsWith('|')) current.Add(line.Trim());
            else if (current.Count > 0)
            {
                tables.Add(current);
                current = [];
            }
        }

        if (current.Count > 0) tables.Add(current);
        return tables;
    }

    /// <summary>The shopping lists in an answer, in order; none when the answer has none.</summary>
    public static List<ShoppingList> Of(string? answer) =>
        Tables(answer ?? "").Where(table => IsList(table[0]))
            .Select(table => new ShoppingList(Cells(table[0]),
                table.Skip(1).Where(row => !IsSeparator(row) && !IsTotal(row)).Select(Cells).ToList()))
            .ToList();

    /// <summary>
    /// The bold rows that close a shopping list (`**Totaal** … **± R312**`, `**Per persoon** …`), as their cells, in
    /// order; none for another table.
    /// </summary>
    public static List<List<string>> TotalRows(string? answer) =>
        Tables(answer ?? "").Where(table => IsList(table[0]))
            .SelectMany(table => table.Skip(1).Where(IsTotal).Select(Cells)).ToList();

    /// <summary>Whether a table's header is a shopping list's: an ingredient column first, and no why column.</summary>
    public static bool IsList(string header) => Header().IsMatch(header) && !WhyColumn().IsMatch(header);

    /// <summary>Whether a row is the line of dashes under a table's header.</summary>
    public static bool IsSeparator(string row) => Separator().IsMatch(row);

    /// <summary>Whether a row is a bold totals row.</summary>
    public static bool IsTotal(string row) => Total().IsMatch(row);

    /// <summary>A table row's cells, without the outer pipes.</summary>
    public static List<string> Cells(string row) =>
        row.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToList();
}
