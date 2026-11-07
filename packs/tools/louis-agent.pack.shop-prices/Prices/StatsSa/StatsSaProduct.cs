namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa;

/// <summary>
/// A product's name as Stats SA writes it, tidied the way the shipped file has it: one space between words, and a dash
/// with a space on each side ("Onions  - fresh" and "Mushrooms-fresh" become "Onions - fresh" and "Mushrooms - fresh").
/// Used only for a product the names file doesn't list yet; the file's own name wins.
/// </summary>
public static class StatsSaProduct
{
    /// <summary>The name, tidied.</summary>
    public static string Tidy(string name) =>
        string.Join(' ', name.Replace("-", " - ").Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
